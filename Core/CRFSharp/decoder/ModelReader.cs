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

        //获取key对应的特征id
        public int get_id(string str)
        {
            return da.SearchByPerfectMatch(str);
        }

        /// <summary>
        /// Load encoded model from file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public void LoadModel(string filename)
        {
            //Load model meta data
            LoadMetaData(filename);

            //Load all feature set data
            LoadFeaturSet(filename);

            //Load all features alpha data
            LoadFeatureWeights(filename);
        }

        public void LoadMetaData(string filename)
        {
            var sr = new StreamReader(filename);
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
        }

        public void LoadFeaturSet(string filename)
        {
            var filename_feature = filename + ".feature";
            da = new DoubleArrayTrieSearch();
            da.Load(filename_feature);
        }

        public void LoadFeatureWeights(string filename)
        {
            //feature weight array
            alpha_ = new double[maxid_ + 1];

            //Load all features alpha data
            var filename_alpha = filename + ".alpha";
            var sr_alpha = new StreamReader(filename_alpha);
            var br_alpha = new BinaryReader(sr_alpha.BaseStream);

            //Get VQ Size
            int vqSize = br_alpha.ReadInt32();

            if (vqSize > 0)
            {
                //This is a VQ model, we need to get code book at first
                Logger.WriteLine("This is a VQ Model. VQSize: {0}", vqSize);
                List<double> vqCodeBook = new List<double>();
                for (int i = 0; i < vqSize; i++)
                {
                    vqCodeBook.Add(br_alpha.ReadDouble());
                }

                //Load weights
                for (long i = 0; i < maxid_; i++)
                {
                    int vqIdx = br_alpha.ReadByte();
                    alpha_[i] = vqCodeBook[vqIdx];
                }
            }
            else
            {
                //This is a normal model
                Logger.WriteLine("This is a normal model.");
                for (long i = 0; i < maxid_; i++)
                {
                    alpha_[i] = br_alpha.ReadSingle();
                }
            }

            br_alpha.Close();
        }

        public double GetAlpha(long index)
        {
            return alpha_[index];
        }
    }
}
