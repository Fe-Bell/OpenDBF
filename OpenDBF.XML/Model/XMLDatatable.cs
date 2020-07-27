using OpenDBF.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace OpenDBF.XML.Model
{
    [Serializable]
    public class XMLDatatable : IDatabase
    {
        [XmlAttribute]
        public string GUID { get; set; }

        public XMLDatatable()
        {

        }

        public static XMLDatatable GetObject()
        {
            var db = new XMLDatatable();
            db.GUID = Guid.NewGuid().ToString().ToUpper();

            return db;
        }
    }
}
