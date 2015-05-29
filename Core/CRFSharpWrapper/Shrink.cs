using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;

namespace CRFSharpWrapper
{
    public class Shrink
    {
        public void Process(string strModelFileName, string strShrinkedModelFileName, int thread_num_ = 1)
        {
            var sr = new StreamReader(strModelFileName);
            string strLine;

            //读入版本号
            strLine = sr.ReadLine();
            var version = uint.Parse(strLine.Split(':')[1].Trim());
            if (version == CRFSharp.Utils.MODEL_TYPE_SHRINKED)
            {
                Console.WriteLine("The input model has been shrinked");
                return;
            }

            //读入cost_factor
            strLine = sr.ReadLine();
            var cost_factor_ = double.Parse(strLine.Split(':')[1].Trim());

            //读入maxid
            strLine = sr.ReadLine();
            var maxid_ = long.Parse(strLine.Split(':')[1].Trim());

            //读入xsize
            strLine = sr.ReadLine();
            var xsize_ = uint.Parse(strLine.Split(':')[1].Trim());

            //读入空行
            strLine = sr.ReadLine();

            //读入待标注的标签
            var y_ = new List<string>();
            while (true)
            {
                strLine = sr.ReadLine();
                if (strLine.Length == 0)
                {
                    break;
                }
                y_.Add(strLine);
            }

            //读入unigram和bigram模板
            var unigram_templs_ = new List<string>();
            var bigram_templs_ = new List<string>();
            while (sr.EndOfStream == false)
            {
                strLine = sr.ReadLine();
                if (strLine.Length == 0)
                {
                    break;
                }
                if (strLine[0] == 'U')
                {
                    unigram_templs_.Add(strLine);
                }
                if (strLine[0] == 'B')
                {
                    bigram_templs_.Add(strLine);
                }
            }
            sr.Close();


            //Load all features alpha data
            var filename_alpha = strModelFileName + ".alpha";
            var filename_shrink_alpha = strShrinkedModelFileName + ".alpha";
            var sr_alpha = new StreamReader(filename_alpha);
            var br_alpha = new BinaryReader(sr_alpha.BaseStream);

            var sw_alpha = new StreamWriter(filename_shrink_alpha);
            var bw_alpha = new BinaryWriter(sw_alpha.BaseStream);
            long shrinked_alpha_size = 0;

            //Only reserve non-zero feature weights and save them into file as two-tuples format
            var alpha_ = new FixedBigArray<double>(maxid_ + 1, 0);
            for (long i = 0; i < maxid_; i++)
            {
                alpha_[i] = br_alpha.ReadSingle();
                if (alpha_[i] != 0)
                {
                    bw_alpha.Write(i);
                    bw_alpha.Write((float)alpha_[i]);
                    shrinked_alpha_size++;
                }
            }

            br_alpha.Close();
            bw_alpha.Close();

            //Only reserved lexical feature whose weights is non-zero
            var varValue = new VarBigArray<int>(1024);
            var varFeature = new VarBigArray<string>(1024);
            var feaCnt = 0;
            var filename_feature = strModelFileName + ".feature.raw_text";
            var sr_fea = new StreamReader(filename_feature);
            while (sr_fea.EndOfStream == false)
            {
                strLine = sr_fea.ReadLine();
                var items = strLine.Split('\t');
                var strFeature = items[0];
                var key = int.Parse(items[1]);
                var size = (strFeature[0] == 'U' ? y_.Count : y_.Count * y_.Count);
                var hasAlpha = false;
                for (var i = key; i < key + size; i++)
                {
                    if (alpha_[i] != 0)
                    {
                        hasAlpha = true;
                        break;
                    }
                }

                if (hasAlpha == true)
                {
                    varFeature[feaCnt] = strFeature;
                    varValue[feaCnt] = key;
                    feaCnt++;
                }

            }
            sr_fea.Close();

            Console.WriteLine("Shrink feature size from {0} to {1}", maxid_, shrinked_alpha_size);
            maxid_ = shrinked_alpha_size;

            //Build new lexical feature
            var val = new FixedBigArray<int>(feaCnt, 0);
            var fea = new FixedBigArray<string>(feaCnt, 0);
            for (var i = 0; i < feaCnt; i++)
            {
                fea[i] = varFeature[i];
                val[i] = varValue[i];
            }
            varFeature = null;
            varValue = null;
            var da = new DoubleArrayTrieBuilder(thread_num_);
            if (da.build(fea, val, 0.95) == false)
            {
                Console.WriteLine("Build lexical dictionary failed.");
                return;
            }
            da.save(strShrinkedModelFileName + ".feature");

            var tofs = new StreamWriter(strShrinkedModelFileName);

            // header
            tofs.WriteLine("version: " + CRFSharp.Utils.MODEL_TYPE_SHRINKED);
            tofs.WriteLine("cost-factor: " + cost_factor_);
            tofs.WriteLine("maxid: " + maxid_);
            tofs.WriteLine("xsize: " + xsize_);

            tofs.WriteLine();

            // y
            for (var i = 0; i < y_.Count; ++i)
            {
                tofs.WriteLine(y_[i]);
            }
            tofs.WriteLine();

            // template
            for (var i = 0; i < unigram_templs_.Count; ++i)
            {
                tofs.WriteLine(unigram_templs_[i]);
            }
            for (var i = 0; i < bigram_templs_.Count; ++i)
            {
                tofs.WriteLine(bigram_templs_[i]);
            }

            tofs.Close();
        }
    }
}
