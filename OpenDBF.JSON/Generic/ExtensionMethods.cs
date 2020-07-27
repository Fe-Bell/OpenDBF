using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace OpenDBF.JSON.Generic
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Serializes an object to its JSON element equivalent.
        /// </summary>
        /// <returns></returns>
        public static string Serialize<T>(this T value) where T : class
        {
            byte[] json = null;
            string jsonStr = string.Empty;

            //Create a stream to serialize the object to.  
            using (var ms = new MemoryStream())
            {
                // Serializer the User object to the stream.  
                var ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(ms, value);
                json = ms.ToArray();
            }

            if (json != null && json.Length != 0)
            {
                jsonStr = Encoding.UTF8.GetString(json, 0, json.Length);
            }

            return jsonStr;
        }

        /// <summary>
        /// Deserializes a JSON object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this string json) where T : class
        {
            T obj = default(T);

            if (!string.IsNullOrEmpty(json))
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    obj = (T)ser.ReadObject(ms);
                }
            }

            return obj;
        }
    }
}
