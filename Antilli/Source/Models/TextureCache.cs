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
        static List<Texture> Cache;

        static TextureCache()
        {
            Cache = new List<Texture>();
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

                    var flushed = new List<Texture>();

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
        public static Texture GetTexture(ITextureData texture)
        {
            Texture cachedTexture;

            var idx = GetCachedTextureIndex(texture);

            if (idx == -1)
            {
                // initialize the first reference
                cachedTexture = new Texture(texture);
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
        public static void Release(Texture reference)
        {
            if (reference.RefCount > 0)
                reference.RefCount--;
        }
    }

    public class TextureBitmap : IDisposable
    {
        private static Dictionary<int, TextureBitmap> m_BitmapCache 
            = new Dictionary<int, TextureBitmap>();

        private static Dictionary<TextureBitmap, int> m_BitmapLookup
            = new Dictionary<TextureBitmap, int>();

        private IntPtr[] m_hbitmaps = new IntPtr[3];
        private BitmapSource[] m_bitmaps = new BitmapSource[3];

        private FIBITMAP m_bitmap;
        private FREE_IMAGE_FORMAT m_format;
        
        private int m_width;
        private int m_height;
        
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
                    NativeMethods.DeleteObject(hBitmap);

                m_hbitmaps[i] = IntPtr.Zero;
                m_bitmaps[i] = null;
            }
            
            if (!m_bitmap.IsNull)
                FreeImage.UnloadEx(ref m_bitmap);
        }

        public bool Update()
        {
            if (m_bitmap.IsNull)
                return false;

            return true;
        }

        public bool GetHBitmap(out IntPtr hBitmap, BitmapSourceLoadFlags flags = BitmapSourceLoadFlags.Default)
        {
            if (m_bitmap.IsNull)
            {
                hBitmap = IntPtr.Zero;
                return false;
            }

            var index = (int)flags;
            var dib = m_hbitmaps[index];
            
            if (dib == IntPtr.Zero)
            {
                var image = m_bitmap;
                var unload = false;
                
                if (flags.HasFlag(BitmapSourceLoadFlags.AlphaMask))
                {
                    image = FreeImage.GetChannel(m_bitmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                    if (image.IsNull)
                    {
                        // that didn't work :(
                        image = m_bitmap;
                    }
                    else
                    {
                        unload = true;
                    }
                }
                
                var bmap = flags.HasFlag(BitmapSourceLoadFlags.Transparency)
                    ? FreeImage.ConvertTo32Bits(image)
                    : FreeImage.ConvertTo24Bits(image);

                try
                {
                    dib = FreeImage.GetHbitmap(bmap, IntPtr.Zero, false);
                    m_hbitmaps[index] = dib;

                    FreeImage.UnloadEx(ref bmap);

                    if (unload)
                        FreeImage.UnloadEx(ref image);
                }
                catch (Exception)
                {
                    // silently fail
                    hBitmap = IntPtr.Zero;
                    return false;
                }
            }

            hBitmap = dib;
            return true;
        }
        
        public BitmapSource GetBitmapSource(BitmapSourceLoadFlags flags = BitmapSourceLoadFlags.Default)
        {
            if (m_bitmap.IsNull)
                return null;

            var index = (int)flags;
            var result = m_bitmaps[index];

            if (result == null)
            {
                var hBitmap = IntPtr.Zero;
                
                if (GetHBitmap(out hBitmap, flags))
                {
                    try
                    {
                        result = Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        result.Freeze();

                        m_bitmaps[index] = result;
                    }
                    catch (Win32Exception)
                    {
                        result = null;
                    }
                }
            }

            return result;
        }

        public static TextureBitmap Create(byte[] buffer)
        {
            if ((buffer == null) || (buffer.Length == 0))
                return null;

            TextureBitmap result = null;
            
            var hash = (int)Memory.GetCRC32(buffer);

            if (m_BitmapCache.TryGetValue(hash, out result))
                return result;

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var ptr = handle.AddrOfPinnedObject();

            try
            {
                result = new TextureBitmap(ptr, buffer.Length);

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
        
        protected TextureBitmap(IntPtr hImage, int size)
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

        protected TextureBitmap(Stream stream)
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
    }
    
    public class Texture : IDisposable
    {
        internal int RefCount { get; set; }

        TextureBitmap m_bitmap = null;
        ITextureData m_textureData = null;
        
        public ITextureData Data
        {
            get { return m_textureData; }
        }

        public TextureBitmap Bitmap
        {
            get
            {
                if (m_bitmap == null)
                    m_bitmap = TextureBitmap.Create(m_textureData.Buffer);

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
                m_bitmap = TextureBitmap.Create(buffer);

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

            // finalizer doesn't serve a purpose anymore
            GC.SuppressFinalize(this);
        }

        internal Texture(ITextureData texture)
        {
            m_textureData = texture;
            m_bitmap = null;
        }

        ~Texture()
        {
            if (RefCount < 1)
                Dispose();
        }
    }
}
