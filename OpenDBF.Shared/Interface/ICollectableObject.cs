using System.Xml.Serialization;

namespace OpenDBF.Shared.Interface
{
    /// <summary>
    /// Interface for database objects.
    /// </summary>
    public interface ICollectableObject : IIdentifiableObject
    {
        //All database objects must implement the following properties.

        /// <summary>
        /// The enumeration ID displays the order of an object in a database.
        /// </summary>
        [XmlAttribute]
        uint EID { get; set; }      
    }
}
