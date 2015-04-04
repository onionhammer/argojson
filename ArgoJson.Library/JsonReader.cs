using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson
{
    internal sealed class JsonReader : IDisposable
    {
        #region Fields

        const int BUFFER_SIZE = 5;// 4096;

        private readonly TextReader _reader;

        private readonly StringBuilder _builder;

        private readonly char[] _buffer;

        private int _max = 0, _index = 0;

        #endregion
        
        #region Constructor

        public JsonReader(TextReader reader)
        {
            _reader  = reader;
            _buffer  = new char[BUFFER_SIZE];
            _builder = new StringBuilder(BUFFER_SIZE);

            ReadNext();
        }

        #endregion

        #region Methods

        private static char IndexOf(char[] buffer, char match, ref int index, int count)
        {
            for (int i = 0; i < count; ++index, ++i)
            {
                if (match == buffer[index])
                    return match;
            }

            return '\0';
        }

        private static char IndexOf(char[] buffer, char match1, char match2, ref int index, int count)
        {
            for (int i = 0; i < count; ++index, ++i)
            {
                if (match1 == buffer[index])
                    return match1;

                if (match2 == buffer[index])
                    return match2;
            }

            return '\0';
        }

        private static char IndexOf(char[] buffer, char match1, char match2, char match3, char match4, ref int index, int count)
        {
            for (int i = 0; i < count; ++index, ++i)
            {
                if (match1 == buffer[index])
                    return match1;
                if (match2 == buffer[index])
                    return match2;
                if (match3 == buffer[index])
                    return match3;
                if (match4 == buffer[index])
                    return match4;
            }

            return '\0';
        }

        private bool ReadNext()
        {
            _index = 0;
            _max   = _reader.Read(_buffer, 0, BUFFER_SIZE);

            return _max > 0;
        }

        /// <summary>
        /// Skip reading until the stopping char is found, then read past that.
        /// </summary>
        private char SkipPast(char stoppingChar, char exceptionChar)
        {
            while (true)
            {
                var match = IndexOf(_buffer, stoppingChar, exceptionChar, ref _index, _max - _index);

                if (match != '\0')
                {
                    ++_index;
                    return match;
                }

                if (ReadNext() == false)
                    return match;
            }
        }

        /// <summary>
        /// Skip reading until the stopping char is found, then read past that.
        /// </summary>
        private char SkipPast(char stoppingChar)
        {
            while (true)
            {
                var match = IndexOf(_buffer, stoppingChar, ref _index, _max - _index);

                if (match != '\0')
                {
                    ++_index;
                    return match;
                }

                if (ReadNext() == false)
                    return match;
            }
        }

        /// <summary>
        /// Read all until there is a stopping character, then skip past that
        /// </summary>
        private void ParsePast(char stoppingChar)
        {
            while (true)
            {
                int start = _index;
                var match = IndexOf(_buffer, stoppingChar, ref _index, _max - _index);

                // Read data into builder
                if (match == stoppingChar)
                {
                    _builder.Append(_buffer, start, _index - start);
                    ++_index;
                    return;
                }

                _builder.Append(_buffer, start, _max - start);
                
                if (ReadNext() == false)
                    return;
            }
        }

        /// <summary>
        /// Reads all until there is a stopping character
        /// </summary>
        private bool ParseToEnd()
        {
            // 'n', ',', ']', '}'
            while (true)
            {
                int start = _index;
                var match = IndexOf(_buffer, 'n', ',', ']', '}', ref _index, _max - _index);

                switch (match)
                {
                    case ',':
                    case ']':
                    case '}':
                        _builder.Append(_buffer, start, _index - start);
                        return true;

                    case 'n':
                        return false;

                    default:
                        _builder.Append(_buffer, start, _max - start);
                        if (ReadNext() == false)
                            return false;
                        continue;
                }
            }
        }


        /// <summary>
        /// Skip until there is a non-whitespace character
        /// </summary>
        private void SkipWhitespace()
        {
            // Check if there is enough in the buffer
            //Char.IsWhiteSpace()
        }

        #region Public Methods

        /// <summary>
        /// Read to '{'
        /// </summary>
        public bool ReadStartObject()
        {
            // TODO - Check for 'null'

            SkipPast('{', 'n');

            return true;
        }

        /// <summary>
        /// Read to '['
        /// </summary>
        public bool ReadStartArray()
        {
            // TODO - Check for 'null'

            if (SkipPast('[', 'n') == 'n')
            {
                return false;
            }

            return true;
        }

        #region Value Methods

        public void SkipNullValue()
        {
            SkipPast('l');
            var remaining = _max - _index;
            if (remaining == 0) ReadNext();
            ++_index; // Read the next 'l'
        }

        /// <summary>
        /// Reads a string value
        /// </summary>
        public string ReadStringValue()
        {
            // TODO - Check for 'null'

            if (SkipPast('"', 'n') == 'n')
            {
                SkipNullValue();
                return null;
            }

            _builder.Clear();

            while (true)
            {
                ParsePast('"');
                var len = _builder.Length;

                if (len > 1 &&
                    _builder[len - 1] == '\\' &&
                    _builder[len - 2] != '\\')
                {
                    // The quote was escaped
                    _builder.Append('"');
                    continue;
                }
                else
                {
                    var result = _builder.ToString();

                    // Unescape the result
                    return Helpers.Unescape(result);
                }
            }
        }

        /// <summary>
        /// Read a GUID value
        /// </summary>
        /// <returns>Returns a guid</returns>
        public bool ReadGuidValue(out Guid value)
        {
            var strValue = ReadStringValue();

            if (strValue == null)
            {
                value = default(Guid);
                return false;
            }
            else
                return Guid.TryParse(strValue, out value);
        }
        
        /// <summary>
        /// Read a datetime value
        /// </summary>
        public bool ReadDateValue(out DateTime value)
        {
            var strValue = ReadStringValue();

            if (strValue == null)
            {
                value = default(DateTime);
                return false;
            }
            else
                return DateTime.TryParse(strValue, out value);
        }

        public bool ReadIntValue(out int value)
        {
            _builder.Clear();

            if (ParseToEnd())
                return int.TryParse(_builder.ToString(), out value);
            else
            {
                value = default(int);
                SkipNullValue();
                return false;
            }
        }

        // TODO:
        //ReadLong()
        //ReadFloat()
        //ReadDouble()

        #endregion

        /// <summary>
        /// Read a string and skip past ":" and any whitespace
        /// </summary>
        public bool ReadPropertyStart(out string property)
        {
            // TODO: Handle 'false' case where there are no more properties
            // before an ending '}'

            property = ReadStringValue();
            SkipPast(':');

            return property != string.Empty;
        }

        /// <summary>
        /// Read to ',' or ']'
        /// </summary>
        /// <returns>True if it reads ',', false if ']'</returns>
        public bool ContinueArray()
        {
            return SkipPast(']', ',') == ',';
        }

        /// <summary>
        /// Read to '}'
        /// </summary>
        public void ReadEndObject()
        {
            SkipPast('}');
        }

        /// <summary>
        /// Dispose underlying reader
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }

        #endregion

        #endregion
    }
}