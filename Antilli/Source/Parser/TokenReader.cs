using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Antilli.Parser
{
    public static class Tokenizer
    {
        public static readonly char CommentLineKey = '#';

        public static bool IsCommentLine(char value)
        {
            return (value == CommentLineKey);
        }

        public static bool IsCommentLine(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Argument cannot be null.");

            return IsCommentLine(value[0]);
        }
        
        public static string[] SplitTokens(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str), "Argument cannot be null.");
            
            if (str.Length < 1)
                return new[] { str };

            var values = new List<String>(32);

            var start = 0;
            var length = 0;

            var stringOpen = false;
            var stringEscaped = false;

            var commentOpen = false;

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                var flags = CharUtils.GetCharFlags(c);

                // break on null
                if ((flags & CharacterTypeFlags.Null) != 0)
                    break;

                if ((flags & CharacterTypeFlags.TabOrWhitespace) != 0)
                {
                    // process tabs/whitespace outside of strings/comments
                    if (!stringOpen)
                    {
                        if (!commentOpen)
                        {
                            if (length > 0)
                                values.Add(str.Substring(start, length));

                            start = (i + 1); // "ABC|  DEF" -> "ABC | DEF" -> "ABC |DEF"
                            length = 0;
                        }
                        continue;
                    }
                }

                // check for inline comments
                if (IsCommentLine(c))
                {
                    commentOpen = true;
                    continue;
                }

                if (commentOpen)
                    continue;
                
                if ((flags & CharacterTypeFlags.ExtendedOperators) != 0)
                {
                    if (!stringOpen)
                    {
                        if (length > 0)
                            values.Add(str.Substring(start, length));

                        values.Add(c.ToString());

                        start = (i + 1);
                        length = 0;

                        continue;
                    }
                }

                // increase string length
                ++length;

                if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    if (stringOpen)
                    {
                        if (stringEscaped)
                        {
                            stringEscaped = false;
                        }
                        else
                        {
                            // complete the string (include last quote)
                            if (length > 0)
                                values.Add(str.Substring(start, length + 1));

                            start = (i + 1); // "ABC|" -> "ABC"|
                            length = 0;

                            stringOpen = false;
                        }
                    }
                    else
                    {
                        start = i; // |"ABC"
                        length = 0;

                        stringOpen = true;
                    }
                }
                else if (stringEscaped)
                {
                    // not an escape sequence
                    stringEscaped = false;
                    continue;
                }

                if (stringOpen && (c == '\\'))
                {
                    stringEscaped = true;
                    continue;
                }
            }

            // final add
            if (length > 0 && !commentOpen)
                values.Add(str.Substring(start, length));

            return values.ToArray();
        }
    }

    public class TokenReader : IDisposable
    {
        private int m_line = 0;

        private int m_tokenIndex = 0;
        private string[] m_tokenBuffer;
        
        protected bool IsBufferEmpty
        {
            get { return (m_tokenBuffer == null || (m_tokenBuffer.Length == 0)); }
        }
        
        protected StreamReader Reader { get; set; }

        public int CurrentLine
        {
            get { return m_line; }
        }

        public bool EndOfLine
        {
            get { return (m_tokenBuffer != null) ? (m_tokenIndex >= m_tokenBuffer.Length) : true; }
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
            get { return m_tokenIndex; }
        }

        public int TokenCount
        {
            get { return (m_tokenBuffer != null) ? m_tokenBuffer.Length : -1;}
        }
        
        public void Dispose()
        {
            if (Reader != null)
                Reader.Dispose();
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
                var startLine = ++m_line;
                
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                // read the next line if safe to do so
                if (Tokenizer.IsCommentLine(line))
                    continue;

                // split them up into the token buffer and reset the index
                m_tokenBuffer = Tokenizer.SplitTokens(line);
                m_tokenIndex = 0;

                // return number of tokens brought in
                return m_tokenBuffer.Length;
            }

            // end of stream
            return -1;
        }

        protected bool CheckToken(int tokenIndex)
        {
            // verifies index into the buffer is accessible
            if (!IsBufferEmpty)
                return (tokenIndex < m_tokenBuffer.Length);

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
                return (m_tokenBuffer[index]);

            // failed to get token :(
            return null;
        }
        
        public string PopToken(int offset = 0)
        {
            m_tokenIndex += offset;

            // don't let the user pop too many values
            if (m_tokenIndex < 0)
                throw new InvalidOperationException("PopToken() -- offset caused negative index, too many values popped!");
            
            return GetToken(m_tokenIndex++);
        }

        private string ReadTokenInternal()
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

                token = GetToken(m_tokenIndex++);
            }

            return token;
        }
        
        /// <summary>
        /// Reads the next valid token from the buffer and increments the token index.
        /// </summary>
        /// <returns>The next valid token from the buffer; otherwise, null.</returns>
        public string ReadToken()
        {
            var token = ReadTokenInternal();
            
            return token;
        }

        /// <summary>
        /// Gets the next token from the buffer without incrementing the token index or filling the buffer.
        /// </summary>
        /// <returns>The next token from the buffer; otherwise, null.</returns>
        public string PeekToken()
        {
            return GetToken(m_tokenIndex);
        }

        public string PeekToken(int offset)
        {
            return GetToken(m_tokenIndex + offset);
        }

        public bool Seek(int offset)
        {
            m_tokenIndex += offset;

            if (m_tokenIndex < 0)
                throw new InvalidOperationException("Seek() -- offset caused negative index!");

            return CheckToken(m_tokenIndex);
        }

        public int FindPattern(string[] tokens, int index)
        {
            if (EndOfStream)
                throw new InvalidOperationException("GetTokensIndex() -- end of stream exception.");

            // do not look past the end of the line
            // the user may decide to move to the next line if necessary
            if (EndOfLine || ((index + tokens.Length) >= m_tokenBuffer.Length))
                return -1;

            // index into the tokens we're looking for
            var tokenIndex = 0;

            // iterate through the tokens available in the buffer
            for (int i = index; i < m_tokenBuffer.Length; i++)
            {
                if (m_tokenBuffer[i] == tokens[tokenIndex])
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
            return FindPattern(tokens, m_tokenIndex);
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

            while (!match && (token = ReadTokenInternal()) != null)
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
