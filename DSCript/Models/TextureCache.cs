using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public static class Models
    {
        static List<PCMPTextureInfo> Textures;
        static List<BitmapSource> Bitmaps;

        public static class TextureCache
        {
            static List<PCMPTextureInfo> Textures;
            static List<BitmapSource> Bitmaps;

            static TextureCache()
            {
                Textures = new List<PCMPTextureInfo>();
                Bitmaps = new List<BitmapSource>();
            }

            public static void CacheTexture(PCMPTextureInfo texture, BitmapSource bitmapSource)
            {
                Textures.Add(texture);
                Bitmaps.Add(bitmapSource);
            }

            public static BitmapSource GetCachedTexture(PCMPTextureInfo texture)
            {
                return Bitmaps[Textures.FindIndex((t) => t == texture)];
            }

            public static bool IsTextureCached(PCMPTextureInfo texture)
            {
                return Textures.Contains(texture);
            }

            /// <summary>
            /// Flushes the texture cache and frees up any memory as needed.
            /// </summary>
            public static void Flush()
            {
                if (Bitmaps.Count > 0 && Textures.Count > 0)
                {
                    for (int t = 0; t < Bitmaps.Count; t++)
                        Bitmaps[t] = null;

                    Bitmaps.Clear();
                    Textures.Clear();

                    DSC.Log("Cleared the texture cache.");
                }
            }
        }
    }
}
