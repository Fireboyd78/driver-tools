﻿using System;
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

using Interop = System.Windows.Interop;

using DSCript;
using DSCript.Models;

using FreeImageAPI;

namespace Antilli
{
    public static class TextureCache
    {
        static List<TextureReference> Cache;

        static TextureCache()
        {
            Cache = new List<TextureReference>();
        }

        public static void FlushIfNeeded()
        {
            if (Cache.Count > 25)
                Flush();
        }

        public static void Flush()
        {
            if (Cache.Count > 0)
            {
                DSC.Log($"Flushing {Cache.Count} textures in the cache.");

                foreach (var texture in Cache)
                    texture.Free();

                Cache.Clear();
            }
        }

        static int GetCachedTextureIndex(ITextureData texture)
        {
            return Cache.FindIndex((t) => t.Data == texture);
        }

        static bool IsCached(ITextureData texture)
        {
            return GetCachedTextureIndex(texture) != -1;
        }

        /// <summary>
        /// Retrieves a cached version of this texture (if it exists), otherwise it will be added to the cache automatically.
        /// </summary>
        /// <param name="texture">The texture to get a cached version of</param>
        /// <returns>The cached version of the texture</returns>
        public static TextureReference GetTexture(ITextureData texture)
        {
            TextureReference cachedTexture;

            var idx = GetCachedTextureIndex(texture);

            if (idx == -1)
            {
                cachedTexture = new TextureReference(texture);
                Cache.Add(cachedTexture);
            }
            else
            {
                cachedTexture = Cache[idx];
            }
            
            return cachedTexture;
        }
    }

    public class BitmapReference
    {
        private IntPtr[] m_hbitmaps = new IntPtr[3];
        private BitmapSource[] m_bitmaps = new BitmapSource[3];

        private FIBITMAP m_bitmap;
        private FREE_IMAGE_FORMAT m_format;

        private int m_width;
        private int m_height;

        public FIBITMAP Bitmap
        {
            get { return m_bitmap; }
        }
        
        public int Width
        {
            get { return m_width; }
        }

        public int Height 
        {
            get { return m_height; }
        }

        public FREE_IMAGE_FORMAT Format
        {
            get { return m_format; }
        }
        
        public void Free()
        {
            for (int i = 0; i < 3; i++)
            {
                IntPtr hBitmap = m_hbitmaps[i];

                if (hBitmap != IntPtr.Zero)
                    FreeImage.FreeHbitmap(hBitmap);

                m_hbitmaps[i] = IntPtr.Zero;
                m_bitmaps[i] = null;
            }

            m_bitmap.Unload();
        }

        public bool Update()
        {
            if (m_bitmap.IsNull)
                return false;

            //--var bmap_a = FreeImage.GetChannel(m_bitmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
            //--
            //--for (int i = 0; i < 3; i++)
            //--{
            //--    IntPtr hBitmap = m_hbitmaps[i];
            //--
            //--    if (hBitmap != IntPtr.Zero)
            //--        FreeImage.FreeHbitmap(hBitmap);
            //--
            //--    var b = (i > 1) ? bmap_a : m_bitmap;
            //--    var bmap = ((i & 1) != 0) ? FreeImage.ConvertTo32Bits(b) : FreeImage.ConvertTo24Bits(b);
            //--    
            //--    m_hbitmaps[i] = FreeImage.GetHbitmap(bmap, IntPtr.Zero, false);
            //--    bmap.Unload();
            //--}
            //--
            //--bmap_a.Unload();
            return true;
        }

        public IntPtr GetHBitmap(BitmapSourceLoadFlags flags = BitmapSourceLoadFlags.Default)
        {
            var index = (int)flags;
            var hBitmap = m_hbitmaps[index];

            if (hBitmap == IntPtr.Zero)
            {
                var bmap_a = FreeImage.GetChannel(m_bitmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                var b = flags.HasFlag(BitmapSourceLoadFlags.AlphaMask)
                    ? bmap_a
                    : m_bitmap;

                var bmap = flags.HasFlag(BitmapSourceLoadFlags.Transparency)
                    ? FreeImage.ConvertTo32Bits(b)
                    : FreeImage.ConvertTo24Bits(b);

                hBitmap = FreeImage.GetHbitmap(bmap, IntPtr.Zero, false);
                m_hbitmaps[index] = hBitmap;

                bmap_a.Unload();
            }

            return hBitmap;
        }
        
        public BitmapSource GetBitmapSource(BitmapSourceLoadFlags flags = BitmapSourceLoadFlags.Default)
        {
            if (m_bitmap.IsNull)
                return null;

            var result = m_bitmaps[(int)flags];

            if (result == null)
            {
                try
                {
                    result = Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        GetHBitmap(flags),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    result.Freeze();

                    m_bitmaps[(int)flags] = result;
                }
                catch (Win32Exception)
                {
                    result = null;
                }
            }

            return result;
        }

        public static BitmapReference Create(byte[] buffer)
        {
            BitmapReference result = null;

            using (var ms = new MemoryStream(buffer))
            {
                try
                {
                    result = new BitmapReference(ms);

                    // make sure it initializes properly
                    if (!result.Update())
                        result = null;
                }
                catch (Exception)
                {
                    result = null;
                }
            }

            return result;
        }

        protected BitmapReference(Stream stream)
        {
            try
            {
                m_format = FreeImage.GetFileTypeFromStream(stream);
                m_bitmap = FreeImage.LoadFromStream(stream, ref m_format);

                m_width = (int)FreeImage.GetWidth(m_bitmap);
                m_height = (int)FreeImage.GetHeight(m_bitmap);
            }
            catch (Exception)
            {
                Free();
            }
        }
    }
    
    public class TextureReference
    {
        BitmapReference m_bitmap = null;
        ITextureData m_textureData = null;
        
        public ITextureData Data
        {
            get { return m_textureData; }
        }

        public BitmapReference Bitmap
        {
            get
            {
                if (m_bitmap == null)
                    m_bitmap = BitmapReference.Create(m_textureData.Buffer);

                return m_bitmap;
            }
        }
        
        public void SetBuffer(byte[] buffer)
        {
            m_textureData.Buffer = buffer;
            
            if (m_bitmap != null)
                m_bitmap.Free();

            m_bitmap = BitmapReference.Create(buffer);

            if (m_bitmap != null)
            {
                m_textureData.Width = m_bitmap.Width;
                m_textureData.Height = m_bitmap.Height;
            }
        }
        
        public void Free()
        {
            if (m_bitmap != null)
                m_bitmap.Free();
        }

        internal TextureReference(ITextureData texture)
        {
            m_textureData = texture;
            m_bitmap = null;
        }
    }
}