using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AdvUtils;
using CRFSharp;
using System.Threading.Tasks;
using CRFSharp.decoder;

namespace CRFSharpWrapper
{
    public class Encoder
    {
        public enum REG_TYPE { L1, L2 };

        //encoding CRF model from training corpus
        public bool Learn(EncoderArgs args)
        {
            if (args.min_diff <= 0.0)
            {
                Logger.WriteLine(Logger.Level.err, "eta must be > 0.0");
                return false;
            }

            if (args.C < 0.0)
            {
                Logger.WriteLine(Logger.Level.err, "C must be >= 0.0");
                return false;
            }

            if (args.threads_num <= 0)
            {
                Logger.WriteLine(Logger.Level.err, "thread must be > 0");
                return false;
            }

            if (args.hugeLexMemLoad > 0)
            {
                Logger.WriteLine("Build feature lexical dictionary in huge mode[shrink when mem used rate:{0}%]", args.hugeLexMemLoad);
            }

            Logger.WriteLine("Open and check training corpus and templates...");
            var modelWriter = new ModelWriter(args.threads_num, args.C,
                args.hugeLexMemLoad, args.strRetrainModelFileName);

            if (modelWriter.Open(args.strTemplateFileName, args.strTrainingCorpus) == false)
            {
                Logger.WriteLine("Open training corpus or template file failed.");
                return false;
            }

            Logger.WriteLine("Load training data and generate lexical features: ");
            var xList = modelWriter.ReadAllRecords();

            Logger.WriteLine("");

            Logger.WriteLine("Shrinking feature set [frequency is less than {0}]...", args.min_feature_freq);
            modelWriter.Shrink(xList, args.min_feature_freq);

            Logger.WriteLine("Saving model meta data...");
            if (!modelWriter.SaveModelMetaData(args.strEncodedModelFileName))
            {
                Logger.WriteLine(Logger.Level.err, "Failed!");
                return false;
            }
            else
            {
                Logger.WriteLine("Success");
            }

            Logger.WriteLine("Indexing feature set with {0} maximum slot usage rate threshold...", args.slot_usage_rate_threshold);
            if (!modelWriter.BuildFeatureSetIntoIndex(args.strEncodedModelFileName, args.slot_usage_rate_threshold, args.debugLevel))
            {
                Logger.WriteLine(Logger.Level.err, "Failed!");
                return false;
            }
            else
            {
                Logger.WriteLine("Success");
            }

            Logger.WriteLine("Sentences size: " + xList.Length);
            Logger.WriteLine("Features size:  " + modelWriter.feature_size());
            Logger.WriteLine("Thread(s): " + args.threads_num);
            Logger.WriteLine("Regularization type: " + args.regType.ToString());
            Logger.WriteLine("Freq:                " + args.min_feature_freq);
            Logger.WriteLine("eta:                 " + args.min_diff);
            Logger.WriteLine("C:                   " + args.C);
            Logger.WriteLine("Vector quantization: " + args.bVQ);

            if (xList.Length == 0)
            {
                Logger.WriteLine(Logger.Level.err, "No sentence for training.");
                return false;
            }

            var orthant = false;
            if (args.regType == REG_TYPE.L1)
            {
                orthant = true;
            }
            if (runCRF(xList, modelWriter, orthant, args) == false)
            {
                Logger.WriteLine(Logger.Level.warn, "Some warnings are raised during encoding...");
            }

            Logger.WriteLine("Saving model feature's weight...");
            modelWriter.SaveFeatureWeight(args.strEncodedModelFileName, args.bVQ);

            return true;
        }

