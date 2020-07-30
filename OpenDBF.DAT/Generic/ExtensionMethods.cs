using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace OpenDBF.DAT.Generic
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Serializes a T object to byte array
        /// </summary>
        /// <typeparam name="T">A generic type T</typeparam>
        /// <param name="value">An object of type T</param>
        /// <returns>A byte array</returns>
        public static byte[] Serialize<T>(this T value) where T : class
        {
            byte[] arr = null;

            if (value != null)
            {
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, value);

                    arr = ms.ToArray();
                }
            }

            return arr;
        }

        /// <summary>
        /// Serializes byte array to T.
        /// </summary>
        /// <typeparam name="T">A generic type T</typeparam>
        /// <param name="value">A byte array</param>
        /// <returns>An object of type T</returns>
        public static T Deserialize<T>(this byte[] value) where T : class
        {
            T obj = null;

            if(value != null && value.Length > 0)
            {
                using (var memStream = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    memStream.Write(value, 0, value.Length);
                    memStream.Seek(0, SeekOrigin.Begin);

                    obj = (T)bf.Deserialize(memStream);
                }
            }
              
            return obj;
        }
    }
}
