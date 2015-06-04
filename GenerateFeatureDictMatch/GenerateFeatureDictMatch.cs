using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;
using CRFSharp;

namespace GenerateFeatureDictMatch
{
    public class GenerateFeatureDictMatch : IGenerateFeature
    {
        DictMatch dictmatch = null;
        List<Lemma> dm_r;
        List<int> dm_offsetList;

        // DictMatchFileName: lexical dictionary file name
        // BinaryDict: true - above dictionary file is binary format, otherwise, it is raw text
        const string KEY_LEXICAL_DICT_FILE_NAME = "DictMatchFileName";
        const string KEY_BINARY_DICT_TYPE = "BinaryDict";

        private Dictionary<string, string> LoadConfigFile(string strFileName)
        {
            var dict = new Dictionary<string,string>();

            var sr = new StreamReader(strFileName);
            while (sr.EndOfStream == false)
            {
                var strLine = sr.ReadLine();
                var items = strLine.Split('=');

                items[0] = items[0].ToLower().Trim();
                items[1] = items[1].ToLower().Trim();
                if (items[0] != KEY_LEXICAL_DICT_FILE_NAME.ToLower() &&
                    items[0] != KEY_BINARY_DICT_TYPE.ToLower())
                {
                    throw new Exception("Invalidate configuration file item");
                }
                
                dict.Add(items[0], items[1]);
            }
            sr.Close();

            return dict;
        }

        /// <summary>
        /// Initialize DictMatch Feature Generator
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            dictmatch = new DictMatch();
            dm_r = new List<Lemma>();
            dm_offsetList = new List<int>();

            Dictionary<string, string> configDict;
            configDict = LoadConfigFile("GenerateFeatureDictMatch.ini");

            if (configDict.ContainsKey(KEY_LEXICAL_DICT_FILE_NAME.ToLower()) == false ||
                configDict.ContainsKey(KEY_BINARY_DICT_TYPE.ToLower()) == false)
            {
                return false;
            }

            var strDictMatchFileName = configDict[KEY_LEXICAL_DICT_FILE_NAME.ToLower()];
            var bBinaryDict = bool.Parse(configDict[KEY_BINARY_DICT_TYPE.ToLower()]);

            if (strDictMatchFileName.Length == 0)
            {
                return true;
            }

            if (bBinaryDict == true)
            {
                dictmatch.LoadDictFromBinary(strDictMatchFileName);
            }
            else
            {
                dictmatch.LoadDictFromRawText(strDictMatchFileName);
            }
            return true;
        }

        public List<List<string>> GenerateFeature(string strText)
        {
            var rstListList = new List<List<string>>();
            if (dictmatch == null)
            {
                return rstListList;
            }

            dm_r.Clear();
            dm_offsetList.Clear();
            dictmatch.Search(strText, ref dm_r, ref dm_offsetList, DictMatch.DM_OUT_FMM);

            string [] astrDictMatch;
            astrDictMatch = new string[strText.Length];

            for (var i = 0; i < dm_r.Count; i++)
            {
                var offset = dm_offsetList[i];
                var len = (int)dm_r[i].len;

                for (var j = offset; j < offset + len; j++)
                {
                    astrDictMatch[j] = dm_r[i].strProp;
                }
            }

            for (var i = 0;i < strText.Length;i++)
            {
                rstListList.Add(new List<string>());
                rstListList[i].Add(strText[i].ToString());

                if (astrDictMatch[i] != null)
                {
                    rstListList[i].Add(astrDictMatch[i]);
                }
                else
                {
                    rstListList[i].Add("NOR");
                }
            }


            return rstListList;
        }
    }
}
