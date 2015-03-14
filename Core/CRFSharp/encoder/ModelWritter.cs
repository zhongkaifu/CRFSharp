using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;

#if NO_SUPPORT_PARALLEL_LIB
#else
using System.Collections.Concurrent;
using System.Threading.Tasks;
#endif

namespace CRFSharp
{
    public class ModelWritter : BaseModel
    {
        int thread_num_;
        public IFeatureLexicalDict featureLexicalDict;
        List<List<List<string>>> trainCorpusList;
        Object thisLock = new object();

#if NO_SUPPORT_PARALLEL_LIB
#else
        ParallelOptions parallelOption = new ParallelOptions();
#endif

        public ModelWritter(int thread_num, double cost_factor, uint hugeLexShrinkMemLoad)
        {
            cost_factor_ = cost_factor;
            maxid_ = 0;
            thread_num_ = thread_num;

#if NO_SUPPORT_PARALLEL_LIB
#else
            parallelOption.MaxDegreeOfParallelism = thread_num;
#endif

            if (hugeLexShrinkMemLoad > 0)
            {
                featureLexicalDict = new HugeFeatureLexicalDict(thread_num_, hugeLexShrinkMemLoad);
            }
            else
            {
                featureLexicalDict = new DefaultFeatureLexicalDict(thread_num_);
            }
        }

        //Regenerate feature id and shrink features with lower frequency
        public void Shrink(EncoderTagger[] xList, int freq)
        {
            BTreeDictionary<long, long> old2new = new BTreeDictionary<long, long>();
            featureLexicalDict.Shrink(freq);
            maxid_ = featureLexicalDict.RegenerateFeatureId(old2new, y_.Count);
            int feature_count = xList.Length;

            //Update feature ids
#if NO_SUPPORT_PARALLEL_LIB
            for (int i = 0;i < feature_cache_.Count;i++)
#else
            Parallel.For(0, feature_count, parallelOption, i =>
#endif
            {
                for (int j = 0; j < xList[i].feature_cache_.Count; j++)
                {
                    List<long> newfs = new List<long>();
                    long rstValue = 0;
                    foreach (long v in xList[i].feature_cache_[j])
                    {
                        if (old2new.TryGetValue(v, out rstValue) == true)
                        {
                            newfs.Add(rstValue);
                        }
                    }
                    xList[i].feature_cache_[j] = newfs.ToArray();
                }
            }
#if NO_SUPPORT_PARALLEL_LIB
#else
            );
#endif

            Console.WriteLine("Feature size in total : {0}", maxid_);
        }

        //Load all records and generate features
        public EncoderTagger[] ReadAllRecords()
        {
            EncoderTagger[] arrayEncoderTagger = new EncoderTagger[trainCorpusList.Count];
            int arrayEncoderTaggerSize = 0;

            //Generate each record features
#if NO_SUPPORT_PARALLEL_LIB
            for (int i = 0;i < trainCorpusList.Count;i++)
#else
            Parallel.For(0, trainCorpusList.Count, parallelOption, i =>
#endif
            {
                EncoderTagger _x = new EncoderTagger(this);
                if (_x.GenerateFeature(trainCorpusList[i]) == false)
                {
                    Console.WriteLine("Load a training sentence failed, skip it.");
                }
                else
                {
                    int oldValue = Interlocked.Increment(ref arrayEncoderTaggerSize) - 1;
                    arrayEncoderTagger[oldValue] = _x;

                    if (oldValue % 10000 == 0)
                    {
                        //Show current progress on console
                        Console.Write("{0}...", oldValue);
                    }
                }
            }
#if NO_SUPPORT_PARALLEL_LIB
#else
            );
#endif

            trainCorpusList.Clear();
            trainCorpusList = null;

            Console.WriteLine();
            return arrayEncoderTagger;
        }

        //Open and check training and template file
        public bool Open(string strTemplateFileName, string strTrainCorpusFileName)
        {
            return OpenTemplateFile(strTemplateFileName) && OpenTrainCorpusFile(strTrainCorpusFileName);
        }

