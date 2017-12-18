﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Audiose
{
    public enum FileType
    {
        Other,

        BinaryData,
        Xml,

        Blk, // PS1 block data
        Sbk, // PS1 soundbank data
    }

    static class Config
    {
        public static string Input { get; set; }
        public static FileType InputType { get; set; }

        public static string OutDir { get; set; }

        public static bool Compile { get; set; }
        public static bool Extract { get; set; }

        static readonly string[] m_format = {
            "Usage: {0}",
            "",
            "Options:",
            "{1}",
        };

        static readonly string[] m_usage = {
            "[:-options] <input> <:output>",
            "  If no output folder is specified, the input's directory is used.",
        };

        static readonly string[] m_options = {
            "  -[c]ompile       Compiles all sound data into a binary format.",
            "                   Use this if you wish to /re/compile binary data instead of dumping it.",
            "                   This will be ignored if you pass in an XML file!",

            "  -e[x]tract       Extracts all sound data from a packaged format and splits them into their own files.",
            "                   Only use this if you know what you're doing!",
        };

        static readonly string[] m_complete_msg = {
            "All done, you're good to go!",
            "Done, now go try it out!",
            "Success! What else did you expect?",
            "0P3rAtiOn c0mPL3t3d SuCc3ssFu11Y.",
            "Failed to cause an error: Operation completed successfully.",
            "These success messages are absolutely cringeworthy, just saying!",
            "Wait, why did you-- nevermind, I don't care.",
            "Bye bye!",
            "See-ya, sucker! :D",
        };

        public static string UsageString
        {
            get
            {
                var format = String.Join("\r\n", m_format);

                return String.Format(format,
                    String.Join("\r\n", m_usage),
                    String.Join("\r\n", m_options));
            }
        }

        public static string VersionString
        {
            get { return $"v{BuildVersion.ToString()}-{m_types[BuildType]}"; }
        }

        public static string GetSuccessMessage()
        {
            var seed = (int)(DateTime.Now.ToBinary() * ~0xF12EB12D);
            var rand = new Random(seed);

            var idx = rand.Next(0, m_complete_msg.Length - 1);
            return m_complete_msg[idx];
        }

        static readonly string[] m_types = {
            "dev",
            "rel",
        };

        public static readonly int BuildType;
        public static readonly Version BuildVersion;

        public static readonly CultureInfo Culture = new CultureInfo("en-US", false);

        static Config()
        {
            BuildVersion = Assembly.GetExecutingAssembly().GetName().Version;

            BuildType =
#if RELEASE
                    1;
#else
                    0;
#endif
        }
        
        static FileType GetFileType(string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();

            switch (ext)
            {
            case ".gsd":
            case ".sp":
                return FileType.BinaryData;
            case ".xml":
                return FileType.Xml;

            // PS1 sounds
            case ".blk":
                return FileType.Blk;
            case ".sbk":
                return FileType.Sbk;
            }

            return FileType.Other;
        }

        public static bool ProcessArgs(string[] args)
        {
            // current index for non-option arguments
            var argIdx = 0;

            foreach (ArgInfo arg in args)
            {
                if (arg.IsSwitch)
                {
                    switch (arg.Name)
                    {
                    case "c":
                    case "compile":
                        Compile = true;
                        continue;
                    case "x":
                    case "extract":
                        Extract = true;
                        continue;
                    default:
                        Console.WriteLine($"WARNING: Unknown argument '{arg.ToString()}', skipping...");
                        break;
                    }
                }
                else
                {
                    switch (argIdx++)
                    {
                    case 0:
                        Input = arg.Value;
                        InputType = GetFileType(Input);
                        continue;
                    case 1:
                        OutDir = arg.Value;
                        continue;
                    }

                    Console.WriteLine($"WARNING: Too many arguments found ('{arg}'), ignoring further arguments.");
                    break;
                }
            }

            // initialize paths
            if (!String.IsNullOrEmpty(Input))
            {
                // make sure the input file exists, or else we can't work with it
                if (!File.Exists(Input))
                {
                    Console.Error.WriteLine($"ERROR: The specified file '{Input}' could not be found.");
                    return false;
                }
            }
            else
            {
                // nice
                Console.Error.WriteLine("ERROR: No input file specified!");
                return false;
            }

            // all done
            return true;
        }
    }
}