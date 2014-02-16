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

using DSCript;

namespace DSCript.Models
{
    public static class TextureCache
    {
        static List<CachedTexture> Cache;

        static TextureCache()
        {
            Cache = new List<CachedTexture>();
        }

        public static void Flush()
        {
            foreach (CachedTexture texture in Cache)
                texture.Clean();

            CachedTexture.ClearAll();
            Cache.Clear();

            DSC.Log("Texture cache flushed.");
        }

        static bool IsCached(PCMPTexture texture)
        {
            int idx = Cache.FindIndex((t) => t.Texture == texture);

            return idx != -1;
        }

        /// <summary>
        /// Retrieves a cached version of this texture (if it exists), otherwise it will be added to the cache automatically.
        /// </summary>
        /// <param name="texture">The texture to get a cached version of</param>
        /// <returns>The cached version of the texture</returns>
        public static CachedTexture GetCachedTexture(PCMPTexture texture)
        {
            CachedTexture cachedTexture;

            if (!IsCached(texture))
            {
                cachedTexture = new CachedTexture(texture);
                Cache.Add(cachedTexture);
            }
            else
            {
                cachedTexture = Cache.Find((t) => t.Texture == texture);
            }

            return cachedTexture;
        }
    }

    public class CachedTexture
    {
        internal static List<PCMPTexture> Textures;
        internal static List<BitmapSource[]> Bitmaps;

        internal static int NumTextures = 0;

        static CachedTexture()
        {
            Textures = new List<PCMPTexture>();
            Bitmaps = new List<BitmapSource[]>();
        }

        internal static void ClearAll()
        {
            Textures.Clear();
            Bitmaps.Clear();

            NumTextures = 0;
        }

        public PCMPTexture Texture { get; private set; }
        public int Index { get; private set; }

        BitmapSource[] bitmaps
        {
            get { return Bitmaps[Index]; }
        }

        public BitmapSource GetBitmapSource()
        {
            return GetBitmapSource(false);
        }

        public BitmapSource GetBitmapSource(bool alphaBlend)
        {
            int i = (alphaBlend) ? 1 : 0;

            if (bitmaps[i] == null)
                bitmaps[i] = Texture.GetBitmapSource(alphaBlend);

            return bitmaps[i];
        }

        public BitmapSource GetBitmapSourceAlphaChannel()
        {
            int i = 2;

            if (bitmaps[i] == null)
                bitmaps[i] = Texture.GetBitmapSource(true, true);

            return bitmaps[i];
        }

        public void Reload()
        {
            bool blend, alpha;

            for (int i = 0; i < 3; i++)
            {
                if (bitmaps[i] != null)
                {
                    blend = (i > 0) ? true : false;
                    alpha = (i == 2) ? true : false;

                    bitmaps[i] = Texture.GetBitmapSource(blend, alpha);
                }
            }
        }

        public void Clean()
        {
            for (int i = 0; i < bitmaps.Length; i++)
                if (bitmaps[i] != null)
                    bitmaps[i] = null;
        }

        internal CachedTexture(PCMPTexture texture)
        {
            Texture = texture;

            Textures.Add(texture);
            Bitmaps.Add(new BitmapSource[3]);

            Index = NumTextures++;
        }
    }
}
