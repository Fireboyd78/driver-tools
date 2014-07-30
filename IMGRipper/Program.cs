using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMGRipper
{
    class Program
    {
        public static bool VerboseLog = false;
        public static bool NoFMV = false;

        static readonly string DefaultOutput = @".\Unpack\";
        static readonly string LookupTable = @".\magicnums.txt";
        
        static string InputFile { get; set; }
        static string OutputDir { get; set; }

        #region WriteVerbose methods
        public static void WriteVerbose(string value)
        {
            if (VerboseLog)
                Console.WriteLine(value);
        }

        public static void WriteVerbose(string format, params object[] arg)
        {
            if (VerboseLog)
                Console.WriteLine(format, arg);
        }
        #endregion

        static void Main(string[] args)
        {
            Console.WriteLine("IMGRipper V1.0 by CarLuver69\r\n");

            if (args.Length > 0)
            {
                bool error = false;

                foreach (var arg in args)
                {
                    if (arg.StartsWith("/") || arg.StartsWith("-"))
                    {
                        switch (arg.ToLower().TrimStart('/', '-'))
                        {
                        case "v":
                        case "verbose":
                            WriteVerbose("Verbose logging enabled.");
                            VerboseLog = true;
                            continue;
                        case "nofmv":
                            WriteVerbose("NoFMV flag enabled.");
                            NoFMV = true;
                            continue;
                        default:
                            Console.WriteLine("WARNING: Unknown argument '{0}'", arg);
                            continue;
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(InputFile))
                            InputFile = arg;
                        else if (String.IsNullOrEmpty(OutputDir))
                            OutputDir = arg;
                        else
                        {
                            Console.WriteLine("ERROR: Too many arguments specified.");
                            error = true;
                            break;
                        }
                    }
                }

                if (error)
                {
                    Console.WriteLine("Terminating....");
                    return;
                }

                if (!String.IsNullOrEmpty(InputFile))
                {
                    if (File.Exists(InputFile))
                    {
                        if (String.IsNullOrEmpty(OutputDir))
                            OutputDir = Path.Combine(Path.GetFullPath(DefaultOutput), Path.GetFileNameWithoutExtension(InputFile));

                        // Load the lookup table
                        if (IMGFile.LoadLookupTable(LookupTable))
                        {
                            Console.WriteLine("Successfully loaded lookup table.");
                            IMGFile.Unpack(InputFile, OutputDir);
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: The specified file does not exist.");
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: No input file was specified.");
                }
            }
            else
            {
                Console.WriteLine(
@"Usage: imgripper --[options] [input file] [:output folder]

If no output folder is specified, a default folder is used.
This folder is usually located where IMGRipper resides.

Options:
    --nofmv         Do not extract .FMV files (These tend to be large files)
    --[v]erbose     Display verbose debugging information");

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }
    }
}
