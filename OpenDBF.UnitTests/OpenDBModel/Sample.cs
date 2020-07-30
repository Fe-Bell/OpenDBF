using OpenDBF.Shared.Interface;
using System;
using System.Xml.Serialization;

namespace OpenDBF.UnitTests.Models
{
    [Serializable]
    public class Sample : ICollectableObject
    {
        [XmlAttribute]
        public uint EID { get; set; }
        [XmlAttribute]
        public string GUID { get; set; }

        public string SomeData { get; set; }

        public Sample()
        {

        }
    }
}
