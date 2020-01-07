using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DSCript.Parser
{
    public class TokenWriter : IDisposable
    {
        protected int m_Line = 0;
        protected int m_TokenIndex = 0;

        protected StreamWriter Writer { get; set; }

        public void Dispose()
        {
            if (Writer != null)
            {
                Writer.Flush();
                Writer.Dispose();
            }
        }

        public int CurrentLine
        {
            get { return m_Line; }
        }

        public int TokenIndex
        {
            get { return m_TokenIndex; }
        }

        public int IndentLevel { get; set; }
        
        public void AppendLine()
        {
            Writer.WriteLine();
            m_TokenIndex = -1;
        }

        public void Write(string token)
        {
            if (m_TokenIndex == -1)
            {
                for (int i = 0; i < IndentLevel; i++)
                    Writer.Write('\t');

                m_TokenIndex = 0;
            }
            else if (m_TokenIndex > 0)
            {
                Writer.Write(' ');
            }

            Writer.Write(token);
        }

        public void WriteLine(string token)
        {
            Write(token);
            AppendLine();
        }
        
        public TokenWriter(Stream stream)
        {
            if (!stream.CanWrite)
                throw new EndOfStreamException("Cannot instantiate a new TokenWriter on a closed/ended Stream.");

            Writer = new StreamWriter(stream, Encoding.UTF8);
        }
    }
}
