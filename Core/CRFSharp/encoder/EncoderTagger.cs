using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using AdvUtils;

namespace CRFSharp
{
    public class EncoderTagger : Tagger
    {
        public ModelWritter feature_index_;
        public short[] answer_;

        public int eval(int[,] merr)
        {
            int err = 0;
            for (int i = 0; i < word_num; ++i)
            {
                if (answer_[i] != result_[i])
                {
                    ++err;
                    merr[answer_[i], result_[i]]++;
                }
            }
            return err;
        }

        public EncoderTagger(ModelWritter modelWriter)
        {
            feature_index_ = modelWriter;
            ysize_ = (short)feature_index_.ysize();
        }

        public bool GenerateFeature(List<List<string>> recordList)
        {
            word_num = (short)recordList.Count;
            if (word_num == 0)
            {
                return false;
            }

            //Try to find each record's answer tag
            int x_num = 0;
            int xsize = (int)feature_index_.xsize_;
            answer_ = new short[word_num];
            foreach (List<string> record in recordList)
            {
                //get result tag's index and fill answer
                for (short k = 0; k < ysize_; ++k)
                {
                    if (feature_index_.y(k) == record[xsize])
                    {
                        answer_[x_num] = k;
                        break;
                    }
                }
                x_num++;
            }

            //Build record feature set
            x_ = recordList;
            Z_ = 0.0;
            feature_cache_ = new List<long[]>();
            feature_index_.BuildFeatures(this);
            x_ = null;

            return true;
        }

        private void LockFreeAdd(double[] expected, long exp_offset, double addValue)
        {
            double initialValue;
            double newValue;
            do
            {
                initialValue = expected[exp_offset]; // read current value
                newValue = initialValue + addValue;  //calculate new value
            }
            while (initialValue != Interlocked.CompareExchange(ref expected[exp_offset], newValue, initialValue));
        }

        private void calcExpectation(int x, int y, double[] expected)
        {
            Node n = node_[x, y];
            double c = Math.Exp(n.alpha + n.beta - n.cost - Z_);
            int offset = y + 1; //since expected array is based on 1
            foreach (long item in feature_cache_[n.fid])
            {
                LockFreeAdd(expected, item + offset, c);
            }

            foreach (CRFSharp.Path p in n.lpathList)
            {
                c = Math.Exp(p.lnode.alpha + p.cost + p.rnode.beta - Z_);
                offset = p.lnode.y * ysize_ + p.rnode.y + 1; //since expected array is based on 1
                foreach (long item in feature_cache_[p.fid])
                {
                    LockFreeAdd(expected, item + offset, c);
                }
            }
        }

        public double gradient(double[] expected)
        {
            buildLattice();
            forwardbackward();
            double s = 0.0;

            for (int i = 0; i < word_num; ++i)
            {
                for (int j = 0; j < ysize_; ++j)
                {
                    calcExpectation(i, j, expected);
                }
            }

            for (int i = 0; i < word_num; ++i)
            {
                short answer_val = answer_[i];
                Node answer_Node = node_[i, answer_val];
                int offset = answer_val + 1; //since expected array is based on 1
                foreach (long fid in feature_cache_[answer_Node.fid])
                {
                    LockFreeAdd(expected, fid + offset, -1.0f);
                }
                s += answer_Node.cost;  // UNIGRAM cost


                foreach (CRFSharp.Path lpath in answer_Node.lpathList)
                {
                    if (lpath.lnode.y == answer_[lpath.lnode.x])
                    {
                        offset = lpath.lnode.y * ysize_ + lpath.rnode.y + 1;
                        foreach (long fid in feature_cache_[lpath.fid])
                        {
                            LockFreeAdd(expected, fid + offset, -1.0f);
                        }

                        s += lpath.cost;  // BIGRAM COST
                        break;
                    }
                }
            }

            viterbi();  // call for eval()
            return Z_ - s;
        }

        public void Init(short[] result, Node[,] node)
        {
            result_ = result;
            node_ = node;
        }




        public void buildLattice()
        {
            RebuildFeatures();
            for (int i = 0; i < word_num; ++i)
            {
                for (int j = 0; j < ysize_; ++j)
                {
                    Node node_i_j = node_[i, j];
                    node_i_j.cost = calcCost(node_i_j.fid, j);
                    foreach (CRFSharp.Path p in node_i_j.lpathList)
                    {
                        int offset = p.lnode.y * ysize_ + p.rnode.y;
                        p.cost = calcCost(p.fid, offset);
                    }
                }
            }
        }

        public double calcCost(int featureListIdx, int offset)
        {
            double c = 0.0f;
            offset++; //since alpha_ array is based on 1
            foreach (int fid in feature_cache_[featureListIdx])
            {
                c += feature_index_.alpha_[fid + offset];
            }
            return feature_index_.cost_factor_ * c;
        }
    }
}
