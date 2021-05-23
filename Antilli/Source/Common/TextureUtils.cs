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
        public static void ExportTexture(ITextureData texture)
        {
            var filename = $"{texture.Handle:X8}";

            if (texture.UID != 0x01010101)
                filename = $"{texture.UID:X8}_{texture.Handle:X8}";

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

        public static bool ExportTextures<T>(List<T> textures, string directory, bool silent = false)
            where T : ITextureData
        {
            foreach (var texture in textures)
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

                filename = $"{filename}.{ext}";

                var path = Path.Combine(directory, filename);

                FileManager.WriteFile(path, texture.Buffer);
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
                    return ExportTextures(textures, saveDlg.SelectedPath, silent);
            }

            return false;
        }
    }
}
