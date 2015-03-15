using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson.Console
{
    public static class Extensions
    {
        public static string NextString(this Random random, int minLength, int maxLength)
        {
            int length = random.Next(minLength, maxLength);
            var sb     = new StringBuilder(length);

            for (int i = 0; i < length; ++i)
            {
                var c = (char)('a' + (char)random.Next(0, 25));

                if (random.Next(0, 1) == 0)
                    sb.Append(Char.ToUpper(c));
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static Guid NextGuid(this Random random)
        {
            return new Guid(
                a: (int)random.Next(),
                b: (short)random.Next(short.MinValue, short.MaxValue),
                c: (byte)random.Next(0, 255),
                d: (byte)random.Next(0, 255),
                e: (byte)random.Next(0, 255),
                f: (byte)random.Next(0, 255),
                g: (byte)random.Next(0, 255),
                h: (byte)random.Next(0, 255),
                i: (byte)random.Next(0, 255),
                j: (byte)random.Next(0, 255),
                k: (byte)random.Next(0, 255)
            );
        }


        public static DateTime NextDateTime(this Random random)
        {
            return new DateTime(
                random.Next(1990, 2020),
                random.Next(1, 12),
                random.Next(1, 28)
            );
        }

        public static DateTime? NextNullableDateTime(this Random random, float percentNull = .5f)
        {
            var isNull = random.NextDouble() <= percentNull;

            if (isNull) 
                return default(DateTime?);

            return random.NextDateTime();
        }
    }
}