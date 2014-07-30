using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace System.IO
{
    /// <summary>
    /// Represents a class used to read/write key values to/from INI files.
    /// </summary>
    public class IniFile
    {
        static readonly char[] nullChar = {'\0'};

        /// <summary>
        /// Gets the filename of the <see cref="IniFile"/>.
        /// </summary>
        public string FileName { get; private set; }

        public string[] GetSections()
        {
            char[] val = new char[1024];

            NativeMethods.GetPrivateProfileString(null, null, "", val, 1024, FileName);

            return new string(val).Split(nullChar, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] ReadValue(string section)
        {
            char[] val = new char[1024];

            NativeMethods.GetPrivateProfileString(section, null, null, val, 1024, FileName);

            return new string(val).Split(nullChar, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Reads a key value from the specified section.
        /// </summary>
        /// <param name="section">The section to retrieve the key value from.</param>
        /// <param name="key">The key value to retrieve.</param>
        /// <returns>The key value from the specified section, otherwise returns a blank string.</returns>
        public string ReadValue(string section, string key)
        {
            char[] val = new char[1024];

            NativeMethods.GetPrivateProfileString(section, key, String.Empty, val, 1024, FileName);

            string[] str = new string(val).Split(nullChar, StringSplitOptions.RemoveEmptyEntries);

            return (str.Length > 0) ? str[0] : String.Empty;
        }

        /// <summary>
        /// Writes a key value to the specified section.
        /// </summary>
        /// <param name="section">The section to write the key value to.</param>
        /// <param name="key">The key to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if successful, False if the operation failed.</returns>
        public bool WriteValue(string section, string key, string value)
        {
            return NativeMethods.WritePrivateProfileString(section, key, value, FileName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniFile"/> class, used to read and write key values to/from INI files.
        /// </summary>
        /// <param name="path">The path to the INI file.</param>
        public IniFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            FileName = path;
        }

        public IniFile(string filename, string path) : this(Path.Combine(path, filename)) { }
    }
}