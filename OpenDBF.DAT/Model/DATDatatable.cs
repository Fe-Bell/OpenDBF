using OpenDBF.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace OpenDBF.DAT.Model
{
    [Serializable]
    public class DATDatatable
    {
        public string GUID { get; set; }
        public List<List<ICollectableObject>> Collections { get; set; }

        public DATDatatable()
        {
            Collections = new List<List<ICollectableObject>>();
        }

        public static DATDatatable Generate()
        {
            var db = new DATDatatable();
            db.GUID = Guid.NewGuid().ToString().ToUpper();

            return db;
        }
        public bool Add<T>(T value) where T : ICollectableObject
        {
            var _collection = Collections.FirstOrDefault(lst => lst.Any(x => x is T));
            if(_collection is null)
            {
                var _col = new List<T>();
                _col.Add(value);

                Collections.Add(_col.Select(x => x as ICollectableObject).ToList());
            }
            else
            {
                _collection.Add(value);
            }
            
            return true;
        }
        public bool Remove<T>(T value) where T : ICollectableObject
        {
            var _collection = Collections.FirstOrDefault(lst => lst.Any(x => x is T));
            if (_collection != null)
            {
                return _collection.Remove(value);
            }

            return false;
        }
        public T FindObjectByGUID<T>(string guid) where T : class, ICollectableObject
        {
            var _collection = Collections.FirstOrDefault(lst => lst.Any(x => x is T));
            if (_collection != null)
            {
                return _collection.FirstOrDefault(x => x.GUID == guid) as T;
            }

            return null;
        }
        public IEnumerable<T> GetCollection<T>(Func<T, bool> predicate = null) where T : ICollectableObject
        {
            var _collection = Collections.FirstOrDefault(lst => lst.Any(x => x is T));
            if (_collection is null)
            {
                return null;
            }

            var _col = _collection.Select(x => (T)x);
            return predicate is null ? _col : _col.Where(predicate);
        }
    }
}
