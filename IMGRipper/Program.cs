using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        public static bool SuperHasher9000 = false;
        
        static readonly string DefaultOutput = @".\Data\";
        static readonly string LookupTable = @".\magicnums.txt";
        
        static string InputFile { get; set; }
        static string OutputDir { get; set; }

        static void RunSuperHasher9000TM()
        {
            Console.WriteLine("<<< SUPER HASHER 9000(tm) >>>");

            var readLine = new Func<string>(() => {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("> ");

                Console.ResetColor();

                return Console.ReadLine();
            });

            var respond = new Action<string>((s) => {
                Console.WriteLine($"{s}");
            });

            var line = "";
            var endItAll = false;

            while (!endItAll && (line = readLine()) != null)
            {
                if (line.Length == 0)
                {
                    respond("Please enter something.");
                    continue;
                }

                switch (line[0])
                {
                case '%':
                    respond($"Hasher mode: {(CustomHasher.PSPMode ? "Driver76" : "Jenkins")}");
                    continue;
                case '?':
                {
                    var sb = new StringBuilder();
                    var num = 0;

                    foreach (var hash in CustomHasher.Missing.OrderBy((k) => $"{k:X8}"))
                    {
                        if ((num % 10) == 0.0)
                        {
                            if (num > 0)
                                sb.AppendLine();

                            sb.Append($" - ");
                        }

                        sb.Append($"{hash:X8} ");
                        num++;
                    }

                    sb.AppendLine();

                    if (num > 0)
                    {
                        respond($"{num} unknown hashes:");
                        respond(sb.ToString());
                    }
                    else
                    {
                        respond("No missing hashes loaded.");
                    }
                } continue;
                case '@':
                {
                    var file = line.Substring(1).Replace("\"", "");

                    respond("Loading...");

                    try
                    {
                        var img = new IMGArchive();

                        img.LoadFile(file);

                        // setup the hasher method
                        CustomHasher.PSPMode = (img.Version == IMGVersion.PSP) ? true : false;

                        respond("Done.");
                    }
                    catch (Exception e)
                    {
                        respond($"Error: {e.Message}");
                    }
                } continue;
                default:
                {
                    var hash = 0u;

                    if (line[0] == '$')
                    {
                        try
                        {
                            hash = uint.Parse(line.Substring(1), NumberStyles.HexNumber);
                        }
                        catch (Exception)
                        {
                            respond("Invalid hexadecimal hash.");
                            continue;
                        }
                    }
                    else
                    {
                        switch (line)
                        {
                        case "help":
                            respond("I'm afraid I can't do that, Dave.");
                            continue;
                        case "exit":
                            endItAll = true;
                            respond("Bye!");
                            continue;
                        }

                        hash = CustomHasher.GetHash(line);
                    }

                    if (CustomHasher.Lookup.ContainsKey(hash))
                    {
                        respond($"{hash:X8} : '{line}'");
                    }
                    else
                    {
                        if (CustomHasher.Missing.Contains(hash))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("You found a missing hash!");

                            Console.ResetColor();
                        }

                        respond($"{hash:X8} : (unknown)");
                    }
                } continue;
                }
            }
        }

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
                        if (String.Equals(arg, "--"))
                        {
                            WriteVerbose("SuperHasher9000 enabled.");
                            SuperHasher9000 = true;
                            break;
                        }

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

                if (SuperHasher9000)
                {
                    RunSuperHasher9000TM();
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
