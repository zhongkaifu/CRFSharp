using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdvUtils;

namespace CRFSharp
{
    public interface IFeatureLexicalDict
    {
        void Shrink(int freq);
        long GetOrAddId(string strFeature);
        long RegenerateFeatureId(BTreeDictionary<long, long> old2new, long ysize);
        void GenerateLexicalIdList(out IList<string> fea, out IList<int> val);
        void Clear();

        long Size
        {
            get;
        }
    }
}
