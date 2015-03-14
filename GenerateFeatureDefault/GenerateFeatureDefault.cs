using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CRFSharp;

namespace GenerateFeatureDefault
{
    public class GenerateFeatureDefault : IGenerateFeature
    {

        public bool Initialize()
        {

            return true;
        }

        public List<List<string>> GenerateFeature(string strText)
        {
            List<List<string>> rstListList = new List<List<string>>();
            foreach (char ch in strText)
            {
                rstListList.Add(new List<string>());
                rstListList[rstListList.Count - 1].Add(ch.ToString());
            }

            return rstListList;
        }
    }
}
