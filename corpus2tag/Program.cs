using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using CRFSharp;

/*
 * convert raw corpus to crfpp training data format, a example as follows:
 * raw corpus format:
 * 付仲恺[PER] 和[NOR] 张晓晨[PER] 一起[NOR] 在家[NOR] 看[NOR] 非诚勿扰[VDO_TVSHOW] 。[NOR]
 * crfpp training data format
 * 付 B_PER
 * 仲 M_PER
 * 恺 E_PER
 * 和 S
 * 张 B_PER
 * 晓 M_PER
 * 晨 E_PER
 * 一 B
 * 起 E
 * 在 B
 * 家 E
 * 看 S
 * 非 B_VDO_TVSHOW
 * 诚 M_VDO_TVSHOW
 * 勿 M_VDO_TVSHOW
 * 扰 E_VDO_TVSHOW
 * 。 S
*/
namespace corpus2tag
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("corpus2tag [raw corpus filename] [type mapping filename]");
                return;
            }


            //Load feature generator for external dll files
            IGenerateFeature featureGenerator = null;
            if (Properties.Settings.Default.FeatureGeneratorDLL.Length > 0 &&
                File.Exists(Properties.Settings.Default.FeatureGeneratorDLL) == true)
            {
                Console.WriteLine("Loading external DLL: {0}", Properties.Settings.Default.FeatureGeneratorDLL);
                Assembly ass = Assembly.LoadFrom(Properties.Settings.Default.FeatureGeneratorDLL);
                if (ass == null)
                {
                    Console.WriteLine("Load {0} failed.", Properties.Settings.Default.FeatureGeneratorDLL);
                    return;
                }
                featureGenerator = ass.CreateInstance(Properties.Settings.Default.FeatureGeneratorNamespace) as IGenerateFeature;
                if (featureGenerator == null)
                {
                    Console.WriteLine("Create instance {0} failed.", Properties.Settings.Default.FeatureGeneratorNamespace);
                    return;
                }

                featureGenerator.Initialize();
            }
            else
            {
                featureGenerator = new GenerateFeatureDefault.GenerateFeatureDefault();
                featureGenerator.Initialize();
            }
            if (featureGenerator == null)
            {
                Console.WriteLine("No feature generator or load feature generator failed.");
                return;
            }

            Dictionary<string, int> mappedTag2num = new Dictionary<string, int>();
            Dictionary<string, int> orignalTag2num = new Dictionary<string, int>();

            Dictionary<string, string> t2t = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(args[0]);
            StreamWriter sw = new StreamWriter(args[0] + ".tag");
            StreamWriter sw_f = new StreamWriter(args[0] + ".filtered");
            StreamReader swmap = new StreamReader(args[1]);

            //Load tag mapping file
            while (swmap.EndOfStream == false)
            {
                string strLine = swmap.ReadLine().Trim();
                string [] items = strLine.Split(new char[]{'\t'});

                if (items.Length != 2)
                {
                    Console.WriteLine("{0} is incorrect format", strLine);
                    continue;
                }

                if (t2t.ContainsKey(items[0]) == false)
                {
                    t2t.Add(items[0], items[1]);
                }
                else
                {
                    Console.WriteLine("{0} is duplicated", items[0]);
                }
            }
            swmap.Close();

            //Read each line and convert it into CRFSharp training format
            int LineNo = 0;
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine().Trim();
                LineNo++;

                try
                {
                    string strFilterLine = "";
                    if (strLine.Length == 0)
                    {
                        continue;
                    }

                    //Split the record into token list
                    string[] items = strLine.Split();
                    List<List<string>> tagListList = new List<List<string>>();

                    foreach (string item in items)
                    {
                        //Check whether the data format is correct
                        if (item[item.Length - 1] != ']')
                        {
                            Console.WriteLine("{0} is invalidated format at Line #{1}", strLine, LineNo);
                            break;
                        }

                        string term = "", tag = "";
                        int pos = item.LastIndexOf('[');
                        if (pos >= 0)
                        {
                            term = item.Substring(0, pos).ToLower();
                            tag = item.Substring(pos + 1, item.Length - pos - 2);
                        }
                        else
                        {
                            //Check whether the data format is correct
                            Console.WriteLine("{0} is invalidated format at Line #{1}", strLine, LineNo);
                            break;
                        }

                        if (term.Length == 0 || tag.Length == 0)
                        {
                            Console.WriteLine("Invalidate line: {0} at #{1}", strLine, LineNo);
                            break;
                        }

                        if (orignalTag2num.ContainsKey(tag) == false)
                        {
                            orignalTag2num.Add(tag, 1);
                        }
                        else
                        {
                            orignalTag2num[tag]++;
                        }

                        string ftag;
                        if (t2t.ContainsKey(tag) == true)
                        {
                            ftag = t2t[tag];
                        }
                        else
                        {
                            ftag = "NOR";
                        }

                        strFilterLine = strFilterLine + term + "[" + ftag + "] ";

                        //Generate combined and mapped CRF tag with word position information
                        tag = ftag;
                        if (tag == "NOR")
                        {
                            tag = "";
                        }
                        for (int i = 0; i < term.Length; i++)
                        {
                            string tag2;

                            if (term.Length == 1)
                            {
                                tag2 = "S";
                            }
                            else if (i == 0)
                            {
                                tag2 = "B";
                            }
                            else if (i == term.Length - 1)
                            {
                                tag2 = "E";
                            }
                            else
                            {
                                tag2 = "M";
                            }

                            if (tag.Length > 0)
                            {
                                tag2 = tag2 + "_" + tag;
                            }

                            if (mappedTag2num.ContainsKey(tag2) == false)
                            {
                                mappedTag2num.Add(tag2, 1);
                            }
                            else
                            {
                                mappedTag2num[tag2]++;
                            }

                            tagListList.Add(new List<string>());
                            tagListList[tagListList.Count - 1].Add(term[i].ToString());
                            tagListList[tagListList.Count - 1].Add(tag2);
                        }
                    }

                    sw_f.WriteLine(strFilterLine);


                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < tagListList.Count; i++)
                    {
                        sb.Append(tagListList[i][0]);
                    }

                    List<List<string>> featureListList;
                    featureListList = featureGenerator.GenerateFeature(sb.ToString());
                    if (tagListList.Count != featureListList.Count)
                    {
                        Console.WriteLine("Generate Feature invalidated. Error: {0}", strLine);
                    }

                    for (int i = 0; i < tagListList.Count; i++)
                    {
                        string strRstLine = tagListList[i][0];
                        if (tagListList[i][0] != featureListList[i][0])
                        {
                            Console.WriteLine("Character feature is not equal.");
                        }

                        for (int j = 1; j < featureListList[i].Count; j++)
                        {
                            strRstLine = strRstLine + "\t" + featureListList[i][j];
                        }
                        strRstLine = strRstLine + "\t" + tagListList[i][1];

                        sw.WriteLine(strRstLine);
                    }


                    sw.WriteLine();
                }
                catch (SystemException err)
                {
                    Console.WriteLine("Invalidated Line: {0} at #{1}", strLine, LineNo);
                    Console.WriteLine("Error Message: {0}", err.Message);
                    Console.WriteLine("Stack Info: {0}", err.StackTrace);
                    return;
                }
            }

            Console.WriteLine("Orignal Tags Statistics:");
            foreach (KeyValuePair<string, int> pair in orignalTag2num)
            {
                Console.WriteLine("{0}\t{1}", pair.Key, pair.Value);
            }

            Console.WriteLine("Mapped Tag Statistics:");
            foreach (KeyValuePair<string, int> pair in mappedTag2num)
            {
                Console.WriteLine("{0}\t{1}", pair.Key, pair.Value);
            }
            sr.Close();
            sw.Close();
            sw_f.Close();
        }
    }
}
