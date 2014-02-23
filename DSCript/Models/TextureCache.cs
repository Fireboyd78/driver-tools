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
        static Dictionary<string, CachedTexture> NamedCache;
        static List<CachedTexture> Cache;

        static TextureCache()
        {
            Cache = new List<CachedTexture>();
            NamedCache = new Dictionary<string, CachedTexture>();
        }

        public static void FlushIfNeeded()
        {
            // 175 cached textures sounds good, no?
            if (NamedCache.Count > 175 || Cache.Count > 175)
                Flush();
        }

        public static void Flush()
        {
            foreach (CachedTexture texture in Cache)
                texture.Clean();

            CachedTexture.ClearAll();

            NamedCache.Clear();
            Cache.Clear();

            DSC.Log("Texture cache flushed.");
        }

        static bool IsCached(CacheableTexture texture)
        {
            int idx = Cache.FindIndex((t) => t.Texture == texture);

            return idx != -1;
        }

        static bool IsCached(string textureName)
        {
            return NamedCache.ContainsKey(textureName);
        }

        /// <summary>
        /// Retrieves a cached version of this texture (if it exists), otherwise it will be added to the cache automatically.
        /// </summary>
        /// <param name="texture">The texture to get a cached version of</param>
        /// <returns>The cached version of the texture</returns>
        public static CachedTexture GetCachedTexture(CacheableTexture texture)
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

        public static CachedTexture GetCachedTexture(string textureName, byte[] buffer, uint width, uint height)
        {
            CachedTexture cachedTexture;

            if (!IsCached(textureName))
            {
                cachedTexture = new CachedTexture(textureName, buffer, width, height);
                Cache.Add(cachedTexture);

                NamedCache.Add(textureName, cachedTexture);
            }
            else
            {
                cachedTexture = NamedCache[textureName];
            }

            return cachedTexture;
        }
    }

    public class CacheableTexture
    {
        public uint Width { get; set; }
        public uint Height { get; set; }

        public uint Size
        {
            get { return (Buffer != null) ? (uint)Buffer.Length : 0; }
        }

        public byte[] Buffer { get; set; }

        public BitmapSource GetBitmapSource(BitmapSourceLoadFlags flags)
        {
            return BitmapSourceHelper.GetBitmapSource(Buffer, flags);
        }
    }

    public class CachedTexture
    {
        internal static List<CacheableTexture> Textures;
        internal static List<BitmapSource[]> Bitmaps;

        internal static int NumTextures = 0;

        static CachedTexture()
        {
            Textures = new List<CacheableTexture>();
            Bitmaps = new List<BitmapSource[]>();
        }

        internal static void ClearAll()
        {
            Textures.Clear();
            Bitmaps.Clear();

            NumTextures = 0;
        }

        public CacheableTexture Texture { get; private set; }
        public int Index { get; private set; }

        BitmapSource[] bitmaps
        {
            get { return Bitmaps[Index]; }
        }

        public BitmapSource GetBitmapSource()
        {
            return GetBitmapSource(BitmapSourceLoadFlags.None);
        }

        public BitmapSource GetBitmapSource(BitmapSourceLoadFlags flags)
        {
            int i = (int)flags;

            if (bitmaps[i] == null)
                bitmaps[i] = Texture.GetBitmapSource(flags);

            return bitmaps[i];
        }

        public void Reload()
        {
            for (int i = 0; i < 3; i++)
            {
                if (bitmaps[i] != null)
                {
                    BitmapSourceLoadFlags flags = (BitmapSourceLoadFlags)i;

                    bitmaps[i] = Texture.GetBitmapSource(flags);
                }
            }
        }

        public void Clean()
        {
            for (int i = 0; i < bitmaps.Length; i++)
                if (bitmaps[i] != null)
                    bitmaps[i] = null;
        }

        internal CachedTexture(CacheableTexture texture)
        {
            Texture = texture;

            Textures.Add(texture);
            Bitmaps.Add(new BitmapSource[3]);

            Index = NumTextures++;
        }

        internal CachedTexture(string textureName, byte[] buffer, uint width, uint height)
        {
            Texture = new CacheableTexture() {
                Buffer = buffer,
                Width = width,
                Height = height
            };

            Textures.Add(Texture);
            Bitmaps.Add(new BitmapSource[3]);

            Index = NumTextures++;
        }
    }
}
