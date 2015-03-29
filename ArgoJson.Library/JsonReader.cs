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

        private bool ReadNext()
        {
            _index = 0;
            _max   = _reader.Read(_buffer, 0, BUFFER_SIZE);

            return _max > 0;
        }

        /// <summary>
        /// Skip reading until the stopping char is found, then read past that.
        /// </summary>
        private void SkipPast(char stoppingChar)
        {
            while (true)
            {
                var match = Array.IndexOf(_buffer, stoppingChar, _index, _max - _index);

                if (match > -1)
                {
                    _index = match + 1;
                    return;
                }
                
                if (ReadNext() == false)
                    return;
            }
        }

        /// <summary>
        /// Read all until there is a stopping character, then skip past that
        /// </summary>
        private void ParsePast(char stoppingChar)
        {
            while (true)
            {
                var match = Array.IndexOf(_buffer, stoppingChar, _index, _max - _index);

                // Read data into builder
                if (match > -1)
                {
                    _builder.Append(_buffer, _index, match - _index);
                    _index = match + 1;
                    return;
                }

                _builder.Append(_buffer, _index, _max - _index);
                
                if (ReadNext() == false)
                    return;
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
        public void ReadStartObject()
        {
            SkipPast('{');
        }

        /// <summary>
        /// Read to '['
        /// </summary>
        public void ReadStartArray()
        {
            SkipPast('[');
        }

        #region Value Methods

        /// <summary>
        /// Reads a string value
        /// </summary>
        public string ReadStringValue()
        {
            SkipPast('"');
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

        // TODO:
        //ReadInt()
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
            return false;
        }

        /// <summary>
        /// Read to ']'
        /// </summary>
        public void ReadEndArray()
        {
            SkipPast(']');
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