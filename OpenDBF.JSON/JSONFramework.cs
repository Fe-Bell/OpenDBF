using OpenDBF.JSON.Generic;
using OpenDBF.JSON.Model;
using OpenDBF.Shared.Generic;
using OpenDBF.Shared.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace OpenDBF.JSON
{
    public class JSONFramework : IDatabaseFramework
    {
        #region Private fields

        private JSONDatatable jsonDatatable = null;
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
        public string CurrentFileNameWithExtension => CurrentFileName + ".json";
        /// <summary>
        /// Returns the current filename of the database.
        /// </summary>
        public string CurrentFileName { get; protected set; }
        /// <summary>
        /// Returns the current workspace of the database.
        /// </summary>
        public string CurrentWorkspace { get; protected set; }

        #endregion

        public JSONFramework()
        {
            jsonDatatable = JSONDatatable.Generate();

            //Xamarin does not currently support mutexes for some mobile platforms.
            //When a new Mutex instance is created by Xamarin.Forms, a NotSupportedException/NotImplementedException is thrown.
            //The following code handles this exception. The mutex will be ignored because it is null.
            try
            {
                mutex = new Mutex(false, "OpenDBF.JSONMutex");
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
            var _collection = jsonDatatable.Root.FirstOrDefault(x => x is IEnumerable<T>);

            if (_collection != null && (_collection as IEnumerable<T>).Any())
            {
                //Gets a list of used IDs
                var idList = (_collection as IEnumerable<T>).Select(x => x.EID);
                if (idList != null && idList.Any())
                {
                    //Gets a range of numbers
                    var range = NumericUtils.Range(startID, (uint)idList.Count() + 1);
                    return range.Except(idList).First();
                }
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

            var _collection = jsonDatatable.Root.FirstOrDefault(x => x is IEnumerable<T>);
           
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
        /// Clears the current database handler. This causes the all internal properties to be null and the workspace to be deleted.
        /// </summary>
        public void ClearHandler()
        {
            //StopMonitoringDirectory();

            //Checks if the current workspace is still exists
            if (Directory.Exists(CurrentWorkspace))
            {
                //Fixes GitHub issue #4
                //Will delete all files matching a database class name
                var filesWithDBName = Directory.GetFiles(CurrentWorkspace, CurrentFileName + ".*", SearchOption.TopDirectoryOnly);
                if (filesWithDBName.Any())
                {
                    //Deletes XML files associated with the current database
                    filesWithDBName.ForEach(f => File.Delete(f));
                }
            }

            //Reset datatable
            jsonDatatable = JSONDatatable.Generate();

            //Clears internal data
            CurrentWorkspace = null;

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
            if(jsonDatatable is null)
            {
                throw new NullReferenceException("The JSON datatable is null.");
            }

            string filePath = Path.Combine(CurrentWorkspace, CurrentFileNameWithExtension);

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

                        var json = jsonDatatable.Serialize();
                        File.WriteAllText(filePath, json);

                        //Will fire the database changed event when the FileSystemWatcher is unavailable.
                        //This should happen only when using Xamarin.Forms as there is no native support for that class.
                        //if (fileSystemWatcher.IsNull())
                        //{
                        //    OnDatabaseChanged?.Invoke(this, new OnDatabaseChangedEventArgs(CurrentFileName, DateTime.Now));
                        //}

                        OnDatabaseChanged?.Invoke(this, new OnDatabaseChangedEventArgs(CurrentFileName, DateTime.Now));
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
        }
        /// <summary>
        /// Deletes a database file from the computer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void DeleteDatabase()
        {
            string path = Path.Combine(CurrentWorkspace, CurrentFileNameWithExtension);

            if (File.Exists(path))
            {
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

                            File.Delete(path);
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
            }
        }
        /// <summary>
        ///  Exports a group of database files to a single zipped file.
        /// </summary>
        /// <param name="pathToSave">A folder where the database file will be created.</param>
        /// <param name="filename">A name for the database file.</param>
        /// <param name="fileExtension">An extension for the file.</param>
        public void ExportDatabase(string pathToSave, string filename, string fileExtension = ".db")
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
                        if (!mutex.IsNull())
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
                        filesWithDBName.ForEach(f => File.Copy(f, Path.Combine(tempFolder, System.IO.Path.GetFileName(f)), true));

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
                        if (!mutex.IsNull())
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
            List<T> _collection = null;
            var _dataBlob = jsonDatatable.Root.FirstOrDefault(x => x.DataType == typeof(List<T>).FullName);
            if (_dataBlob != null)
            {               
                _collection = (List<T>)typeof(OpenDBF.JSON.Generic.ExtensionMethods).
                    GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(typeof(List<T>)).Invoke(null, new object[] { _dataBlob.Data });

                //A collection or null should be returned.
                return predicate is null ? _collection : _collection.Where(predicate);
            }

            return null;
        }
        /// <summary>
        /// Imports a zipped database file.
        /// </summary>
        /// <param name="fileToImport"></param>
        /// <param name="exportPath"></param>
        public void ImportDatabase(string fileToImport, string exportPath)
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
                        if (!mutex.IsNull())
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
                        if (!mutex.IsNull())
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

            List<T> _collection = null;
            var _dataBlob = jsonDatatable.Root.FirstOrDefault(x => x.DataType == _collection.GetType().FullName);         
            if(_dataBlob is null)
            {
                _collection = new List<T>();
                _dataBlob = DataBlob.Generate(_collection.GetType().FullName, _collection.Serialize());

                jsonDatatable.Root.Add(_dataBlob);
                _dataBlob = jsonDatatable.Root.FirstOrDefault(x => x.DataType == _collection.GetType().FullName);
            }

            foreach (var item in items)
            {
                item.EID = GetNextID<T>();
                item.GUID = GetNextGUID<T>();
                               
                _collection.Add(item);
            }

            _dataBlob.Data = _collection.Serialize();
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
                throw new NullReferenceException("No items to remove.");
            }

            List<T> _collection = null;
            var _dataBlob = jsonDatatable.Root.FirstOrDefault(x => x.DataType == typeof(List<T>).FullName);
            if (_dataBlob != null)
            {
                _collection = (List<T>)typeof(OpenDBF.JSON.Generic.ExtensionMethods).
                      GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static)
                      .MakeGenericMethod(typeof(List<T>)).Invoke(null, new object[] { _dataBlob.Data });

                foreach (var item in items)
                {
                    _collection.RemoveAll(x => x.GUID == item.GUID);
                }

                _dataBlob.Data = _collection.Serialize();
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
                var _json = File.ReadAllText(fullFilePath);
                if (!string.IsNullOrEmpty(_json))
                {
                    jsonDatatable = _json.Deserialize<JSONDatatable>();
                }
            }
            else
            {
                //Create random instance of the database
                jsonDatatable = JSONDatatable.Generate();
                if(jsonDatatable != null)
                {
                    var _json = jsonDatatable.Serialize();
                    if (!string.IsNullOrEmpty(_json))
                    {
                        if(!Directory.Exists(CurrentWorkspace))
                        {
                            Directory.CreateDirectory(CurrentWorkspace);
                        }

                        File.WriteAllText(fullFilePath, _json);
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
            if (items is null || !items.Any())
            {
                throw new NullReferenceException("No items to remove.");
            }

            var _collection = jsonDatatable.Root.FirstOrDefault(x => x is ICollection<T>);
            if (_collection != null)
            {
                foreach (var item in items)
                {
                    (_collection as ICollection<T>).RemoveAll(x => x.GUID == item.GUID);
                    (_collection as ICollection<T>).Add(item);
                }
            }
        }

        #endregion
    }
}
