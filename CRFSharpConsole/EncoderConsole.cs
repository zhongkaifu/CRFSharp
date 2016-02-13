using System;
using CRFSharpWrapper;
using AdvUtils;

namespace CRFSharpConsole
{
    class EncoderConsole
    {
        public void Run(string [] args)
        {
            var encoder = new CRFSharpWrapper.Encoder();
            var options = new EncoderArgs();

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    var key = args[i].Substring(1).ToLower().Trim();
                    var value = "";

                    if (key == "encode")
                    {
                        continue;
                    }

                    if (key == "debug")
                    {
                        options.debugLevel = 1;

                        try
                        {
                            if (i < args.Length - 1)
                            {
                                var debugLevel = int.Parse(args[i + 1]);
                                options.debugLevel = debugLevel;
                                i++;
                            }
                        }
                        catch (Exception){}
                    }
                    else if (i < args.Length - 1)
                    {
                        i++;
                        value = args[i];
                        switch (key)
                        {
                            case "template":
                                options.strTemplateFileName = value;
                                break;
                            case "trainfile":
                                options.strTrainingCorpus = value;
                                break;
                            case "modelfile":
                                options.strEncodedModelFileName = value;
                                break;
                            case "maxiter":
                                options.max_iter = int.Parse(value);
                                break;
                            case "minfeafreq":
                                options.min_feature_freq = int.Parse(value);
                                break;
                            case "mindiff":
                                options.min_diff = double.Parse(value);
                                break;
                            case "thread":
                                options.threads_num = int.Parse(value);
                                break;
                            case "costfactor":
                                options.C = double.Parse(value);
                                break;
                            case "slotrate":
                                options.slot_usage_rate_threshold = double.Parse(value);
                                break;
                            case "hugelexmem":
                                options.hugeLexMemLoad = uint.Parse(value);
                                break;
                            case "retrainmodel":
                                options.strRetrainModelFileName = value;
                                break;
                            case "vq":
                                options.bVQ = (int.Parse(value) != 0) ? true : false;
                                break;
                            case "regtype":
                                if (value.ToLower().Trim() == "l1")
                                {
                                    options.regType = CRFSharpWrapper.Encoder.REG_TYPE.L1;
                                }
                                else if (value.ToLower().Trim() == "l2")
                                {
                                    options.regType = CRFSharpWrapper.Encoder.REG_TYPE.L2;
                                }
                                else
                                {
                                    Logger.WriteLine("Invalidated regularization type");
                                    Usage();
                                    return;
                                }
                                break;
                            default:
                                var cc = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Logger.WriteLine("No supported {0} parameter, exit", key);
                                Console.ForegroundColor = cc;
                                Usage();
                                return;
                        }
                    }
                    else
                    {
                        var cc = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Logger.WriteLine("{0} is invalidated parameter.", key);
                        Console.ForegroundColor = cc;
                        Usage();
                        return;
                    }
                }
            }

            if (options.strTemplateFileName == null || options.strEncodedModelFileName == null || options.strTrainingCorpus == null)
            {
                Usage();
                return;
            }

            if (options.threads_num <= 0)
            {
                options.threads_num = Environment.ProcessorCount;
            }

            bool bRet;
            bRet = encoder.Learn(options);
        }

        private static void Usage()
        {
            Console.WriteLine("Linear-chain CRF encoder & decoder by Zhongkai Fu (fuzhongkai@gmail.com)");
            Console.WriteLine("CRFSharpConsole.exe -encode [parameters list]");
            Console.WriteLine("\t-template <string> : template file name");
            Console.WriteLine("\t-trainfile <string> : training corpus file name");
            Console.WriteLine("\t-modelfile <string> : encoded model file name");
            Console.WriteLine("\t-maxiter <int> : The maximum encoding iteration. Default value is 1000");
            Console.WriteLine("\t-minfeafreq <int> : Any feature's frequency is less than the value will be dropped. Default value is 2");
            Console.WriteLine("\t-mindiff <float> : If diff is less than the value consecutive 3 times, the encoding will be ended. Default value is 0.0001");
            Console.WriteLine("\t-thread <int> : the amount of threads for encoding. Default value is 1");
            Console.WriteLine("\t-slotrate <float> : the maximum slot usage rate threshold when building feature set. it is ranged in (0.0, 1.0). the higher value takes longer time to build feature set, but smaller feature set size.  Default value is 0.95");
            Console.WriteLine("\t-regtype <string> : regularization type (L1 and L2). L1 will generate a sparse model. Default is L2");
            Console.WriteLine("\t-hugelexmem <int> : build lexical dictionary in huge mode and shrinking start when used memory reaches this value. This mode can build more lexical items, but slowly. Value ranges [1,100] and default is disabled.");
            Console.WriteLine("\t-retrainmodel <string> : the existed model for re-training.");
            Console.WriteLine("\t-vq <int> : vector quantization value (0/1). Default value is 1");
            Console.WriteLine("\t-debug <int> : debug level, default value is 1");
            Console.WriteLine("\t               0 - no debug information output");
            Console.WriteLine("\t               1 - only output raw lexical dictionary for feature set");
            Console.WriteLine("\t               2 - full debug information output, both raw lexical dictionary and detailed encoded information for each iteration");
            Console.WriteLine();
            Console.WriteLine("Note: either -maxiter reaches setting value or -mindiff reaches setting value in consecutive three times, the training process will be finished and saved encoded model.");
            Console.WriteLine("Note: -hugelexmem is only used for special task, and it is not recommended for common task, since it costs lots of time for memory shrink in order to load more lexical features into memory");
            Console.WriteLine();
            Console.WriteLine("A command line example as follows:");
            Console.WriteLine("\tCRFSharpConsole.exe -encode -template template.1 -trainfile ner.train -modelfile ner.model -maxiter 100 -minfeafreq 1 -mindiff 0.0001 -thread 4 -debug 2 -vq 1 -slotrate 0.95");
        }
    }
}
