using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

using DSCript;
using DSCript.Spooling;

using UnluacNET;

namespace LuaSF
{
    class Program
    {
        static readonly string DefaultOutput = @".\Resources\";

        static bool BigEndian { get; set; }
        static bool ListOnly { get; set; }

        // symbols
        static readonly string S_DriverSF = DSC.Configuration.GetDirectory("DriverSF");
        static readonly string S_Scripts = $"{S_DriverSF}\\media";

        static readonly string COMPrefix = "COM:luaScripts";
        static readonly string CMDPath = "cmd.exe";
        
        static readonly Dictionary<string, string> ReservedSymbols = new Dictionary<string, string>() {
            { "DriverSF",   S_DriverSF  },
            { "Scripts",    S_Scripts   },
        };

        private static ProcessStartInfo m_luacProcInfo;
        
        private static ProcessStartInfo LuacProcInfo
        {
            get
            {
                if (m_luacProcInfo == null)
                {
                    m_luacProcInfo = new ProcessStartInfo(CMDPath) {
                        WorkingDirectory        = Environment.CurrentDirectory,
                        RedirectStandardError   = true,
                        RedirectStandardInput   = true,
                        RedirectStandardOutput  = true,
                        UseShellExecute         = false,
                        WindowStyle             = ProcessWindowStyle.Hidden
                    };
                }

                return m_luacProcInfo;
            }
        }

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

