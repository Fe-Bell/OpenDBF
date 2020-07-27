using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.Shared.Interface
{
    public interface IDatabaseFramework
    {
        void ClearHandler();
        void Commit();
        void DeleteDatabase();
        void ExportDatabase(string pathToSave, string filename, string fileExtension = ".db");
        IEnumerable<T> Get<T>(Func<T, bool> predicate = null) where T : ICollectableObject;      
        void ImportDatabase(string fileToImport, string exportPath);     
        void Insert<T>(IEnumerable<T> items) where T : ICollectableObject, new();
        void Insert<T>(params T[] items) where T : ICollectableObject, new();
        void Remove<T>(IEnumerable<T> items) where T : ICollectableObject, new();
        void Remove<T>(params T[] items) where T : ICollectableObject, new();        
        void SetWorkspace(string workspace, string databaseName = null);
        void Update<T>(IEnumerable<T> items) where T : ICollectableObject, new();
    }
}