        bool runCRF(EncoderTagger[] x, ModelWriter modelWriter, bool orthant, EncoderArgs args)
        {
            var old_obj = double.MaxValue;
            var converge = 0;
            var lbfgs = new LBFGS(args.threads_num);
            lbfgs.expected = new double[modelWriter.feature_size() + 1];

            var processList = new List<CRFEncoderThread>();
            var parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = args.threads_num;

            //Initialize encoding threads
            for (var i = 0; i < args.threads_num; i++)
            {
                var thread = new CRFEncoderThread();
                thread.start_i = i;
                thread.thread_num = args.threads_num;
                thread.x = x;
                thread.lbfgs = lbfgs;
                thread.Init();
                processList.Add(thread);
            }

            //Statistic term and result tags frequency
            var termNum = 0;
            int[] yfreq;
            yfreq = new int[modelWriter.y_.Count];
            for (int index = 0; index < x.Length; index++)
            {
                var tagger = x[index];
                termNum += tagger.word_num;
                for (var j = 0; j < tagger.word_num; j++)
                {
                    yfreq[tagger.answer_[j]]++;
                }
            }

            //Iterative training
            var startDT = DateTime.Now;
            var dMinErrRecord = 1.0;
            for (var itr = 0; itr < args.max_iter; ++itr)
            {
                //Clear result container
                lbfgs.obj = 0.0f;
                lbfgs.err = 0;
                lbfgs.zeroone = 0;

                Array.Clear(lbfgs.expected, 0, lbfgs.expected.Length);

                var threadList = new List<Thread>();
                for (var i = 0; i < args.threads_num; i++)
                {
                    var thread = new Thread(processList[i].Run);
                    thread.Start();
                    threadList.Add(thread);
                }

                int[,] merr;
                merr = new int[modelWriter.y_.Count, modelWriter.y_.Count];
                for (var i = 0; i < args.threads_num; ++i)
                {
                    threadList[i].Join();
                    lbfgs.obj += processList[i].obj;
                    lbfgs.err += processList[i].err;
                    lbfgs.zeroone += processList[i].zeroone;

                    //Calculate error
                    for (var j = 0; j < modelWriter.y_.Count; j++)
                    {
                        for (var k = 0; k < modelWriter.y_.Count; k++)
                        {
                            merr[j, k] += processList[i].merr[j, k];
                        }
                    }
                }

                long num_nonzero = 0;
                var fsize = modelWriter.feature_size();
                var alpha = modelWriter.alpha_;
                if (orthant == true)
                {
                    //L1 regularization
                    Parallel.For<double>(1, fsize + 1, parallelOption, () => 0, (k, loop, subtotal) =>
                    {
                        subtotal += Math.Abs(alpha[k] / modelWriter.cost_factor_);
                        if (alpha[k] != 0.0)
                        {
                            Interlocked.Increment(ref num_nonzero);
                        }
                        return subtotal;
                    },
                   (subtotal) => // lock free accumulator
                   {
                       double initialValue;
                       double newValue;
                       do
                       {
                           initialValue = lbfgs.obj; // read current value
                           newValue = initialValue + subtotal;  //calculate new value
                       }
                       while (initialValue != Interlocked.CompareExchange(ref lbfgs.obj, newValue, initialValue));
                   });
                }
                else
                {
                    //L2 regularization
                    num_nonzero = fsize;
                    Parallel.For<double>(1, fsize + 1, parallelOption, () => 0, (k, loop, subtotal) =>
                   {
                       subtotal += (alpha[k] * alpha[k] / (2.0 * modelWriter.cost_factor_));
                       lbfgs.expected[k] += (alpha[k] / modelWriter.cost_factor_);
                       return subtotal;
                   },
                   (subtotal) => // lock free accumulator
                   {
                       double initialValue;
                       double newValue;
                       do
                       {
                           initialValue = lbfgs.obj; // read current value
                           newValue = initialValue + subtotal;  //calculate new value
                       }
                       while (initialValue != Interlocked.CompareExchange(ref lbfgs.obj, newValue, initialValue));
                   });
                }

                //Show each iteration result
                var diff = (itr == 0 ? 1.0f : Math.Abs(old_obj - lbfgs.obj) / old_obj);
                old_obj = lbfgs.obj;

                ShowEvaluation(x.Length, modelWriter, lbfgs, termNum, itr, merr, yfreq, diff, startDT, num_nonzero, args);
                if (diff < args.min_diff)
                {
                    converge++;
                }
                else
                {
                    converge = 0;
                }
                if (itr > args.max_iter || converge == 3)
                {
                    break;  // 3 is ad-hoc
                }

                if (args.debugLevel > 0 && (double)lbfgs.zeroone / (double)x.Length < dMinErrRecord)
                {
                    var cc = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[Debug Mode] ");
                    Console.ForegroundColor = cc;
                    Logger.WriteLine("Save intermediate feature weights at current directory");

                    //Save current best feature weight into file
                    dMinErrRecord = (double)lbfgs.zeroone / (double)x.Length;
                    modelWriter.SaveFeatureWeight("feature_weight_tmp", false);
                }

                int iret;
                iret = lbfgs.optimize(alpha, modelWriter.cost_factor_, orthant);
                if (iret <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ShowEvaluation(int recordNum, ModelWriter feature_index, LBFGS lbfgs, int termNum, int itr, int[,] merr, int[] yfreq, double diff, DateTime startDT, long nonzero_feature_num, EncoderArgs args)
        {
            var ts = DateTime.Now - startDT;

            if (args.debugLevel > 1)
            {
                for (var i = 0; i < feature_index.y_.Count; i++)
                {
                    var total_merr = 0;
                    var sdict = new SortedDictionary<double, List<string>>();
                    for (var j = 0; j < feature_index.y_.Count; j++)
                    {
                        total_merr += merr[i, j];
                        var v = (double)merr[i, j] / (double)yfreq[i];
                        if (v > 0.0001)
                        {
                            if (sdict.ContainsKey(v) == false)
                            {
                                sdict.Add(v, new List<string>());
                            }
                            sdict[v].Add(feature_index.y_[j]);
                        }
                    }
                    var vet = (double)total_merr / (double)yfreq[i];
                    vet = vet * 100.0F;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("{0} ", feature_index.y_[i]);
                    Console.ResetColor();
                    Console.Write("[FR={0}, TE=", yfreq[i]);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("{0:0.00}%", vet);
                    Console.ResetColor();
                    Console.WriteLine("]");

                    var n = 0;
                    foreach (var pair in sdict.Reverse())
                    {
                        for (int index = 0; index < pair.Value.Count; index++)
                        {
                            var item = pair.Value[index];
                            n += item.Length + 1 + 7;
                            if (n > 80)
                            {
                                //only show data in one line, more data in tail will not be show.
                                break;
                            }
                            Console.Write("{0}:", item);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("{0:0.00}% ", pair.Key * 100);
                            Console.ResetColor();
                        }
                        if (n > 80)
                        {
                            break;
                        }
                    }
                    Console.WriteLine();
                }
            }

            var act_feature_rate = (double)(nonzero_feature_num) / (double)(feature_index.feature_size()) * 100.0;
            Logger.WriteLine("iter={0} terr={1:0.00000} serr={2:0.00000} diff={3:0.000000} fsize={4}({5:0.00}% act)", itr, 1.0 * lbfgs.err / termNum, 1.0 * lbfgs.zeroone / recordNum, diff, feature_index.feature_size(), act_feature_rate);
            Logger.WriteLine("Time span: {0}, Aver. time span per iter: {1}", ts, new TimeSpan(0, 0, (int)(ts.TotalSeconds / (itr + 1))));
        }
    }
}
