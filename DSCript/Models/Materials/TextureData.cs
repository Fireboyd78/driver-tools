using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using FreeImageAPI;

namespace DSCript.Models
{
    public interface ITextureData
    {
        int UID { get; set; }
        int Hash { get; set; }
        
        int Type { get; set; }
        int Flags { get; set; }

        int Width { get; set; }
        int Height { get; set; }

        byte[] Buffer { get; set; }
    }

    public sealed class TextureDataPC : ITextureData
    {
        private byte[] m_buffer = null;
        private int m_size = 0;

        private DSCTempFile m_tempFile = null;
        
        public int UID { get; set; }
        public int Hash { get; set; }

        public int Type { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        public int Flags { get; set; }

        public byte[] Buffer
        {
            get
            {
#if USE_TEXTURE_CACHE
                if ((m_buffer == null) && (m_size != 0))
                {
                    if (m_tempFile != null)
                        return m_tempFile.GetBuffer();
                }
#endif
                return m_buffer;
            }
            set
            {
#if USE_TEXTURE_CACHE
                m_size = (value != null) ? value.Length : 0;
                
                // cache textures larger than 512kb
                if (m_size > 0x80000)
                {
                    if (m_tempFile == null)
                        m_tempFile = new DSCTempFile();

                    m_tempFile.SetBuffer(value);
                    m_buffer = null;
                }
                else
                {
                    m_buffer = value;
                }
#else
                m_buffer = value;
#endif
            }
        }
    }
}
