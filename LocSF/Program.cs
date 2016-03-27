using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace LocSF
{
    class Program
    {
        static readonly string DefaultOutput    = @".\Resources\";

        static readonly string LocaleFile       = "loc.sp";
        static readonly string GlobalLocaleFile = "global.fchunk";
        static readonly string SubtitlesFile    = "subtitles.astd";

        // symbols
        static readonly string S_DriverSF = DSC.Configuration["DriverSF"] as string;
        static readonly string S_Locale = $"{S_DriverSF}\\Locale";

        static readonly char[] DirectorySeparators = { '/', '\\' };

        static readonly Dictionary<string, string> ReservedSymbols = new Dictionary<string, string>() {
            { "DriverSF",   S_DriverSF  },
            { "Locale",    S_Locale   },
        };
        
        private static string ParseSymbols(string str)
        {
            var start = -1;
            var end = -1;

            var symbol = "";

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];

                switch (c)
                {
                // symbol begin
                case '$':
                    {
                        if (start != -1)
                            throw new InvalidOperationException("New symbol opened before the last one was closed!");

                        start = i;
                        continue;
                    }
                // symbol open
                case '<':
                    {
                        if (end != -1)
                            throw new InvalidOperationException("Malformed symbol definition!");

                        // after open tag
                        end = (i + 1);
                        continue;
                    };
                // symbol close
                case '>':
                    {
                        if (end == -1)
                            throw new InvalidOperationException("Malformed symbol definition!");

                        // after close tag
                        end = (i + 1);

                        // split the string up
                        var left = str.Substring(0, start);
                        var right = str.Substring(end);

                        // process the symbol
                        if (ReservedSymbols.ContainsKey(symbol))
                        {
                            return ParseSymbols(String.Concat(left, ReservedSymbols[symbol], right));
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: Symbol '{symbol}' not found!");

                            return ParseSymbols(String.Concat(left, right));
                        }
                    };
                }

                // are we reading a symbol?
                if (start != -1)
                    symbol += c;
            }

            // couldn't find any symbols to parse
            return str;
        }

        private static string GetFullFilePath(string path)
        {
            // also parse symbols
            return Path.GetFullPath(ParseSymbols(path));
        }

        static void Warning(string message)
        {
            Console.WriteLine($"WARNING: {message}");
        }

        static void Error(string message)
        {
            Console.Error.WriteLine($"ERROR: {message}");
        }

        static void Error(Exception exception)
        {
            Error(exception.Message);

            Console.Error.WriteLine("\r\n==================== Stack Trace ====================");
            Console.Error.WriteLine($"<{exception.GetType().FullName}>:");
            Console.Error.WriteLine(exception.StackTrace);
        }

        static void Terminate(int errorCode = 1)
        {
            Console.WriteLine("Terminating...");
            Environment.Exit(errorCode);
        }

        static void Terminate(string errorMessage, int errorCode = 1)
        {
            Error(errorMessage);
            Terminate(errorCode);
        }

        static string InputDirectory { get; set; }
        static string OutputDirectory { get; set; }


        static void PrepareDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        
        static void Build()
        {
            if (String.IsNullOrEmpty(OutputDirectory))
            {
                OutputDirectory = Path.Combine(Environment.CurrentDirectory,
                    Path.GetFullPath(DefaultOutput), Directory.GetParent(InputDirectory).Name, "bin");
            }

            PrepareDirectory(OutputDirectory);


        }

        static void Unpack()
        {

        }

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var build = false;

                var input = "";
                var outputDir = "";

                try
                {
                    foreach (var arg in args)
                    {
                        if (arg.StartsWith("/") || arg.StartsWith("-"))
                        {
                            switch (arg.ToLower().TrimStart('/', '-'))
                            {
                            case "b":
                            case "build":
                                build = true;
                                continue;
                            default:
                                Warning($"Unknown argument '{arg}'");
                                continue;
                            }
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(input))
                                input = GetFullFilePath(arg);
                            else if (String.IsNullOrEmpty(outputDir))
                                outputDir = GetFullFilePath(arg);
                            else
                            {
                                Terminate("Too many arguments specified.");
                                break;
                            }
                        }
                    }

                    if (build)
                    {
                        // build locale data
                        if (String.IsNullOrEmpty(input))
                            throw new InvalidOperationException("No input directory specified.");
                        if (!Directory.Exists(input))
                            throw new DirectoryNotFoundException("The specified input directory was not found.");
                    }
                    else
                    {

                    }
                }
                catch (Exception e)
                {
                    Error(e);
                }
            }
            else
            {
                PrintUsage();
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine(
@"Usage: LocSF --[options] [input] [:output]

If no output folder is specified, a default folder is used.
This folder is usually located where LocSF resides.

Options:
    --[b]uild       Builds the locale files in the specified directory.

Symbols:
    $<DriverSF>     Path to your Driver San Francisco installation.
    $<Locale>       Path to your Driver San Francisco locale folder.");
        }
    }
}
