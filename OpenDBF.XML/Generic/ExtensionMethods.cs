using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace OpenDBF.XML.Generic
{
    /// <summary>
    /// Provides extension methods for the OpenDBF.XML.
    /// </summary>
    internal static class ExtensionMethods
    {      
        /// <summary>
        /// Gets a byte array representing the xml file.
        /// </summary>
        /// <param name="xDocument"></param>
        /// <returns></returns>
        public static byte[] GetBytes(this XDocument xDocument)
        {
            byte[] byteArray = null;

            using (MemoryStream ms = new MemoryStream())
            {
                xDocument.Save(ms);
                byteArray = ms.ToArray();
            }

            return byteArray;
        }

        /// <summary>
        /// Serializes a class to its equivalent XDocument.
        /// </summary>
        /// <param name="serializableObject"></param>
        /// <returns></returns>
        public static XDocument Serialize<T>(this T serializableObject, bool useDefaultNamespace = true)
        {
            //if (!typeof(T).IsSerializable && !(typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(typeof(T))))
            //{
            //    throw new InvalidOperationException("A serializable Type is required");
            //}

            const string XMLSCHEMAINSTANCE_ATB = "http://www.w3.org/2001/XMLSchema-instance";
            const string XMLSCHEMA_ATB = "http://www.w3.org/2001/XMLSchema";

            XDocument xDocument = new XDocument();

            using (System.Xml.XmlWriter writer = xDocument.CreateWriter())
            {
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                serializer.Serialize(writer, serializableObject);
            }

            if (!useDefaultNamespace)
            {
                //Removes unwanted namespaces that are added automatically by the serializer

                if (xDocument.Root.Attributes().Any(attrib => attrib.Value == XMLSCHEMAINSTANCE_ATB))
                {
                    xDocument.Root.Attributes().FirstOrDefault(attrib => attrib.Value == XMLSCHEMAINSTANCE_ATB).Remove();
                }

                if (xDocument.Root.Attributes().Any(attrib => attrib.Value == XMLSCHEMA_ATB))
                {
                    xDocument.Root.Attributes().FirstOrDefault(attrib => attrib.Value == XMLSCHEMA_ATB).Remove();
                }
            }

            return xDocument;
        }
        
        /// <summary>
        /// Serializes a class to its equivalent XDocument.
        /// </summary>
        /// <param name="serializableObject"></param>
        /// <returns></returns>
        public static XElement SerializeToXElement<T>(this T serializableObject, bool useDefaultNamespace = true)
        {
            var xml = serializableObject.Serialize(false);
            return xml.Root;
        }

        /// <summary>
        /// Deserializes a XML to its corresponding object.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this XElement value)
        {
            //Checks if the XDocument is null and throws exception if yes.
            if (value is null)
            {
                throw new NullReferenceException("The XDocument cannot be null.");
            }

            T newObj = default(T);

            using (var reader = value.CreateReader())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                newObj = (T)serializer.Deserialize(value.CreateReader());
            }

            return newObj;
        }
        /// <summary>
        /// Deserializes a XML to its corresponding object.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <param name="xDocument"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this XDocument xDocument)
        {    
            //Checks if the XDocument is null and throws exception if yes.
            if(xDocument is null)
            {
                throw new NullReferenceException("The XDocument cannot be null.");
            }

            T newObj = default(T);

            using (var reader = xDocument.CreateReader())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                newObj = (T)serializer.Deserialize(xDocument.CreateReader());
            }

            return newObj;
        }
        /// <summary>
        /// Deserializes a XML from a path to its corresponding object.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this string xmlPath)
        {
            //Checks if the file is a xml file
            if(Path.GetExtension(xmlPath) == "xml")
            {
                throw new Exception(string.Format("The file at {0} is not a xml file.", xmlPath));
            }

            //Checks if the file exists in the path selected
            if (!File.Exists(xmlPath))
            {            
                throw new FileNotFoundException();
            }

            //Loads the XML and serializes it back to the caller.
            XDocument xDocument = XDocument.Load(xmlPath);

            T newObj = default(T);

            using (var reader = xDocument.CreateReader())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                newObj = (T)serializer.Deserialize(xDocument.CreateReader());
            }

            return newObj;
        }
     
        /// <summary>
        /// Deep copies an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToClone"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(this T objectToClone) where T : class
        {
            if (objectToClone is null)
            {
                throw new NullReferenceException("Source cannot be null.");
            }

            Type objectType = typeof(T);

            T newObject = (T)Activator.CreateInstance(objectType);

            foreach(var prop in objectType.GetProperties())
            {
                newObject.GetType().GetProperty(prop.Name).SetValue(newObject, prop.GetValue(objectToClone));
            }

            return newObject;
        }
    }
}
