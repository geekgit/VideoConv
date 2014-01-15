using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
namespace VideoConv
{
    class Program
    {
        class Options
        {
            [Option('i', Required = true, HelpText = "Input file.")]
            public string InputFile { get; set; }
            [Option('o', Required = true, HelpText = "Output file.")]
            public string OutputFile { get; set; }
            [Option("x264_path", Required = true,
                HelpText = @"Full path to x264 (8 bit) executable. See http://download.videolan.org/pub/x264/binaries/" )]
            public string x264Path { get; set; }
            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("Input: \"{0}\"", options.InputFile);
                Console.WriteLine("Output: \"{0}\"", options.OutputFile);
                Console.WriteLine("x264 path: \"{0}\"", options.x264Path);

                EasyConvert(options.x264Path, options.InputFile, options.OutputFile);
            }
        }
        static void EasyConvert(string X264Path, string InputFile,string OutputFile)
        {
            //only video MKV. Without subs and audio.
            string cmd = String.Format("\"{0}\" --preset veryfast --tune animation --crf 18 -o \"{1}\" \"{2}\"", X264Path, OutputFile, InputFile);
            Console.WriteLine("Execute command: {0}",cmd);
            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = String.Format("/C \"{0}\"", cmd);
            proc.Start();

        }
    }
}
