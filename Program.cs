using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Collections;
using System.IO;
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
               Console.WriteLine("===Start merging===");
               MergeMKV(options.mkvPath, options.InputFile, options.OutputFile);
               Console.WriteLine("OK!");
            }
        }
        static void EasyConvert(string X264Path, string InputFile, string OutputFile)
        {
            //only video MKV. Without subs and audio.
            string cmd = String.Format("\"{0}\" --preset veryfast --tune animation --crf 18 -o \"{1}\" \"{2}\"", X264Path, OutputFile, InputFile);
#if DEBUG
            Console.WriteLine("Execute command: {0}", cmd);
#endif
            
            Process proc = new Process {
                StartInfo = new ProcessStartInfo
                {
                    FileName="cmd.exe",
                    Arguments=String.Format("/C \"{0}\"", cmd),
                    UseShellExecute=false,
                    CreateNoWindow=true,
                    RedirectStandardOutput = true
                }
            };
            proc.Start();
            Console.WriteLine("Converting...");
            while (!proc.StandardOutput.EndOfStream) ;
            }
        static void MergeMKV(string MKVPath, string InputFile, string OutputFile)
        {
            string TempFile = @"temp.mkv";
            string cmd = String.Format("\"{0}\\mkvmerge.exe\" \"{1}\" --no-video \"{2}\" --output \"{3}\" --ui-language en ", MKVPath,OutputFile,InputFile,TempFile);
#if DEBUG
            Console.WriteLine(cmd);
#endif
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
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            File.Delete(OutputFile);
            Console.WriteLine("Deleted {0}", OutputFile);
            File.Copy(TempFile, OutputFile);
            Console.WriteLine("Copied {0} to {1}", TempFile, OutputFile);
            File.Delete(TempFile);
            Console.WriteLine("Deleted {0}", TempFile);
            
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
        static void ExtractContent(string MKVPath, string FilePath, out int VIDEO_ID, out string[] ExtractedContentFilenames,out string[] ExtractedTypes)
        {
            VIDEO_ID = -1;//if no video in MKV
            ArrayList filenames = new ArrayList();
            //
            string[] tracks = GetTracksInfo(MKVPath, FilePath);
            string[] exts = null;
            string[] types = null;
            
            ParseTracksInfo(tracks, out exts, out types);
            
            string _args = "";
            for (int i = 0; i < tracks.Length; ++i)
            {
                string filename=String.Format("{0}{1}.{2}",i,types[i],exts[i]);
                filenames.Add(filename);
                string param = String.Format("{0}:{1}", i, filename);
                
                if (types[i] == "video")
                    VIDEO_ID = i;

                _args += param + " ";
                
            }
            _args = '"'+MKVPath + "\\mkvextract.exe\" tracks "+'"'+FilePath +'"'+' '+ _args+" --ui-language en";
            Console.WriteLine(_args);
            ExtractedContentFilenames = (string[])filenames.ToArray(typeof(string));
            ExtractedTypes = types;
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
