using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using AdvUtils;

#if NO_SUPPORT_PARALLEL_LIB
#else
using System.Threading.Tasks;
#endif

namespace CRFSharp
{
    public class DefaultFeatureLexicalDict : IFeatureLexicalDict
    {
        BTreeDictionary<string, FeatureIdPair> featureset_dict_;
        long maxid_;
        Object thisLock = new object();
#if NO_SUPPORT_PARALLEL_LIB
#else
        ParallelOptions parallelOption;
#endif

        public DefaultFeatureLexicalDict(int thread_num)
        {
            featureset_dict_ = new BTreeDictionary<string, FeatureIdPair>(StringComparer.Ordinal, 128);
            maxid_ = 0;
#if NO_SUPPORT_PARALLEL_LIB
#else
            parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = thread_num;
#endif
        }

        public void Clear()
        {
            featureset_dict_.Clear();
            featureset_dict_ = null;
        }

        public long Size
        {
            get
            {
                return featureset_dict_.Count;
            }
        }

        public void Shrink(int freq)
        {
            int i = 0;
            while (i < featureset_dict_.Count)
            {
                if (featureset_dict_.ValueList[i].Value < freq)
                {
                    //If the feature's frequency is less than specific frequency, drop the feature.
                    featureset_dict_.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void GenerateLexicalIdList(out IList<string> keyList, out IList<int> valList)
        {
            keyList = featureset_dict_.KeyList;
            int [] fixArrayValue = new int[Size];
            valList = fixArrayValue;

#if NO_SUPPORT_PARALLEL_LIB
            for (int i = 0;i < featureset_dict_.ValueList.Count;i++)
#else
            Parallel.For(0, featureset_dict_.ValueList.Count, parallelOption, i =>
#endif
            {
                fixArrayValue[i] = (int)featureset_dict_.ValueList[i].Key;
            }
#if NO_SUPPORT_PARALLEL_LIB
#else
);
#endif

        }

        public long RegenerateFeatureId(BTreeDictionary<long, long> old2new, long ysize)
        {
            long new_maxid = 0;
            //Regenerate new feature id and create feature ids mapping
            foreach (KeyValuePair<string, FeatureIdPair> it in featureset_dict_)
            {
                string strFeature = it.Key;
                //Regenerate new feature id
                old2new.Add(it.Value.Key, new_maxid);
                it.Value.Key = new_maxid;

                long addValue = (strFeature[0] == 'U' ? ysize : ysize * ysize);
                new_maxid += addValue;
            }

            return new_maxid;
        }

        //Get feature id from feature set by feature string
        //If feature string is not existed in the set, generate a new id and return it
        private long GetId(string key)
        {
            FeatureIdPair pair;
            if (featureset_dict_.TryGetValue(key, out pair) == true)
            {
                return pair.Key;
            }

            return Utils.ERROR_INVALIDATED_FEATURE;
        }

        public long GetOrAddId(string key)
        {
            FeatureIdPair pair;
            if (featureset_dict_.TryGetValue(key, out pair) == true && pair != null)
            {
                //Find its feature id
                System.Threading.Interlocked.Increment(ref pair.Value);
            }
            else
            {
                lock (thisLock)
                {
                    if (featureset_dict_.TryGetValue(key, out pair) == true)
                    {
                        System.Threading.Interlocked.Increment(ref pair.Value);
                    }
                    else
                    {
                        long oldValue = Interlocked.Increment(ref maxid_) - 1;
                        pair = new FeatureIdPair(oldValue, 1);
                        featureset_dict_.Add(key, pair);
                    }
                }
            }
            return pair.Key;
        }
    }
}
