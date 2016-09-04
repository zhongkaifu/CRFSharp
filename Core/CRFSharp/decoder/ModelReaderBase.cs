using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;
using CRFSharp.decoder;

namespace CRFSharp
{
    public abstract class ModelReaderBase : BaseModel
    {

        public uint version; //模型版本号,读取模型时读入
        private DoubleArrayTrieSearch da; //特征集合

        //获取key对应的特征id
        public virtual int get_id(string str)
        {
            return da.SearchByPerfectMatch(str);
        }

        public virtual double GetAlpha(long index)
        {
            return alpha_[index];
        }

        /// <summary>
        /// Loads the model into memory.
        /// </summary>
        public void LoadModel()
        {
            //Load model meta data
            LoadMetadata();

            //Load all feature set data
            LoadFeatureSet();

            //Load all features alpha data
            LoadFeatureWeights();
        }

        /// <summary>
        /// Provides access to the metadata stream.
        /// </summary>
        /// <returns>
        /// A <see cref="Stream"/> instance
        /// that points to the model metadata file.
        /// </returns>
        protected abstract Stream GetMetadataStream();

        /// <summary>
        /// Provides access to the feature set stream.
        /// </summary>
        /// <returns>
        /// A <see cref="Stream"/> instance
        /// that allows accessing the model feature set file.
        /// </returns>
        protected abstract Stream GetFeatureSetStream();

        /// <summary>
        /// Provides access to the feature set stream.
        /// </summary>
        /// <returns>
        /// A <see cref="Stream"/> instance
        /// that allows accessing the model feature weight file.
        /// </returns>
        protected abstract Stream GetFeatureWeightStream();

        private void LoadMetadata()
        {
            using (Stream metadataStream = GetMetadataStream())
            {
                var sr = new StreamReader(metadataStream);
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
        }

        private void LoadFeatureSet()
        {
            Stream featureSetStream = GetFeatureSetStream();
            da = new DoubleArrayTrieSearch();
            da.Load(featureSetStream);
        }

        private void LoadFeatureWeights()
        {
            //feature weight array
            alpha_ = new double[maxid_ + 1];

            using (Stream featureWeightStream = GetFeatureWeightStream())
            {
                //Load all features alpha data
                var sr_alpha = new StreamReader(featureWeightStream);
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
        }

    }
}
