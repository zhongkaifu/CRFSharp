using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRFSharpWrapper
{
    public class EncoderArgs
    {
        public int max_iter = 1000; //maximum iteration, when encoding iteration reaches this value, the process will be ended.
        public int min_feature_freq = 2; //minimum feature frequency, if one feature's frequency is less than this value, the feature will be dropped.
        public double min_diff = 0.0001; //minimum diff value, when diff less than the value consecutive 3 times, the process will be ended.
        public double slot_usage_rate_threshold = 0.95; //the maximum slot usage rate threshold when building feature set.
        public int threads_num = 1; //the amount of threads used to train model.
        public CRFSharpWrapper.Encoder.REG_TYPE regType = CRFSharpWrapper.Encoder.REG_TYPE.L2; //regularization type
        public string strTemplateFileName = null; //template file name
        public string strTrainingCorpus = null; //training corpus file name
        public string strEncodedModelFileName = null; //encoded model file name
        public string strRetrainModelFileName = null; //the model file name for re-training
        public int debugLevel = 0; //Debug level
        public uint hugeLexMemLoad = 0;
        public double C = 1.0; //cost factor, too big or small value may lead encoded model over tune or under tune
        public bool bVQ = true; //If we build vector quantization model for feature weights
    }

    public class DecoderArgs
    {
        public string strModelFileName;
        public string strInputFileName;
        public string strOutputFileName;
        public string strOutputSegFileName;
        public int nBest;
        public int thread;
        public int probLevel;
        public int maxword;

        public DecoderArgs()
        {
            thread = 1;
            nBest = 1;
            probLevel = 0;
            maxword = 100;
        }
    }
}
