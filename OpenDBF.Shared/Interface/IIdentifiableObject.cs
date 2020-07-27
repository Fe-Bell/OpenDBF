using System.Xml.Serialization;

namespace OpenDBF.Shared.Interface
{
    /// <summary>
    /// Interface for database implementations.
    /// </summary>
    public interface IIdentifiableObject
    {
        //All identifiable objects must implement the following properties.

        /// <summary>
        /// The GUID is an internal unique identification of an object.
        /// </summary>
        [XmlAttribute]
        string GUID { get; set; }
    }
}
