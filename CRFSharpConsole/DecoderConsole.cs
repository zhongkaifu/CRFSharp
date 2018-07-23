using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CRFSharpWrapper;
using CRFSharp;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using AdvUtils;

namespace CRFSharpConsole
{
    class DecoderConsole
    {
        public void Usage()
        {
            Console.WriteLine("CRFSharpConsole -decode <options>");
            Console.WriteLine("-modelfile <string>  : The model file used for decoding");
            Console.WriteLine("-inputfile <string>  : The input file to predict its content tags");
            Console.WriteLine("-outputfile <string> : The output file to save raw tagged result");
            Console.WriteLine("-outputsegfile <string> : The output file to save segmented tagged result");
            Console.WriteLine("-nbest <int>         : Output n-best result, default value is 1");
            Console.WriteLine("-thread <int>        : <int> threads used for decoding");
            Console.WriteLine("-prob                : output probability, default is not output");
            Console.WriteLine("                       0 - not output probability");
            Console.WriteLine("                       1 - only output the sequence label probability");
            Console.WriteLine("                       2 - output both sequence label and individual entity probability");
            Console.WriteLine("-maxword <int>       : <int> max words per sentence, default value is 100");
            Console.WriteLine("Example: ");
            Console.WriteLine("         CRFSharp_Console -decode -modelfile ner.model -inputfile ner_test.txt -outputfile ner_test_result.txt -outputsegfile ner_test_result_seg.txt -thread 4 -nbest 3 -prob 2 -maxword 500");
        }

        public void Run(string[] args)
        {
            var options = new DecoderArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    var key = args[i].Substring(1).ToLower().Trim();
                    var value = "";

                    if (key == "decode")
                    {
                        continue;
                    }
                    else if (i < args.Length - 1)
                    {
                        i++;
                        value = args[i];
                        switch (key)
                        {
                            case "outputfile":
                                options.strOutputFileName = value;
                                break;
                            case "inputfile":
                                options.strInputFileName = value;
                                break;
                            case "modelfile":
                                options.strModelFileName = value;
                                break;
                            case "outputsegfile":
                                options.strOutputSegFileName = value;
                                break;
                            case "thread":
                                options.thread = int.Parse(value);
                                break;
                            case "nbest":
                                options.nBest = int.Parse(value);
                                break;
                            case "prob":
                                options.probLevel = int.Parse(value);
                                break;
                            case "maxword":
                                options.maxword = int.Parse(value);
                                break;

                            default:
                                var cc = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("No supported {0} parameter, exit", key);
                                Console.ForegroundColor = cc;
                                Usage();
                                return;
                        }
                    }
                    else
                    {
                        var cc = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("{0} is invalidated parameter.", key);
                        Console.ForegroundColor = cc;
                        Usage();
                        return;
                    }
                }
            }

            if (options.strInputFileName == null || options.strModelFileName == null)
            {
                Usage();
                return;
            }

            new CRFSharpWrapper.DecoderDriver().Decode(options);
        }

    }
}
