using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.Shared.Interface
{
    public interface IDatabaseFramework : IDisposable
    {
        /// <summary>
        /// Returns the current filename of the database with extension.
        /// </summary>
        string CurrentFileNameWithExtension { get; }
        /// <summary>
        /// Returns the current filename of the database.
        /// </summary>
        string CurrentFileName { get; }
        /// <summary>
        /// Returns the current workspace of the database.
        /// </summary>
        string CurrentWorkspace { get; }

        void Commit();
        bool DropTable<T>() where T : ICollectableObject;       
        IEnumerable<T> Get<T>(Func<T, bool> predicate = null) where T : ICollectableObject;               
        void Insert<T>(IEnumerable<T> items) where T : ICollectableObject;
        void Insert<T>(params T[] items) where T : ICollectableObject;
        void Pack(string pathToSave, string filename, string fileExtension = ".db");
        void Remove<T>(IEnumerable<T> items) where T : ICollectableObject;
        void Remove<T>(params T[] items) where T : ICollectableObject;        
        void SetWorkspace(string workspace, string databaseName = null);
        void Unpack(string fileToImport, string exportPath);
        void Update<T>(IEnumerable<T> items) where T : ICollectableObject;
    }
}
