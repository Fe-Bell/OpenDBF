using OpenDBF.Shared.Interface;
using System;
using System.Collections.Generic;

namespace OpenDBF.JSON.Model
{
    public class JSONDatatable : IDatabase
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
