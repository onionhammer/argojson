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

        const int BUFFER_SIZE = 4096;

        private readonly TextReader _reader;

        private readonly char[] _buffer;

        private readonly char[] _overflow;

        private int _max = 0, _index = 0;

        #endregion
        
        #region Constructor

        public JsonReader(TextReader reader)
        {
            _reader   = reader;
            _buffer   = new char[BUFFER_SIZE];
            _overflow = new char[BUFFER_SIZE];
        }

        #endregion

        #region Methods

        private void ReadNext()
        {
            _index = 0;
            _max   = _reader.Read(_buffer, 0, BUFFER_SIZE);
        }

        /// <summary>
        /// Skips any leading whitespace
        /// </summary>
        private void SkipWhitespace()
        {
            // Check if there is enough in the buffer
            
        
        }

        /// <summary>
        /// Skips to and past (by 1) the character specified
        /// </summary>
        private void SkipPast(char value)
        {
        }

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

        /// <summary>
        /// Read a string and skip past ":" and any whitespace
        /// </summary>
        public bool ReadPropertyStart(out string property)
        {
            property = ReadStringValue();
            SkipPast(':');
            return false;
        }

        #region Value Methods

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

        /// <summary>
        /// Reads a string value
        /// </summary>
        public string ReadStringValue()
        {
            SkipPast('"');

            // TODO - Read until end-quote, ignoring escape char + 1.

            return null;
        }

        // TODO:
        //ReadInt()
        //ReadLong()
        //ReadFloat()
        //ReadDouble()

        #endregion

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

        public void Dispose()
        {
            /* Do Nothing */
        }

        #endregion

    }
}