        //Build feature set into indexed data
        public bool BuildFeatureSetIntoIndex(string filename, double max_slot_usage_rate_threshold, int debugLevel, string strRetrainModelFileName)
        {
            Console.WriteLine("Building {0} features into index...", featureLexicalDict.Size);

            IList<string> keyList;
            IList<int> valList;
            featureLexicalDict.GenerateLexicalIdList(out keyList, out valList);

            if (debugLevel > 0)
            {
                Console.Write("Debug: Writing raw feature set into file...");
                string filename_featureset_raw_format = filename + ".feature.raw_text";
                StreamWriter sw = new StreamWriter(filename_featureset_raw_format);
                // save feature and its id into lists in raw format
                for (int i = 0; i < keyList.Count; i++)
                {
                    sw.WriteLine("{0}\t{1}", keyList[i], valList[i]);
                }
                sw.Close();
                Console.WriteLine("Done.");
            }

            //Build feature index
            string filename_featureset = filename + ".feature";
            DoubleArrayTrieBuilder da = new DoubleArrayTrieBuilder(thread_num_);
            if (da.build(keyList, valList, max_slot_usage_rate_threshold) == false)
            {
                Console.WriteLine("Build lexical dictionary failed.");
                return false;
            }
            //Save indexed feature set into file
            da.save(filename_featureset);

            if (strRetrainModelFileName == null || strRetrainModelFileName.Length == 0)
            {
                //Clean up all data
                featureLexicalDict.Clear();
                featureLexicalDict = null;
                keyList = null;
                valList = null;

                GC.Collect();

                //Create weight matrix
                alpha_ = new double[feature_size() + 1];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Loading the existed model for re-training...");
                //Create weight matrix
                alpha_ = new double[feature_size() + 1];

                ModelReader modelReader = new ModelReader();
                modelReader.LoadModel(strRetrainModelFileName);

                if (modelReader.y_.Count == y_.Count)
                {
                    for (int i = 0; i < keyList.Count; i++)
                    {
                        int index = modelReader.get_id(keyList[i]);
                        if (index < 0)
                        {
                            continue;
                        }
                        int size = (keyList[i][0] == 'U' ? y_.Count : y_.Count * y_.Count);
                        for (int j = 0; j < size; j++)
                        {
                            alpha_[valList[i] + j + 1] = modelReader.GetAlpha(index + j);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("The number of tags isn't equal between two models, it cannot be re-trained.");
                }

                //Clean up all data
                featureLexicalDict.Clear();
                featureLexicalDict = null;
                keyList = null;
                valList = null;

                GC.Collect();
            }

            return true;
        }

        //Save model meta data into file
        public bool SaveModelMetaData(string filename)
        {
            StreamWriter tofs = new StreamWriter(filename);

            // header
            tofs.WriteLine("version: " + Utils.MODEL_TYPE_NORM);
            tofs.WriteLine("cost-factor: " + cost_factor_);
            tofs.WriteLine("maxid: " + maxid_);
            tofs.WriteLine("xsize: " + xsize_);

            tofs.WriteLine();

            // y
            for (int i = 0; i < y_.Count; ++i)
            {
                tofs.WriteLine(y_[i]);
            }
            tofs.WriteLine();

            // template
            for (int i = 0; i < unigram_templs_.Count; ++i)
            {
                tofs.WriteLine(unigram_templs_[i]);
            }
            for (int i = 0; i < bigram_templs_.Count; ++i)
            {
                tofs.WriteLine(bigram_templs_[i]);
            }

            tofs.Close();

            return true;
        }


        public bool SaveFeatureWeight(string filename)
        {
            string filename_alpha = filename + ".alpha";
            StreamWriter tofs = new StreamWriter(filename_alpha, false);
            BinaryWriter bw = new BinaryWriter(tofs.BaseStream);

            for (long i = 1; i <= maxid_; ++i)
            {
                bw.Write((float)alpha_[i]);
            }
  
            bw.Close();
            return true;
        }

        bool OpenTemplateFile(string filename)
        {
            StreamReader ifs = new StreamReader(filename);
            unigram_templs_ = new List<string>();
            bigram_templs_ = new List<string>();
            while (ifs.EndOfStream == false)
            {
                string line = ifs.ReadLine();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }
                if (line[0] == 'U')
                {
                    unigram_templs_.Add(line);
                }
                else if (line[0] == 'B')
                {
                    bigram_templs_.Add(line);
                }
                else
                {
                    Console.WriteLine("unknown type: {0}", line);
                }
            }
            ifs.Close();
            return true;
        }

        bool OpenTrainCorpusFile(string strTrainingCorpusFileName)
        {
            StreamReader ifs = new StreamReader(strTrainingCorpusFileName);
            y_ = new List<string>();
            trainCorpusList = new List<List<List<string>>>();
            HashSet<string> hashCand = new HashSet<string>();
            List<List<string>> recordList = new List<List<string>>();

            int last_xsize = -1;
            while (ifs.EndOfStream == false)
            {
                string line = ifs.ReadLine();
                if (line.Length == 0 || line[0] == ' ' || line[0] == '\t')
                {
                    //Current record is finished, save it into the list
                    if (recordList.Count > 0)
                    {
                        trainCorpusList.Add(recordList);
                        recordList = new List<List<string>>();
                    }
                    continue;
                }

                string[] items = line.Split('\t');
                int size = items.Length;
                if (last_xsize >= 0 && last_xsize != size)
                {
                    return false;
                }
                last_xsize = size;
                xsize_ = (uint)(size - 1);
                recordList.Add(new List<string>(items));

                if (hashCand.Contains(items[items.Length - 1]) == false)
                {
                    hashCand.Add(items[items.Length - 1]);
                    y_.Add(items[items.Length - 1]);
                }
            }
            ifs.Close();

            Console.WriteLine("Training corpus size: {0}", trainCorpusList.Count);
            return true;
        }

        //Get feature id from feature set by feature string
        //If feature string is not existed in the set, generate a new id and return it
        public bool BuildFeatures(EncoderTagger tagger)
        {
            List<long> feature = new List<long>();

            //tagger.feature_id_ = tagger.feature_cache_.Count;
            for (int cur = 0; cur < tagger.word_num; ++cur)
            {
                foreach (string it in unigram_templs_)
                {
                    string strFeature = apply_rule(it, cur, tagger);
                    if (strFeature == "")
                    {
                        Console.WriteLine(" format error: " + it);
                    }

                    long id = featureLexicalDict.GetOrAddId(strFeature);
                    feature.Add(id);
                }
                tagger.feature_cache_.Add(feature.ToArray());
                feature.Clear();
            }

            for (int cur = 1; cur < tagger.word_num; ++cur)
            {
                foreach (string it in bigram_templs_)
                {
                    string strFeature = apply_rule(it, cur, tagger);
                    if (strFeature == "")
                    {
                        Console.WriteLine(" format error: " + it);
                    }
                    long id = featureLexicalDict.GetOrAddId(strFeature);
                    feature.Add(id);
                }

                tagger.feature_cache_.Add(feature.ToArray());
                feature.Clear();

            }

            return true;
        }

    }
}
