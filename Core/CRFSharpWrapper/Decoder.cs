/**********************************************/
/*Project: CRF#                               */
/*Author: Zhongkai Fu                         */
/*Email: fuzhongkai@gmail.com                 */
/**********************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CRFSharp;
using CRFSharp.decoder;

namespace CRFSharpWrapper
{
    public class Decoder
    {
        ModelReader _modelReader;

        /// <summary>
        /// Load encoded model from file
        /// </summary>
        /// <param name="modelFilename">
        /// The model path.
        /// </param>
        /// <returns></returns>
        public void LoadModel(string modelFilename)
        {
            _modelReader = new ModelReader(modelFilename);
            _modelReader.LoadModel();
        }

        /// <summary>
        /// Loads an encoded model using the specified delegate.
        /// Using this overload you can read the model e.g. 
        /// from network, zipped archives or other locations, as you wish.
        /// </summary>
        /// <param name="modelLoader">
        /// Allows reading the model from arbitrary formats and sources.
        /// </param>
        /// <param name="modelFilename">
        /// The model file name, as used by the given <paramref name="modelLoader"/>
        /// for file resolution.
        /// </param>
        /// <returns></returns>
        public void LoadModel(Func<string, Stream> modelLoader, string modelFilename)
        {
            this._modelReader = new ModelReader(modelLoader, modelFilename);
            _modelReader.LoadModel();
        }

        public SegDecoderTagger CreateTagger(int nbest, int this_crf_max_word_num = Utils.DEFAULT_CRF_MAX_WORD_NUM)
        {
            if (_modelReader == null)
            {
                return null;
            }

            var tagger = new SegDecoderTagger(nbest, this_crf_max_word_num);
            tagger.init_by_model(_modelReader);

            return tagger;
        }

        //Segment given text
        public int Segment(crf_seg_out[] pout, //segment result
            SegDecoderTagger tagger, //Tagger per thread
            List<List<string>> inbuf //feature set for segment
            )
        {
            var ret = 0;
            if (inbuf.Count == 0)
            {
                //Empty input string
                return Utils.ERROR_SUCCESS;
            }

            ret = tagger.reset();
            if (ret < 0)
            {
                return ret;
            }

            ret = tagger.add(inbuf);
            if (ret < 0)
            {
                return ret;
            }

            //parse
            ret = tagger.parse();
            if (ret < 0)
            {
                return ret;
            }

            //wrap result
            ret = tagger.output(pout);
            if (ret < 0)
            {
                return ret;
            }

            return Utils.ERROR_SUCCESS;
        }



        //Segment given text
        public int Segment(crf_term_out[] pout, //segment result
            DecoderTagger tagger, //Tagger per thread
            List<List<string>> inbuf //feature set for segment
            )
        {
            var ret = 0;
            if (inbuf.Count == 0)
            {
                //Empty input string
                return Utils.ERROR_SUCCESS;
            }

            ret = tagger.reset();
            if (ret < 0)
            {
                return ret;
            }

            ret = tagger.add(inbuf);
            if (ret < 0)
            {
                return ret;
            }

            //parse
            ret = tagger.parse();
            if (ret < 0)
            {
                return ret;
            }

            //wrap result
            ret = tagger.output(pout);
            if (ret < 0)
            {
                return ret;
            }

            return Utils.ERROR_SUCCESS;
        }
    }
}