        private static bool RunLuaCompiler(CompiledScript scriptData, string filename, bool useBackupOnFail)
        {
            var tmpFile = Path.GetTempFileName();
            var success = true;

            var luacPath = Path.ChangeExtension(filename, ".luac");

            // set up lua compiler
            LuacProcInfo.Arguments = String.Format("/C start \"\" /B /D \"{0}\" luac -o \"{1}\" \"{2}\" ",
                        Environment.CurrentDirectory, tmpFile, filename);

            using (var cmd = new Process() {
                StartInfo = LuacProcInfo
            })
            {
                cmd.Start();

                var output = cmd.StandardOutput;
                var stdError = cmd.StandardError;

                while (!output.EndOfStream)
                    Console.Out.WriteLine(output.ReadLine());
                while (!stdError.EndOfStream)
                {
                    // script failed to compile
                    success = false;

                    Console.Error.WriteLine();
                    Console.Error.WriteLine(@"/*=== COMPILE ERROR ===*\");

                    Console.Error.WriteLine(stdError.ReadLine());

                    Console.Error.WriteLine(@"/*======================*\");
                    Console.Error.WriteLine();
                }

                cmd.WaitForExit();

                if (success)
                {
                    var buffer = File.ReadAllBytes(tmpFile);
                    scriptData.Buffer = buffer;

                    // write to .luac file as well
                    File.WriteAllBytes(luacPath, buffer);

                    // sync filetimes
                    File.SetCreationTimeUtc(luacPath, File.GetLastWriteTimeUtc(filename));
                    File.SetLastWriteTimeUtc(luacPath, File.GetLastWriteTimeUtc(filename));
                }
                else if (useBackupOnFail)
                {
                    if (!File.Exists(luacPath))
                        throw new InvalidOperationException("Cannot compile script package - one or more scripts failed to compile (and no pre-compiled backup exists).");

                    // use the original data
                    scriptData.Buffer = File.ReadAllBytes(luacPath);
                }

                File.Delete(tmpFile);
            }

            return success;
        }

        private static string GetRelativePath(string root, string filename)
        {
            // verify the filename can actually be a part of the root
            if (filename.Length > root.Length)
            {
                var idx = filename.IndexOf(root, 0, StringComparison.CurrentCultureIgnoreCase);

                if (idx > -1)
                {
                    var relativeName = filename.Substring(idx + root.Length);

                    while (relativeName.StartsWith("\\"))
                        relativeName = relativeName.Substring(1);

                    return relativeName;
                }
            }
        
            // TODO: add error checking?
            return null;
        }

        static void Compile(string inputDir, string outputDir)
        {
            if (!Directory.Exists(inputDir))
                throw new DirectoryNotFoundException("Invalid directory input - cannot compile script package!");

            var scripts = new List<CompiledScript>();

            Console.WriteLine($"Compiling all scripts in \"{inputDir}\"...");

            foreach (var file in Directory.EnumerateFiles(inputDir, "*.lua", SearchOption.AllDirectories))
            {
                // skip luac files...
                if (file.EndsWith("luac"))
                    continue;

                var filePath = GetRelativePath(inputDir, file);

                var comPath = Path.Combine(COMPrefix, filePath).ToLower();
                var fileCRC = CRC32Hasher.GetHash(comPath);

                Console.WriteLine($" - {comPath}");

                var script = new CompiledScript() {
                    Hash = fileCRC,
                    Filename = file
                };

                scripts.Add(script);

                // put a double parenthesis to denote root directory
                filePath = $@"{inputDir}\\{filePath}";

                var luacPath = Path.ChangeExtension(filePath, ".luac");

                if (File.Exists(luacPath))
                {
                    if (File.GetLastWriteTimeUtc(filePath) > File.GetLastWriteTimeUtc(luacPath))
                    {
                        Console.WriteLine("   - New version detected, compiling...");

                        // compile if newer than .luac file
                        RunLuaCompiler(script, filePath, true);
                    }
                    else
                    {
                        // get pre-compiled buffer
                        script.Buffer = File.ReadAllBytes(luacPath);
                    }
                }
                else
                {
                    Console.WriteLine("    - No previous version detected, compiling...");

                    // there's no backup, so if this fails, it'll throw an exception
                    RunLuaCompiler(script, filePath, true);
                }
            }

            var scriptFile = new ScriptPackageFile();
            var scriptName = Path.GetFileNameWithoutExtension(inputDir);

            if (String.IsNullOrEmpty(outputDir))
                outputDir = Path.Combine(Path.GetDirectoryName(inputDir), "bin");

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            scriptFile.Scripts = scripts;
            scriptFile.Save(Path.Combine(outputDir, $"{scriptName}.fchunk"));

            Console.WriteLine();
            Console.WriteLine($"Successfully compiled \"{scriptFile.FileName}\"!");
        }

        static void Patch(string patchFile, string patchDir, string outputDir)
        {
            var cfgFile = Path.Combine(patchDir, "patch.cfg");
            
            if (!File.Exists(cfgFile))
                throw new FileNotFoundException("Patch file not found!");

            var scriptsFile = new ScriptPackageFile(patchFile, BigEndian);

            var lines = File.ReadAllLines(cfgFile);

            var start = 0;
            var end = lines.Length;

            if (lines[0].StartsWith("@"))
            {
                //comRoot = lines[0].Substring(1);
                Console.WriteLine("Patch file V1 detected, top line is obsolete");

                // skip the first line
                start++;
            }

            Console.WriteLine("Patching script package...");

            for (int i = start; i < end; i++)
            {
                var line = lines[i];
                
                // skip empty lines, comments
                if (String.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                var fileName = scriptsFile.GetScriptFilePath(line);
                
                // filename hashes use a special format
                // e.g. COM:luaScripts\loadFiles.lua
                var comPath = scriptsFile.GetCOMFilePath(fileName);

                Console.WriteLine($" - {comPath.ToLower()}");

                // make sure this file exists in the script package
                var script = scriptsFile.GetCompiledScript(comPath);

                if (script == null)
                    throw new InvalidDataException("Cannot patch file - file not found in script package!");

                // don't split the root directory yet
                var filePath = Path.Combine(patchDir, fileName);
                
                // make sure the file is in the patch directory
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Cannot find script file '{filePath}'!");
                
                // now split it at the root directory
                filePath = String.Join(@"\\", patchDir, fileName);
                
                // all our ducks are in a row, now compile it!
                RunLuaCompiler(script, filePath, false);
            }

            if (!String.IsNullOrEmpty(outputDir))
            {
                scriptsFile.Save(Path.Combine(outputDir, Path.GetFileName(scriptsFile.FileName)));
            }
            else
            {
                scriptsFile.Save();
            }

            Console.WriteLine();
            Console.WriteLine($"Successfully patched \"{scriptsFile.FileName}\"!");
        }

        static void Unpack(string input, string outputDir)
        {
            var inputDir = Path.GetDirectoryName(input);
            var inputName = Path.GetFileNameWithoutExtension(input);

            if (String.IsNullOrEmpty(outputDir))
                outputDir = Path.GetFullPath(Path.Combine(inputDir, DefaultOutput, inputName));

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            
            var scriptsFile = new ScriptPackageFile(input, BigEndian);
            var timeUTC = DateTime.UtcNow;

            Console.WriteLine("Decompiling scripts...");

            var filePaths = new List<String>();
            var dirPaths = new List<String>();

            foreach (var script in scriptsFile.Scripts)
            {
                var filename = script.Filename;
                var badName = false;
                
                Console.WriteLine($" - {script.Filename}");

                if (!String.IsNullOrEmpty(filename) && filename.IndexOf(':') == -1)
                {
                    var comPath = scriptsFile.GetCOMFilePath(filename);

                    if (scriptsFile.GetCompiledScript(comPath) != script)
                        badName = true;
                }
                else
                {
                    badName = true;
                }

                if (badName)
                {
                    filename = $"${script.Hash:X8}.lua";
                    Console.WriteLine($"  > Saving as {filename}");
                }

                using (var fB = new MemoryStream(script.Buffer))
                {
                    var luaPath = Path.Combine(outputDir, filename);
                    var luacPath = Path.ChangeExtension(luaPath, ".luac");

                    var dir = Path.GetDirectoryName(luaPath);

                    // create directory if necessary
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var sb = new StringBuilder();

                    try
                    {
                        using (var printer = new StringWriter(sb))
                        {
                            var header = new BHeader(fB);
                            var function = header.Function.Parse(fB, header);

                            var d = new Decompiler(function);

                            d.Decompile();
                            d.Print(new Output(printer));
                        }
                    }
                    catch (Exception e)
                    {
                        var err = $"DECOMPILATION FAILURE: {e}";

                        Console.WriteLine($"  > {err}");

                        sb.AppendLine($"--[[ ******** {err} ******** ]]--");
                    }
                    finally
                    {
                        File.WriteAllText(luaPath, sb.ToString());
                    }

                    // write compiled version as well
                    File.WriteAllBytes(luacPath, script.Buffer);

                    // for our timestamp stuff
                    filePaths.Add(luaPath);
                    filePaths.Add(luacPath);

                    if (!dirPaths.Contains(dir))
                        dirPaths.Add(dir);
                }
            }

            // set file timestamps
            foreach (var file in filePaths)
            {
                try
                {
                    File.SetCreationTimeUtc(file, timeUTC);
                    File.SetLastWriteTimeUtc(file, timeUTC);
                }
                catch (Exception) { /* keep moving */ }
            }

            // set directory timestamps
            foreach (var dir in dirPaths)
            {
                try
                {
                    Directory.SetCreationTimeUtc(dir, timeUTC);
                    Directory.SetLastWriteTimeUtc(dir, timeUTC);
                }
                catch (Exception) { /* keep moving */ }
            }

            Console.WriteLine();
            Console.WriteLine($"Successfully decompiled scripts to \"{outputDir}\"!");
        }
        
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var compile     = false;
                var error       = false;
                var patch       = false;

                var input       = "";
                var outputDir   = "";

                // used with "patch" argument
                // path to .fchunk file to be patched
                var patchFile   = "";

                try
                {
                    foreach (var arg in args)
                    {
                        if (arg.StartsWith("/") || arg.StartsWith("-"))
                        {
                            switch (arg.ToLower().TrimStart('/', '-').Split(':')[0])
                            {
                            case "big":
                            case "big-endian":
                                BigEndian = true;
                                continue;
                            case "compile":
                                compile = true;
                                continue;
                            case "l":
                            case "list":
                                ListOnly = true;
                                continue;
                            case "patch":
                                patch = true;

                                var idx = arg.IndexOf(':');

                                if (idx == -1)
                                {
                                    Console.Error.WriteLine("ERROR: Invalid patch argument. The colon before the filename is missing!");
                                    Console.Error.WriteLine("       See program usage for more details.");
                                    error = true;
                                }

                                patchFile = GetFullFilePath(arg.Substring(idx + 1));
                                continue;
                            default:
                                Console.WriteLine("WARNING: Unknown argument '{0}'", arg);
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
                                Console.Error.WriteLine("ERROR: Too many arguments specified.");
                                error = true;
                                break;
                            }
                        }
                    }

                    if (compile && patch)
                    {
                        if (ListOnly)
                        {
                            // ohhhh, funny guy, eh?
                            if (BigEndian)
                            {
                                Console.Error.WriteLine("DUDE: What the f#@% are you doing? This isn't funny!");
                            }
                            else
                            {
                                Console.Error.WriteLine("DUDE: WTF are you even trying to do?! This is ridiculous!");
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("ERROR: 'compile' and 'patch' cannot be specified in the same session.");
                        }

                        error = true;
                    }
                    else if (ListOnly && (compile || patch))
                    {
                        Console.Error.WriteLine("ERROR: 'list' cannot be specified with 'compile' or 'patch' in the same session.");
                        error = true;
                    }
                    

                    if (error)
                    {
                        Console.WriteLine("Terminating....");
                        return;
                    }

                    PrintVersion();
                    
                    if (compile)
                    {
                        // compile an fchunk
                        if (String.IsNullOrEmpty(input))
                            throw new InvalidOperationException("No input directory specified.");
                        if (!Directory.Exists(input))
                            throw new DirectoryNotFoundException("The specified input directory was not found.");

                        Compile(input, outputDir);
                    }
                    else if (patch)
                    {
                        // patch an fchunk
                        if (!File.Exists(patchFile))
                            throw new FileNotFoundException("Cannot patch file - script package not found.");
                        if (String.IsNullOrEmpty(input))
                            throw new DirectoryNotFoundException("Cannot patch file - no patch directory was specified.");
                        if (!Directory.Exists(input))
                            throw new DirectoryNotFoundException("Cannot patch file - the specified directory does not exist.");
                        
                        Patch(patchFile, input, outputDir);
                    }
                    else
                    {
                        if (!File.Exists(input))
                            throw new FileNotFoundException("Script package not found.");

                        if (ListOnly)
                        {
                            // list contents of .fchunk file
                            var scriptFile = new ScriptPackageFile(input, BigEndian);
                            
                            Console.WriteLine($"Version: {scriptFile.Version}");
                            Console.WriteLine($"Reserved: {scriptFile.Flags}");

                            Console.WriteLine();

                            foreach (var script in scriptFile.Scripts)
                            {
                                Console.WriteLine($"- {script.Filename}");
                                Console.WriteLine($"    [hash: {script.Hash:X8}, size: {script.Buffer.Length} bytes]");
                            }
                        }
                        else
                        {
                            // unpack an .fchunk
                            Unpack(input, outputDir);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERROR: {e.Message}\r\n");

                    Console.Error.WriteLine("==================== Stack Trace ====================");
                    Console.Error.WriteLine($"<{e.GetType().FullName}>:");
                    Console.Error.WriteLine(e.StackTrace);
                }
            }
            else
            {
                Console.WriteLine(
@"Usage: LuaSF [:options] [input] [:output folder]

If no output folder is specified, the input file's directory is used.
A simple folder structure may be created to keep things organized.

Specifying an output folder overrides this behavior entirely.

Options:
    --big
    --big-endian    Specifies the Big-Endian format for scripts.
                    
                    NOTE:
                      This option is for advanced users only.
                      Unexpected behaviour may occur.

    --compile       Compiles all .lua files in the specified directory.

                    The default output folder is a 'bin' folder,
                    located in the same directory as the script files.

                    Specifying an output folder overrides this behavior.

    --l
    --list          Lists the contents of the script package.
                    Useful for verifying script packages.

                    NOTE:
                      This option doesn't output anything.
                      The output folder has no effect on this option.

                      If you want to output contents to a file,
                      run the program like this:

                        LuaSF --list scripts.fchunk > scripts.txt

    --patch:[file]  Compiles all .lua files using a patch file
            │       from the specified directory.
            │
            └─────› Path to .fchunk to be patched
                    (e.g. 'Resources\ScriptsLuaScripts.fchunk')
                    
                    NOTE:
                      With this option, the input must be a path
                      to a directory that contains a 'patch.cfg' file
                      with a list of files to be patched.
                      
                      If no output folder is specified, the original
                      file will be overwritten WITHOUT warning.

Symbols:
    $<DriverSF>     Path to your Driver San Francisco installation.

    $<Scripts>      Path to your Driver San Francisco scripts.
                    
                    This is actually the 'media' folder inside of your
                    Driver San Francisco installation directory
                    (e.g. '<DriverSF>\media')");
            }
        }

        static void PrintVersion()
        {
            var assembly = typeof(Program).Assembly;
            
            var versionLine = $"LuaSF V{assembly.GetName().Version} by Fireboyd78";
            var separator = String.Empty.PadLeft(versionLine.Length, '=');
            
            string[] lines = {
                separator,
                versionLine,
                separator,
                ""
            };

            Console.WriteLine(String.Join(Environment.NewLine, lines));
        }
    }
}
