using System.IO;
using System.Text;

namespace ArgoJson
{
    public class Serializer
    {
        #region Constructor

        protected Serializer() { }

        #endregion

        #region Methods

        public static string Serialize(object value)
        {
            var builder = new StringBuilder(256);

            using (var sw = new StringWriter(builder))
                Serialize(value, sw);
            
            return builder.ToString();
        }

        public static void Serialize(object value, Stream destination)
        {
            using (var sw = new StreamWriter(destination))
                Serialize(value, sw);
        }

        public static void Serialize(object value, TextWriter destination)
        {
            var type = value.GetType();

            SerializerNode node;
            SerializerNode.GetHandler(type, out node);

            node._serialize(value, destination);
        }

        #endregion
    }
}