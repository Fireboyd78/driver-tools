using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Antilli.Parser
{
    public class TokenReader : IDisposable
    {
        protected int m_Line = 0;
        protected int m_TokenIndex = 0;

        protected string[] m_TokenBuffer;

        protected StreamReader Reader { get; set; }

        protected bool IsBufferEmpty
        {
            get { return (m_TokenBuffer == null || (m_TokenBuffer.Length == 0)); }
        }

        public void Dispose()
        {
            if (Reader != null)
                Reader.Dispose();

            m_TokenBuffer = null;
        }

        public int CurrentLine
        {
            get { return m_Line; }
        }

        public bool EndOfLine
        {
            get { return (m_TokenBuffer != null) ? (m_TokenIndex >= m_TokenBuffer.Length) : true; }
        }

        public bool EndOfStream
        {
            get
            {
                if (Reader != null)
                    return (Reader.EndOfStream && EndOfLine);

                return true;
            }
        }
        
        public int TokenIndex
        {
            get { return m_TokenIndex; }
        }

        public int TokenCount
        {
            get { return (m_TokenBuffer != null) ? m_TokenBuffer.Length : -1;}
        }
        
        /// <summary>
        /// Reads in the tokens on the next line and returns the number of tokens loaded.
        /// </summary>
        /// <returns>The number of tokens parsed, otherwise -1 if end of stream reached.</returns>
        protected int ReadInTokens()
        {
            while (!Reader.EndOfStream)
            {
                // read in the next line of tokens
                var line = Reader.ReadLine();
                var startLine = ++m_Line;
                
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                // read the next line if safe to do so
                if (Tokenizer.IsCommentLine(line))
                    continue;

                // split them up into the token buffer and reset the index
                m_TokenBuffer = Tokenizer.SplitTokens(line);
                m_TokenIndex = 0;

                // return number of tokens brought in
                return m_TokenBuffer.Length;
            }

            // end of stream
            return -1;
        }

        protected bool CheckToken(int tokenIndex)
        {
            // verifies index into the buffer is accessible
            if (!IsBufferEmpty)
                return (tokenIndex < m_TokenBuffer.Length);

            return false;
        }

        public bool NextLine()
        {
            // try filling in the buffer
            // won't affect token index if it fails
            return (ReadInTokens() != -1);
        }

        public string GetToken(int index)
        {
            if (CheckToken(index))
                return (m_TokenBuffer[index]);

            // failed to get token :(
            return null;
        }
        
        public string PopToken(int offset = 0)
        {
            m_TokenIndex += offset;

            // don't let the user pop too many values
            if (m_TokenIndex < 0)
                throw new InvalidOperationException("PopToken() -- offset caused negative index, too many values popped!");
            
            return GetToken(m_TokenIndex++);
        }
        
        /// <summary>
        /// Reads the next valid token from the buffer and increments the token index.
        /// </summary>
        /// <returns>The next valid token from the buffer; otherwise, null.</returns>
        public string ReadToken()
        {
            string token = null;

            while (token == null)
            {
                if (EndOfLine)
                {
                    // don't proceed any further
                    if (EndOfStream)
                        return null;

                    NextLine();
                }

                token = GetToken(m_TokenIndex++);
            }

            return token;
        }

        /// <summary>
        /// Gets the next token from the buffer without incrementing the token index or filling the buffer.
        /// </summary>
        /// <returns>The next token from the buffer; otherwise, null.</returns>
        public string PeekToken()
        {
            return GetToken(m_TokenIndex);
        }

        public string PeekToken(int offset)
        {
            return GetToken(m_TokenIndex + offset);
        }

        public bool Seek(int offset)
        {
            m_TokenIndex += offset;

            if (m_TokenIndex < 0)
                throw new InvalidOperationException("Seek() -- offset caused negative index!");

            return CheckToken(m_TokenIndex);
        }

        public int FindPattern(string[] tokens, int index)
        {
            if (EndOfStream)
                throw new InvalidOperationException("GetTokensIndex() -- end of stream exception.");

            // do not look past the end of the line
            // the user may decide to move to the next line if necessary
            if (EndOfLine || ((index + tokens.Length) >= m_TokenBuffer.Length))
                return -1;

            // index into the tokens we're looking for
            var tokenIndex = 0;

            // iterate through the tokens available in the buffer
            for (int i = index; i < m_TokenBuffer.Length; i++)
            {
                if (m_TokenBuffer[i] == tokens[tokenIndex])
                {
                    // stop when the pattern is found
                    if ((tokenIndex + 1) == tokens.Length)
                    {
                        /*
                            if "BAZ" in "FOOBARBAZ":
                              tokenIndex = 2
                              i = 8
                            therefore:
                              tokensIndex = 6
                        */
                        return (i - tokenIndex);
                    }

                    ++tokenIndex;
                }
                else
                {
                    // reset the token index if needed
                    if (tokenIndex > 0)
                        tokenIndex = 0;
                }
            }

            return -1;
        }
        
        public int FindNextPattern(string[] tokens)
        {
            return FindPattern(tokens, m_TokenIndex);
        }

        public bool MatchToken(string matchToken, string nestedToken)
        {
            var startLine = CurrentLine;
            
            // TODO: Fix this?
            if (nestedToken.Length != matchToken.Length)
                throw new InvalidOperationException("MatchToken() -- length of nested token isn't equal to the match token.");
            
            // did we find the match?
            var match = false;
            var token = "";

            while (!match && (token = ReadToken()) != null)
            {
                if (token == matchToken)
                {
                    match = true;
                    break;
                }
                else if (token == nestedToken)
                {
                    var nestLine = CurrentLine;

                    // nested blocks
                    if (!MatchToken(matchToken, nestedToken))
                        throw new InvalidOperationException($"MatchToken() -- nested token '{nestedToken}' on line {nestLine} wasn't closed before the original token '{matchToken}' on line {startLine}.");
                }
                else if (CurrentLine > startLine)
                {
                    // multi-line match
                    NextLine();
                }
            }
            
            return match;
        }
        
        public TokenReader(Stream stream)
        {
            if (!stream.CanRead || !stream.CanSeek)
                throw new EndOfStreamException("Cannot instantiate a new TokenReader on a closed/ended Stream.");

            Reader = new StreamReader(stream, true);

            if (ReadInTokens() == -1)
                throw new InvalidOperationException("Failed to create TokenReader -- could not read in tokens.");
        }
    }
}
