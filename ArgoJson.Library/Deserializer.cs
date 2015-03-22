using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson
{
    public class Deserializer
    {
        #region Fields

        #endregion

        #region Constructor

        static Deserializer()
        {
            // TODO: Define dynamic assembly
        }

        protected Deserializer() { }

        #endregion

        #region Methods

        public static T Deserialize<T>(string source)
        {
            using (var sr = new StringReader(source))
                return Deserialize<T>(sr); 
        }

        public static T Deserialize<T>(Stream source)
        {
            using (var sr = new StreamReader(source))
                return Deserialize<T>(sr); 
        }

        public static T Deserialize<T>(TextReader source)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
