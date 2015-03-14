using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdvUtils;

namespace CRFSharp
{
    public class BaseModel
    {
        public long maxid_;
        public double cost_factor_;

        public List<string> unigram_templs_;
        public List<string> bigram_templs_;
        
        //Labeling tag list
        public List<string> y_;
        public uint ysize() { return (uint)y_.Count; }

        //The dimension training corpus
        public uint xsize_;

        //Feature set value array
        public double[] alpha_;

        //获取类别i的字符表示
        public string y(int i) { return y_[i]; }

        public long feature_size() { return maxid_; }

        public BaseModel()
        {
            cost_factor_ = 1.0;
        }

        string get_index(string p, int pos, ref int i, Tagger tagger)
        {
            if (p[i] != '[')
            {
                return null;
            }
            i++;

            int col = 0;
            int row = 0;
            int neg = 1;

            if (p[i] == '-')
            {
                neg = -1;
                i++;
            }

            while (i < p.Length)
            {
                if (p[i] >= '0' && p[i] <= '9')
                {
                    row = 10 * row + (p[i] - '0');
                }
                else if (p[i] == ',')
                {
                    i++;
                    goto NEXT1;
                }
                else return null;

                i++;
            }

        NEXT1:
            while (i < p.Length)
            {
                if (p[i] >= '0' && p[i] <= '9')
                {
                    col = 10 * col + (p[i] - '0');
                }
                else if (p[i] == ']')
                {
                    goto NEXT2;
                }
                else return null;

                i++;
            }
        NEXT2:
            row *= neg;

            if (col < 0 || col >= xsize_)
            {
                return null;
            }
            int idx = pos + row;
            if (idx < 0)
            {
                return "_B-" + (-idx).ToString();
            }
            if (idx >= tagger.word_num)
            {
                return "_B+" + (idx - tagger.word_num + 1).ToString();
            }

            return tagger.x_[idx][col];
        }

        public string apply_rule(string p, int pos, Tagger tagger)
        {
            StringBuilder feature_function = new StringBuilder();
            string r;
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] == '%')
                {
                    i++;
                    switch (p[i])
                    {
                        case 'x':
                            i++;
                            r = get_index(p, pos, ref i, tagger);
                            if (r == null)
                            {
                                return "";
                            }
                            feature_function.Append(r);
                            break;
                        default:
                            return "";
                    }
                }
                else
                {
                    feature_function.Append(p[i]);
                }
            }

            return feature_function.ToString();
        }
    }
}
