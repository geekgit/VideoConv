using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Collections;
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
            [Option("mkv_path", Required = true,
                HelpText = @"Full path to MKVToolNix. See http://www.bunkus.org/videotools/mkvtoolnix/downloads.html" )]
            public string mkvPath { get; set; }
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
                Console.WriteLine("MKVToolNix path: \"{0}\"", options.mkvPath);
                string[] tracks=GetTracksInfo(options.mkvPath,options.InputFile);
                Console.WriteLine("===Tracks info===");
                foreach (string track in tracks)
                {
                    Console.WriteLine(track);
                }
                Console.WriteLine("===Start converting===");
                EasyConvert(options.x264Path, options.InputFile, options.OutputFile);
            }
        }
        static void EasyConvert(string X264Path, string InputFile, string OutputFile)
        {
            //sort of async
            //only video MKV. Without subs and audio.
            string cmd = String.Format("\"{0}\" --preset veryfast --tune animation --crf 18 -o \"{1}\" \"{2}\"", X264Path, OutputFile, InputFile);
            Console.WriteLine("Execute command: {0}", cmd);
            Process proc = new Process {
                StartInfo = new ProcessStartInfo
                {
                    FileName="cmd.exe",
                    Arguments=String.Format("/C \"{0}\"", cmd),
                    UseShellExecute=false,
                    CreateNoWindow=true
                }
            };
            proc.Start();
        }
        static void ParseTracksInfo(string[] tracks,out string[] exts,out string[] types)
        {
            exts = new string[tracks.Length];
            types = new string[tracks.Length];
                for(int i=0;i<tracks.Length;++i)
                {
                    string track = tracks[i];
                    string[] elements = track.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < elements.Length; ++j)
                        {
                            elements[j] = (elements[j].TrimEnd()).TrimStart();
                        }
                    string id_part = elements[0];
                    string[] id_elements = id_part.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int ID = int.Parse(id_elements[id_elements.Length - 1]);
                    string codec_part = elements[1];
                    string[] codec_elements = codec_part.Split(new char[] { ' ','_','/','(',')' }, StringSplitOptions.RemoveEmptyEntries);
                    string type = codec_elements[0];
                    string ext = codec_elements[codec_elements.Length - 1];
                    types[i] = type;
                    exts[i] = ext;
                }
        }
        static void ExtractContent(string MKVPath, string FilePath)
        {
            string[] tracks = GetTracksInfo(MKVPath, FilePath);
            string[] exts = null;
            string[] types = null;
            
            ParseTracksInfo(tracks, out exts, out types);
            string _args = "";
            for (int i = 0; i < tracks.Length; ++i)
            {
                string param = String.Format("{0}:{0}{1}.{2}", i, types[i], exts[i]);
                _args += param + " ";
                
            }
            _args = '"'+MKVPath + "\\mkvextract.exe\" tracks "+'"'+FilePath +'"'+' '+ _args+" --ui-language en";
            Console.WriteLine(_args);
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = String.Format("/C \"{0}\"", _args),
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
        }
        static string[] GetTracksInfo(string MKVPath, string FilePath)
        {
            //get tracks into
            string cmd = String.Format("\"{0}\\mkvmerge.exe\" -i --ui-language en \"{1}\"", MKVPath,FilePath);
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = String.Format("/C \"{0}\"", cmd),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            ArrayList al = new ArrayList();
            StringBuilder sb = new StringBuilder();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                if (!line.Contains("Track ID")) continue;
                else al.Add(line);
            }
            return (string[])al.ToArray(typeof(string));
            
        }
    }
}
