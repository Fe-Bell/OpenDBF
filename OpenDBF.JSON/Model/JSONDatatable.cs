using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.JSON.Model
{
    public class JSONDatatable
    {
        public string GUID { get; set; }

        public List<DataBlob> Root { get; set; } 

        public JSONDatatable()
        {
            Root = new List<DataBlob>();
        }

        public static JSONDatatable Generate()
        {
            var db = new JSONDatatable();
            db.GUID = Guid.NewGuid().ToString().ToUpper();

            return db;
        }
    }
}
