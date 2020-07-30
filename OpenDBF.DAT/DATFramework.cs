using OpenDBF.DAT.Generic;
using OpenDBF.DAT.Model;
using OpenDBF.Shared.Generic;
using OpenDBF.Shared.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace OpenDBF.DAT
{
    /// <summary>
    /// Database framework based on object to byte array serialization.
    /// </summary>
    public class DATFramework : IDatabaseFramework
    {
        #region Private fields

        /// <summary>
        /// An active instance of the database.
        /// </summary>
        private DATDatatable database;
        /// <summary>
        /// Object used for cross-thread protection.
        /// </summary>
        private static readonly object lockObject = new object();
        /// <summary>
        /// Mutex to protect file access from multiple processes.
        /// </summary>
        protected static Mutex mutex = null;

        #endregion

        #region Public events

        /// <summary>
        /// Event fired when there is a change in one of the database files.
        /// </summary>
        public event OnDatabaseChangedEventHandler OnDatabaseChanged = null;
        /// <summary>
        /// Delegate event handler for the OnDatabaseChanged event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void OnDatabaseChangedEventHandler(object sender, OnDatabaseChangedEventArgs e);
        /// <summary>
        /// EventArgs delivered with OnDatabaseChanged event.
        /// </summary>
        public class OnDatabaseChangedEventArgs : EventArgs
        {
            public string DatabaseName { get; private set; }
            public DateTime Time { get; private set; }

            public OnDatabaseChangedEventArgs(string databaseName, DateTime time)
            {
                DatabaseName = databaseName;
                Time = time;
            }
        }

        /// <summary>
        /// EventArgs delivered with OnDatabaseImported event.
        /// </summary>
        public class OnDatabaseImportedEventArgs
        {
            public string DatabaseName { get; private set; }
            public DateTime Time { get; private set; }

            public OnDatabaseImportedEventArgs(string databaseName, DateTime date)
            {
                DatabaseName = databaseName;
                Time = Time;
            }
        }
        /// <summary>
        /// Delegate event handler for the OnDatabaseImported event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void OnDatabaseImportedEventHandler(object sender, OnDatabaseImportedEventArgs e);
        /// <summary>
        /// Event fired when a database import is completed.
        /// </summary>
        public OnDatabaseImportedEventHandler OnDatabaseImported = null;

        /// <summary>
        /// EventArgs delivered with OnDatabaseExported event.
        /// </summary>
        public class OnDatabaseExportedEventArgs
        {
            public string DatabaseName { get; private set; }
            public DateTime Time { get; private set; }

            public OnDatabaseExportedEventArgs(string databaseName, DateTime date)
            {
                DatabaseName = databaseName;
                Time = Time;
            }
        }
        /// <summary>
        /// Delegate event handler for the OnDatabaseExported event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void OnDatabaseExportedEventHandler(object sender, OnDatabaseExportedEventArgs e);
        /// <summary>
        /// Event fired when a database export is completed.
        /// </summary>
        public OnDatabaseExportedEventHandler OnDatabaseExported = null;

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the current filename of the database with extension.
        /// </summary>
        public string CurrentFileNameWithExtension => CurrentFileName + ".dat";
        /// <summary>
        /// Returns the current filename of the database.
        /// </summary>
        public string CurrentFileName { get; protected set; }
        /// <summary>
        /// Returns the current workspace of the database.
        /// </summary>
        public string CurrentWorkspace { get; protected set; }

        #endregion

        public DATFramework()
        {
            database = DATDatatable.Generate();

            //Xamarin does not currently support mutexes for some mobile platforms.
            //When a new Mutex instance is created by Xamarin.Forms, a NotSupportedException/NotImplementedException is thrown.
            //The following code handles this exception. The mutex will be ignored because it is null.
            try
            {
                mutex = new Mutex(false, "OpenDBF.DATMutex");
            }
            catch (Exception ex)
            {
                if (ex is NotSupportedException || ex is NotImplementedException)
                {
                    Console.WriteLine("Cross-process disabled.");
                }
            }
        }

        #region Private methods

        /// <summary>
        /// Gets a the next available ID in a collection of collectables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startID"></param>
        /// <returns></returns>
        private uint GetNextID<T>(uint startID = 0) where T : ICollectableObject
        {
            //Gets all elements in the collection
            var _collection = database.GetCollection<T>();

            if (_collection != null && _collection.Any())
            {
                //Gets a list of used IDs
                IEnumerable<int> idList = _collection.Select(x => (int)x.EID);
                return (uint)Enumerable.Range((int)startID, Int32.MaxValue).Except(idList).First();
            }

            return startID;
        }
        /// <summary>
        /// Gets the next non repeated GUID in a collection of collectables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private string GetNextGUID<T>() where T : IIdentifiableObject
        {
            string newGUID = Guid.NewGuid().ToString().ToUpper();

            //Gets all elements in the collection
            var _collection = database.Collections.FirstOrDefault(x => x is IEnumerable<T>);

            if (_collection is null || !(_collection as IEnumerable<T>).Any())
            {
                return newGUID;
            }
            else
            {
                if ((_collection as IEnumerable<T>).Any(collectableObject => collectableObject.GUID == newGUID))
                {
                    return GetNextGUID<T>();
                }
                else
                {
                    return newGUID;
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Clears the current database framework.
        /// </summary>
        public void Dispose()
        {
            //StopMonitoringDirectory();

            //Reset datatable
            database = DATDatatable.Generate();

            //Clears internal data
            CurrentWorkspace = string.Empty;
            CurrentFileName = string.Empty;

            //Disengage events
            if (!OnDatabaseChanged.IsNull())
            {
                OnDatabaseChanged = null;
            }
            if (!OnDatabaseExported.IsNull())
            {
                OnDatabaseExported = null;
            }
            if (!OnDatabaseImported.IsNull())
            {
                OnDatabaseImported = null;
            }
        }
        /// <summary>
        /// Saves a selection of items in its respective database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Commit()
        {
            if (database != null)
            {
                var _dat = database.Serialize();
                if (_dat != null)
                {
                    if (!Directory.Exists(CurrentWorkspace))
                    {
                        Directory.CreateDirectory(CurrentWorkspace);
                    }

                    var fullFilePath = Path.Combine(CurrentWorkspace, CurrentFileNameWithExtension);
                    File.WriteAllBytes(fullFilePath, _dat);
                }
            }
        }
        /// <summary>
        /// Deletes a table of items from the current datatable.
        /// </summary>
        /// <typeparam name="T">An ICollectableObject</typeparam>
        /// <returns>Items deleted (true) or not (false)</returns>
        public bool DropTable<T>() where T : ICollectableObject
        {
            if(database != null)
            {
                return database.Collections.RemoveAll(lst => lst.Any(x => x is T)) != 0;
            }

            return false;
        }
        /// <summary>
        ///  Exports a group of database files to a single zipped file.
        /// </summary>
        /// <param name="pathToSave">A folder where the database file will be created.</param>
        /// <param name="filename">A name for the database file.</param>
        /// <param name="fileExtension">An extension for the file.</param>
        public void Pack(string pathToSave, string filename, string fileExtension = ".db")
        {
            //Checks if the path to save ends with a slash and if not, adds it.
            if (!pathToSave.Last().Equals('\\'))
            {
                pathToSave += '\\';
            }

            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if (mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        //Make sure the specified directory exists, else creates it
                        if (!Directory.Exists(pathToSave))
                        {
                            Directory.CreateDirectory(pathToSave);
                        }

                        //1. Create temp folder with the database files
                        string tempFolder = Path.Combine(pathToSave, Guid.NewGuid().ToString().ToUpper());
                        if (!Directory.Exists(Path.Combine(tempFolder)))
                        {
                            Directory.CreateDirectory(tempFolder);
                        }

                        //2. Copy database file to the temp folder
                        //Will copy all files matching a database class name
                        var filesWithDBName = Directory.GetFiles(CurrentWorkspace, CurrentFileName + ".*", SearchOption.TopDirectoryOnly);
                        if (!filesWithDBName.Any())
                        {
                            Commit();

                            filesWithDBName = Directory.GetFiles(CurrentWorkspace, CurrentFileName + ".*", SearchOption.TopDirectoryOnly);
                            if (!filesWithDBName.Any())
                            {
                                return;
                            }
                        }

                        //Copy files to temp folder
                        foreach(var f in filesWithDBName)
                        {
                            File.Copy(f, Path.Combine(tempFolder, System.IO.Path.GetFileName(f)), true);
                        }

                        //3. Archive this temp folder to the database file
                        string fullFilePath = Path.Combine(pathToSave, filename + fileExtension);
                        if (File.Exists(fullFilePath))
                        {
                            //If the file already exists, delete and recreate
                            File.Delete(fullFilePath);
                        }

                        ZipFile.CreateFromDirectory(tempFolder, fullFilePath, CompressionLevel.Optimal, false);

                        //4. Delete temp folder
                        Directory.Delete(tempFolder, true);

                        //Fire events.
                        OnDatabaseExported?.Invoke(this, new OnDatabaseExportedEventArgs(fullFilePath, DateTime.Now));
                    }
                    catch
                    {
                        //The exception is caught after the mutex is release.
                        throw;
                    }
                    finally
                    {
                        if (mutex != null)
                        {
                            mutex.ReleaseMutex();
                        }
                    }
                }
                catch
                {
                    //Exception is caught here and sent to the caller.
                    throw;
                }
                finally
                {

                }
            }
        }
        /// <summary>
        /// Gets a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<T> Get<T>(Func<T, bool> predicate = null) where T : ICollectableObject
        {
            return database.GetCollection(predicate);
        }
        /// <summary>
        /// Imports a zipped database file.
        /// </summary>
        /// <param name="fileToImport"></param>
        /// <param name="exportPath"></param>
        public void Unpack(string fileToImport, string exportPath)
        {
            //This stores the path where the file should be unzipped to,
            //including any subfolders that the file was originally in.
            string fileUnzipFullPath = string.Empty;

            //This is the full name of the destination file including
            //the path
            string fileUnzipFullName = string.Empty;

            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if (mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        //Opens the zip file up to be read
                        using (ZipArchive archive = ZipFile.OpenRead(fileToImport))
                        {
                            //Loops through each file in the zip file
                            foreach (ZipArchiveEntry file in archive.Entries)
                            {
                                //Identifies the destination file name and path
                                fileUnzipFullName = Path.Combine(exportPath, file.FullName);

                                //Extracts the files to the output folder in a safer manner
                                if (File.Exists(fileUnzipFullName))
                                {
                                    File.Delete(fileUnzipFullName);
                                }

                                //Calculates what the new full path for the unzipped file should be
                                fileUnzipFullPath = Path.GetDirectoryName(fileUnzipFullName);

                                //Creates the directory (if it doesn't exist) for the new path
                                Directory.CreateDirectory(fileUnzipFullPath);

                                //Extracts the file to (potentially new) path
                                file.ExtractToFile(fileUnzipFullName);
                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if (mutex != null)
                        {
                            mutex.ReleaseMutex();
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {

                }
            }

            OnDatabaseImported?.Invoke(this, new OnDatabaseImportedEventArgs(fileUnzipFullName, DateTime.Now));
        }
        /// <summary>
        /// Inserts a collection of items in the active database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Insert<T>(IEnumerable<T> items) where T : ICollectableObject, new()
        {
            Insert(items.ToArray());
        }
        /// <summary>
        /// Inserts one of more items in the active database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Insert<T>(params T[] items) where T : ICollectableObject, new()
        {
            if (items is null || !items.Any())
            {
                throw new NullReferenceException("No items to insert.");
            }

            foreach(var item in items)
            {
                item.EID = GetNextID<T>();
                item.GUID = GetNextGUID<T>();

                database.Add(item);
            }
        }
        /// <summary>
        /// Removes a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Remove<T>(IEnumerable<T> items) where T : ICollectableObject, new()
        {
            Remove(items.ToArray());
        }
        /// <summary>
        /// Removes a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Remove<T>(params T[] items) where T : ICollectableObject, new()
        {
            if (items is null || !items.Any())
            {
                throw new NullReferenceException("No items to insert.");
            }

            foreach (var item in items)
            {
                database.Remove(item);
            }
        }
        /// <summary>
        /// Sets the workspace of this class. Type parameters must all inherit from IDatabase.
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="databaseTypes"></param>
        public void SetWorkspace(string workspace, string databaseName = null)
        {
            //Checks parameters
            //---------------------------------------------------------------------------
            if (string.IsNullOrEmpty(workspace))
            {
                throw new Exception("Cannot have a null workspace.");
            }
            //---------------------------------------------------------------------------

            //Checks if the workspace path ends with a slash and if not, adds it.
            if (!workspace.Last().Equals('\\'))
            {
                workspace += '\\';
            }

            //Saves the database tyoes and the workspace in the current instance.
            CurrentFileName = string.IsNullOrEmpty(databaseName) ? StringUtils.RandomString(8) : Path.GetFileNameWithoutExtension(databaseName);
            CurrentWorkspace = workspace;

            var fullFilePath = Path.Combine(CurrentWorkspace, CurrentFileNameWithExtension);
            if (File.Exists(fullFilePath))
            {
                var _dat = File.ReadAllBytes(fullFilePath);
                database = _dat.Deserialize<DATDatatable>();
            }
            else
            {
                //Create random instance of the database
                database = DATDatatable.Generate();
                if (database != null)
                {
                    //OpenDBF.DAT.Generic.ExtensionMethods.Serialize(datatable);
                    var _dat = database.Serialize();
                    if (_dat != null)
                    {
                        if (!Directory.Exists(CurrentWorkspace))
                        {
                            Directory.CreateDirectory(CurrentWorkspace);
                        }

                        File.WriteAllBytes(fullFilePath, _dat);
                    }
                }
            }

            //Stop monitoring the current workspace
            //StopMonitoringDirectory();

            //Creates the workspace folder if it does not exist.
            if (!Directory.Exists(workspace))
            {
                Directory.CreateDirectory(workspace);
            }

            //Start monitoring the current workspace
            //StartMonitoringDirectory(workspace);
        }
        /// <summary>
        /// Updates a selection of items in its respective database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Update<T>(IEnumerable<T> items) where T : ICollectableObject, new()
        {
            Remove(items);
            Insert(items);
        }

        #endregion
    }
}
