using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.JSON.Model
{
    public class DataBlob
    {
        public string DataType { get; set; }
        public string Data { get; set; }
                
        public static DataBlob Generate(string dataType = null, string data = null)
        {
            var jsonData = new DataBlob();
            jsonData.DataType = dataType;
            jsonData.Data = data;

            return jsonData;
        }
    }
}
