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
            var rstListList = new List<List<string>>();
            for (int index = 0; index < strText.Length; index++)
            {
                var ch = strText[index];
                rstListList.Add(new List<string>());
                rstListList[rstListList.Count - 1].Add(ch.ToString());
            }

            return rstListList;
        }
    }
}
