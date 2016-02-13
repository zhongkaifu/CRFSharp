using System;
using AdvUtils;

namespace CRFSharpConsole
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Linear-chain CRF encoder & decoder by Zhongkai Fu (fuzhongkai@gmail.com)");
            Console.WriteLine("CRFSharpConsole.exe [parameters list...]");
            Console.WriteLine("  -encode [parameters list...] - Encode CRF model from training corpus");
            Console.WriteLine("  -decode [parameters list...] - Decode CRF model on test corpus");
        }

        static void Main(string[] args)
        {
            Logger.LogFile = "CRFSharpConsole.log";

            if (args.Length < 1)
            {
                Usage();
                return;
            }

            var bEncoder = false;
            var bDecoder = false;

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
            }

            //Invalidated parameter
            if (bEncoder == false && bDecoder == false)
            {
                Usage();
                return;
            }

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
            else
            {
                Usage();
            }
        }
    }
}
