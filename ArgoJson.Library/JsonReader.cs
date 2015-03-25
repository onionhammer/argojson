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

        private int _step = 0;

        private int _index = 0;

        private bool _atEnd = false;

        #endregion
        
        #region Constructor

        public JsonReader(TextReader reader)
        {
            _reader = reader;
            _buffer = new char[BUFFER_SIZE * 2];
        }

        #endregion

        #region Methods

        /// <summary>
        /// Ensures there is something in the buffer to read
        /// </summary>
        private void EnsureBuffer()
        {
            if (_atEnd) return;

            _step    = (_step == 0 ? 1 : 0);
            _index   = _step * BUFFER_SIZE;
            int read = _reader.ReadBlock(_buffer,
                _step * BUFFER_SIZE, BUFFER_SIZE) +
                _step * BUFFER_SIZE;

            if (read < BUFFER_SIZE)
                _atEnd = true;
        }

        /// <summary>
        /// Skips any leading whitespace
        /// </summary>
        private void SkipWhitespace()
        {
            _index = Array.IndexOf(_buffer, '{',
                _step * BUFFER_SIZE + _index, BUFFER_SIZE) -
                _step * BUFFER_SIZE + 1;
        }

        /// <summary>
        /// Skips to and past (by 1) the character specified
        /// </summary>
        private void SkipPast(char value)
        {
            EnsureBuffer();

            _index = Array.IndexOf(_buffer, value,
                _step * BUFFER_SIZE + _index, BUFFER_SIZE) -
                _step * BUFFER_SIZE + 1;
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
        /// Read a datetime value
        /// </summary>
        public DateTime? ReadDateValue()
        {
            DateTime result;
            if (DateTime.TryParse(ReadStringValue(), out result))
                return result;

            return null;
        }

        /// <summary>
        /// Reads a string value
        /// </summary>
        public string ReadStringValue()
        {
            SkipPast('"');

            return null;
        }

        // TODO:
        //ReadInt()
        //ReadLong()
        //ReadFloat()
        //ReadDouble()

        #endregion

        public bool ContinueArray()
        {
            return false;
        }

        public void ReadEndArray()
        {
        }

        public void ReadEndObject()
        {
        }

        public void Dispose()
        {
        }

        #endregion

    }
}