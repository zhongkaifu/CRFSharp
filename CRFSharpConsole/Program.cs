using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRFSharpConsole
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Linear-chain CRF encoder & decoder by Zhongkai Fu (fuzhongkai@gmail.com)");
            Console.WriteLine("CRFSharpConsole [parameter list...]");
            Console.WriteLine("  -encode [parameter list...] - Encode CRF model from given training corpus");
            Console.WriteLine("  -decode [parameter list...] - Decode CRF model to label text");
            Console.WriteLine("  -shrink [parameter list...] - Shrink encoded CRF model");
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                return;
            }

            var bEncoder = false;
            var bDecoder = false;
            var bShrink = false;

            for (int index = 0; index < args.Length; index++)
            {
                var item = args[index];
                if (item.Length <= 1)
                {
                    continue;
                }

                if (item[0] != '-')
                {
                    continue;
                }

                var strType = item.Substring(1).ToLower().Trim();
                if (strType == "encode")
                {
                    bEncoder = true;
                }
                if (strType == "decode")
                {
                    bDecoder = true;
                }
                if (strType == "shrink")
                {
                    bShrink = true;
                }
            }

            //Invalidated parameter
            if (bEncoder == false && bDecoder == false && bShrink == false)
            {
                Usage();
                return;
            }

            //try
            //{
            if (bEncoder == true)
            {
                var encoderConsole = new EncoderConsole();
                encoderConsole.Run(args);
            }
            else if (bDecoder == true)
            {
                var decoderConsole = new DecoderConsole();
                decoderConsole.Run(args);
            }
            else if (bShrink == true)
            {
                var shrinkConsole = new ShrinkConsole();
                shrinkConsole.Run(args);
            }
            else
            {
                Usage();
            }
            // }
            //catch (System.AggregateException err)
            //{
            //    Console.WriteLine("Error Message : {0}", err.Message);
            //    Console.WriteLine("Call stack : {0}", err.StackTrace);
            //    Console.WriteLine("Inner Exception : {0}", err.InnerException);
            //    foreach (Exception exp in err.InnerExceptions)
            //    {
            //        Console.WriteLine("Inner Exception in Collect: {0}", exp);
            //    }
            //}
            //catch (System.Exception err)
            //{
            //    Console.WriteLine("Error Message : {0}", err.Message);
            //    Console.WriteLine("Call stack : {0}", err.StackTrace);
            //}
        }
    }
}
