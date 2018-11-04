using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Antilli.Parser
{
    public class TokenWriter : IDisposable
    {
        private int m_line = 0;
        private int m_tokenIndex = 0;

        protected StreamWriter Writer { get; set; }

        public int CurrentLine
        {
            get { return m_line; }
        }

        public int TokenIndex
        {
            get { return m_tokenIndex; }
        }

        public int IndentLevel { get; set; }

        public void Dispose()
        {
            if (Writer != null)
            {
                Writer.Flush();
                Writer.Dispose();
            }
        }

        public void NextLine()
        {
            Writer.WriteLine();
            m_tokenIndex = -1;
        }

        public void Write(string token)
        {
            if (m_tokenIndex == -1)
            {
                for (int i = 0; i < IndentLevel; i++)
                    Writer.Write('\t');

                m_tokenIndex = 0;
            }
            else if (m_tokenIndex > 0)
            {
                Writer.Write(' ');
            }

            Writer.Write(token);
        }

        public void WriteLine(string token)
        {
            Write(token);
            NextLine();
        }
        
        public TokenWriter(Stream stream)
        {
            if (!stream.CanWrite)
                throw new EndOfStreamException("Cannot instantiate a new TokenWriter on a closed/ended Stream.");

            Writer = new StreamWriter(stream, Encoding.UTF8);
        }
    }
}
