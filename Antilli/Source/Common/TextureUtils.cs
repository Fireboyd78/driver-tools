using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

namespace Antilli
{
    public static class TextureUtils
    {
        public static bool BuggedAboutDuplicateFiles = false;
        public static bool OverwriteDuplicateFiles = false;

        public static void ExportTexture(ITextureData texture)
        {
            var filename = $"{texture.Handle:X8}";

            if (texture.UID != 0x01010101)
            {
                var uid = new UID(texture.UID, texture.Handle);

                filename = uid.ToString("_");
            }

            var ext = "biff";

            if (!Utils.TryGetImageFormat(texture.Buffer, out ext))
                ext = "bin";

            var saveDlg = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ext,
                FileName = filename,
                InitialDirectory = Settings.TexturesDirectory,
                Title = "Please enter a filename",
                ValidateNames = true,
                OverwritePrompt = true,
            };

            if (saveDlg.ShowDialog() ?? false)
                FileManager.WriteFile(saveDlg.FileName, texture.Buffer);
        }

        public static bool ExportTextures<T>(List<T> textures, string directory, string prefix = "", bool prefixIndex = false, bool silent = false)
            where T : ITextureData
        {
            var index = 0;

            foreach (var texture in textures)
            {
                var filename = $"{texture.Handle:X8}";

                if (texture.UID != 0x01010101)
                {
                    var uid = new UID(texture.UID, texture.Handle);

                    filename = uid.ToString("_");
                }

                if (prefixIndex)
                    filename = $"[{index:D4}]-{filename}";

                var ext = "biff";

                if (!Utils.TryGetImageFormat(texture.Buffer, out ext))
                    ext = "bin";

                if (!String.IsNullOrEmpty(prefix))
                    filename = $"{prefix}_{filename}";

                if (!prefixIndex)
                    filename = $"{filename}={index:D4}"; // suffix the index for safe measure

                var path = Path.Combine(directory, $"{filename}.{ext}");
                var dupe = 2;

                while (!OverwriteDuplicateFiles && File.Exists(path))
                {
                    var crc1 = Memory.GetCRC32(texture.Buffer);
                    var crc2 = Memory.GetCRC32(File.ReadAllBytes(path));

                    // check with the user if they want to overwrite actual duplicate textures
                    // if they're truly different textures, it might be of interest to keep them ;)
                    if (crc1 == crc2)
                    {
                        if (!BuggedAboutDuplicateFiles)
                        {
                            BuggedAboutDuplicateFiles = true;

                            if (MessageBox.Show("Would you like to add a number for duplicate texture files? If you select NO, they will be overwritten!",
                                "Texture Exporter", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                            {
                                // user wants to overwrite from now on
                                OverwriteDuplicateFiles = true;
                                break;
                            }
                        }

                        // incremental dupe number setup ;)
                        path = Path.Combine(directory, $"{filename} ({dupe++}).{ext}");
                    }
                    else
                    {
                        // if this was already done before, then it will prompt user about duplicate textures next
                        path = Path.Combine(directory, $"{filename}[{crc1:X8}].{ext}");
                    }
                }

                FileManager.WriteFile(path, texture.Buffer);
                index++;
            }

            if (!silent)
                MessageBox.Show("Successfully exported all textures!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        public static bool ExportTextures<T>(List<T> textures, bool silent = false)
            where T : ITextureData
        {
            if (textures != null)
            {
                var saveDlg = new FolderSelectDialog()
                {
                    InitialDirectory = Settings.TexturesDirectory,
                };

                if (saveDlg.ShowDialog())
                {
                    var result = ExportTextures(textures, saveDlg.SelectedPath, "", false, silent);

                    // make sure to reset these
                    BuggedAboutDuplicateFiles = false;
                    OverwriteDuplicateFiles = false;

                    return result;
                }
            }

            return false;
        }
    }
}
