using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;

namespace CRFSharp
{
    public class ModelReader : BaseModel
    {
        public uint version; //模型版本号,读取模型时读入
        private DoubleArrayTrieSearch da; //特征集合
        private BTreeDictionary<long, double> alpha_two_tuples;

        //获取key对应的特征id
        public int get_id(string str)
        {
            return da.SearchByPerfectMatch(str);
        }

        //加载model文件
        //返回值<0 为出错，=0为正常
        public bool LoadModel(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            string strLine;


            //读入版本号
            strLine = sr.ReadLine();
            version = uint.Parse(strLine.Split(':')[1].Trim());

            //读入cost_factor
            strLine = sr.ReadLine();
            cost_factor_ = double.Parse(strLine.Split(':')[1].Trim());

            //读入maxid
            strLine = sr.ReadLine();
            maxid_ = long.Parse(strLine.Split(':')[1].Trim());

            //读入xsize
            strLine = sr.ReadLine();
            xsize_ = uint.Parse(strLine.Split(':')[1].Trim());

            //读入空行
            strLine = sr.ReadLine();

            //读入待标注的标签
            y_ = new List<string>();
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
            unigram_templs_ = new List<string>();
            bigram_templs_ = new List<string>();
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

            //Load all feature set data
            string filename_feature = filename + ".feature";
            da = new DoubleArrayTrieSearch();
            da.Load(filename_feature);


            //Load all features alpha data
            string filename_alpha = filename + ".alpha";
            StreamReader sr_alpha = new StreamReader(filename_alpha);
            BinaryReader br_alpha = new BinaryReader(sr_alpha.BaseStream);

            if (version == Utils.MODEL_TYPE_NORM)
            {
                //feature weight array
                alpha_two_tuples = null;
                alpha_ = new double[maxid_ + 1];
                for (long i = 0; i < maxid_; i++)
                {
                    alpha_[i] = br_alpha.ReadSingle();
                }
            }
            else if (version == Utils.MODEL_TYPE_SHRINKED)
            {
                alpha_ = null;
                alpha_two_tuples = new BTreeDictionary<long, double>();
                for (long i = 0; i < maxid_; i++)
                {
                    long key = br_alpha.ReadInt64();
                    double weight = br_alpha.ReadSingle();
                    alpha_two_tuples.Add(key, weight);
                }
            }
            else
            {
                Console.WriteLine("This model is not supported.");
                return false;
            }
            br_alpha.Close();
            return true;
        }

        public double GetAlpha(long index)
        {
            if (alpha_ != null)
            {
                return alpha_[index];
            }
            double weight = 0.0f;
            alpha_two_tuples.TryGetValue(index, out weight);

            return weight;
        }
    }
}
