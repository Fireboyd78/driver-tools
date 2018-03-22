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
        public static bool BuildArchive = false;
        public static bool VerboseLog = false;
        public static bool NoFMV = false;
        public static bool ListOnly = false;
        public static bool Overwrite = false;
        
        static readonly string DefaultOutput = @".\Data\";
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
            Console.WriteLine("IMGRipper V2.0 by Fireboyd78\r\n");

            if (args.Length > 0)
            {
                bool error = false;

                foreach (var arg in args)
                {
                    if (arg.StartsWith("/") || arg.StartsWith("-"))
                    {
                        switch (arg.ToLower().TrimStart('/', '-'))
                        {
                        case "b":
                        case "build":
                            WriteVerbose("BuildArchive flag enabled.");
                            BuildArchive = true;
                            continue;
                        case "l":
                        case "listonly":
                            WriteVerbose("ListOnly flag enabled.");
                            ListOnly = true;
                            continue;
                        case "o":
                        case "overwrite":
                            WriteVerbose("Overwrite flag enabled.");
                            Overwrite = true;
                            continue;
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
                        if (BuildArchive)
                        {
                            if (String.IsNullOrEmpty(OutputDir))
                                OutputDir = Path.GetDirectoryName(InputFile);

                            IMGArchive.BuildArchive(InputFile, OutputDir);
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(OutputDir))
                            {
                                var outDir = Path.Combine(Path.GetDirectoryName(InputFile), DefaultOutput);
                                OutputDir = Path.GetFullPath(outDir);
                            }

                            if (IMGArchive.LoadLookupTable(LookupTable))
                            {
                                Console.WriteLine("Successfully loaded lookup table.");
                                IMGArchive.Unpack(InputFile, OutputDir);
                            }
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
This folder will be relative to the input file directory.
    NOTE: Files are no longer placed relative to IMGRipper's directory.

Extracted files will be extracted to [Output]\Files\.
An archive configuration file will be created in [Output].

Options:
    --[b]uild       Builds an archive using an archive configuration file.
                    The input file should be the path to the config file.
                    The archive will be placed in [Output]\Build\ folder.
    --[o]verwrite   Overwrites files if they exist.
    --[l]istonly    Prints out a list of all files inside the archive.
    --nofmv         Do not extract movie files (These tend to be very large!)
    --[v]erbose     Display verbose debugging information.");

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }
    }
}
