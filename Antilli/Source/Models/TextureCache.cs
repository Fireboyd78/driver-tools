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
            var nTextures = Cache.Count;
            var forced = false;

            if (nTextures > 75)
            {
                if (nTextures > 250)
                {
                    if (nTextures > 500)
                    {
                        Debug.WriteLine($"**** TEXTURE CACHE IS LEAKING -- forcing a cleanup!");
                        forced = true;
                    }
                    else
                    {
                        Debug.WriteLine($"**** WARNING: {nTextures} cached textures are in-use!");
                    }
                }

                Flush(forced);
            }
        }

        public static void Flush(bool forced = false)
        {
            var nTextures = Cache.Count;

            if (nTextures > 0)
            {
                if (forced)
                {
                    Debug.WriteLine($"Flushing {nTextures} textures from the cache...");

                    for (int i = 0; i < nTextures; i++)
                    {
                        Cache[i].Dispose();
                        Cache[i] = null;
                    }

                    Cache.Clear();
                }
                else
                {
                    Debug.WriteLine($"Cleaning texture cache...");

                    var flushed = new List<TextureReference>();

                    for (int i = 0; i < nTextures; i++)
                    {
                        var texture = Cache[i];

                        if (texture.RefCount == 0)
                        {
                            texture.Dispose();
                            flushed.Add(texture);
                        }
                    }

                    var nFlushed = flushed.Count;

                    if (nFlushed != 0)
                    {
                        for (int i = 0; i < nFlushed; i++)
                        {
                            Cache.Remove(flushed[i]);
                            nTextures--;
                        }
                        
                        Debug.WriteLine($" - {nFlushed} unused textures flushed (remaining: {nTextures})");
                    }
                    else
                    {
                        Debug.WriteLine($" - no textures flushed ({nTextures} textures still in use)");
                    }
                }
            }
        }

        static int GetCachedTextureIndex(ITextureData texture)
        {
            if (Cache.Count == 0)
                return -1;

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
                // initialize the first reference
                cachedTexture = new TextureReference(texture);
                cachedTexture.RefCount = 1;

                Cache.Add(cachedTexture);
            }
            else
            {
                // add a new reference owner
                cachedTexture = Cache[idx];
                cachedTexture.RefCount++;
            }
            
            return cachedTexture;
        }

        /// <summary>
        /// Informs the cache this texture reference is no longer being used by a previous owner.
        /// </summary>
        /// <param name="reference">The texture reference to release.</param>
        public static void Release(TextureReference reference)
        {
            if (reference.RefCount > 0)
                reference.RefCount--;
        }
    }

    public class BitmapReference : IDisposable
    {
        private static Dictionary<int, BitmapReference> m_BitmapCache 
            = new Dictionary<int, BitmapReference>();

        private static Dictionary<BitmapReference, int> m_BitmapLookup
            = new Dictionary<BitmapReference, int>();

        private IntPtr[] m_hbitmaps = new IntPtr[3];
        private BitmapSource[] m_bitmaps = new BitmapSource[3];

        private FIBITMAP m_bitmap;
        private FREE_IMAGE_FORMAT m_format;

        private bool m_CanFreeBitmap;

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

        public void Dispose()
        {
            var hash = 0;

            if (m_BitmapLookup.TryGetValue(this, out hash))
            {
                m_BitmapCache.Remove(hash);
                m_BitmapLookup.Remove(this);
            }

            for (int i = 0; i < 3; i++)
            {
                IntPtr hBitmap = m_hbitmaps[i];

                if (hBitmap != IntPtr.Zero)
                {
                    FreeImage.FreeHbitmap(hBitmap);
                    NativeMethods.DeleteObject(hBitmap);
                }

                m_hbitmaps[i] = IntPtr.Zero;
                m_bitmaps[i] = null;
            }

            if (m_CanFreeBitmap)
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

                hBitmap = FreeImage.GetHbitmap(bmap, IntPtr.Zero, true);
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
                var hbitmap = GetHBitmap(flags);

                try
                {
                    result = Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hbitmap,
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
            if ((buffer == null) || (buffer.Length == 0))
                return null;

            BitmapReference result = null;
            
            var hash = (int)Memory.GetCRC32(buffer);

            if (m_BitmapCache.TryGetValue(hash, out result))
                return result;

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var ptr = handle.AddrOfPinnedObject();

            try
            {
                result = new BitmapReference(ptr, buffer.Length);

                // make sure it initializes properly
                if (result.Update())
                {
                    // cache it!
                    m_BitmapCache.Add(hash, result);
                    m_BitmapLookup.Add(result, hash);
                }
                else
                {
                    result = null;
                }
            }
            catch (Exception)
            {
                result = null;
            }
            finally
            {
                handle.Free();
            }
            
            return result;
        }
        
        protected BitmapReference(IntPtr hImage, int size)
        {
            var memory = FIMEMORY.Zero;

            try
            {
                memory = FreeImage.OpenMemory(hImage, (uint)size);

                m_format = FreeImage.GetFileTypeFromMemory(memory, size);
                m_bitmap = FreeImage.LoadFromMemory(m_format, memory, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
                
                m_width = (int)FreeImage.GetWidth(m_bitmap);
                m_height = (int)FreeImage.GetHeight(m_bitmap);
            }
            catch (Exception)
            {
                Dispose();
            }
            finally
            {
                FreeImage.CloseMemory(memory);

                memory.SetNull();
            }
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
                Dispose();
            }
        }

        ~BitmapReference()
        {
            Dispose();
        }
    }
    
    public class TextureReference : IDisposable
    {
        internal int RefCount { get; set; }

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
        
        public void SetBuffer(byte[] buffer, bool updateHash = false)
        {
            m_textureData.Buffer = buffer;

            if (updateHash)
                m_textureData.Handle = (int)Memory.GetCRC32(buffer);
            
            if (m_bitmap != null)
                m_bitmap.Dispose();

            if ((buffer != null) && (buffer.Length != 0))
            {
                m_bitmap = BitmapReference.Create(buffer);

                if (m_bitmap != null)
                {
                    m_textureData.Width = m_bitmap.Width;
                    m_textureData.Height = m_bitmap.Height;
                }
            }
            else
            {
                m_bitmap = null;

                m_textureData.Width = 0;
                m_textureData.Height = 0;
            }
        }
        
        public void Dispose()
        {
            if (m_bitmap != null)
            {
                m_bitmap.Dispose();
                m_bitmap = null;
            }

            if (m_textureData != null)
                m_textureData = null;
        }

        internal TextureReference(ITextureData texture)
        {
            m_textureData = texture;
            m_bitmap = null;
        }

        ~TextureReference()
        {
            if (RefCount < 1)
                Dispose();
        }
    }
}
