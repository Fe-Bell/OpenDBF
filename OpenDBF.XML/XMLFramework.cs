using OpenDBF.XML.Generic;
using OpenDBF.Shared.Interface;
using OpenDBF.XML.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using OpenDBF.Shared.Generic;

namespace OpenDBF.XML
{
    /// <summary>
    /// Provides resources for manipulating XML-based databases.
    /// </summary>
    public class XMLFramework : IDatabaseFramework
    {
        #region Private fields

        const string COLLECTION_STR = "Collection";

        /// <summary>
        /// Watches the database's current folder.
        /// </summary>
        private FileSystemWatcher fileSystemWatcher = null;
        /// <summary>
        /// Stores the name of the last modifiled file in the database folder.
        /// </summary>
        private string lastFileChanged = string.Empty;
        /// <summary>
        /// Object used for cross-thread protection.
        /// </summary>
        private static readonly object lockObject = new object();
        /// <summary>
        /// Mutex to protect file access from multiple processes.
        /// </summary>
        protected static Mutex mutex = null;

        private XDocument database = null;

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the current filename of the database with extension.
        /// </summary>
        public string CurrentFileNameWithExtension => CurrentFileName + ".xml";
        /// <summary>
        /// Returns the current filename of the database.
        /// </summary>
        public string CurrentFileName { get; protected set; }
        /// <summary>
        /// Returns the current workspace of the database.
        /// </summary>
        public string CurrentWorkspace { get; protected set; }

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

        /// <summary>
        /// Creates a new instance of DatabaseHandler.
        /// </summary>
        public XMLFramework()
        {
            database = XMLDatatable.GetObject().Serialize();

            //Xamarin does not currently support mutexes for some mobile platforms.
            //When a new Mutex instance is created by Xamarin.Forms, a NotSupportedException/NotImplementedException is thrown.
            //The following code handles this exception. The mutex will be ignored because it is null.
            try
            {
                mutex = new Mutex(false, "OpenDBF.XMLMutex");
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
        /// Handles file changed events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!lastFileChanged.Equals(e.Name))
            {
                string fileName = e.Name;

                if (fileName.Contains(".xml"))
                {
                    fileName = fileName.Replace(".xml", "");

                    if (fileName.Contains("Database"))
                    {
                        OnDatabaseChanged?.Invoke(this, new OnDatabaseChangedEventArgs(fileName, DateTime.Now));
                    }
                }

                lastFileChanged = e.Name;
            }
            else
            {
                lastFileChanged = string.Empty;
            }

            Console.WriteLine("Changed " + e.FullPath);
        }
        /// <summary>
        /// Handles file created events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Created " + e.FullPath);
        }
        /// <summary>
        /// Handles file deleted events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Deleted " + e.FullPath);
        }
        /// <summary>
        /// Handles file renamed events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("Renamed " + e.FullPath);
        }
        /// <summary>
        /// Gets the database path of a database.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetDatabasePath(Type type)
        {
            FieldInfo dbPathField = GetType().GetField("paths", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!dbPathField.IsNull())
            {
                var paths = dbPathField.GetValue(this);
                return (paths as IEnumerable<string>).FirstOrDefault(path => path.Contains(type.Name));
            }
            else
            {
                throw new Exception("Could not find any paths.");
            }
        }
        /// <summary>
        /// Monitors a specified directory. This creates a filewatcher.
        /// </summary>
        /// <param name="path"></param>
        private void StartMonitoringDirectory(string path)
        {
            //Xamarin does not currently support a unified FileSystemWatcher for mobile platforms.
            //When a new FileSystemWatcher instance is created by Xamarin.Forms, a NotSupportedException/NotImplementedException is thrown.
            //The following code handles this exception. The FileSystemWatcher will be ignored because it is null.
            try
            {
                fileSystemWatcher = new FileSystemWatcher
                {
                    Path = path
                };
                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                fileSystemWatcher.Created += FileSystemWatcher_Created;
                fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
                fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;

                fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
                fileSystemWatcher.EnableRaisingEvents = true;
            }
            catch(Exception ex)
            {
                if(ex is NotSupportedException || ex is NotImplementedException)
                {
                    Console.WriteLine("FileWatcher disabled.");
                }
            }
        }
        /// <summary>
        /// Stops monitoring a folder. This disposes the Filewatcher and its events.
        /// </summary>
        private void StopMonitoringDirectory()
        {
            if (!fileSystemWatcher.IsNull())
            {
                fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
                fileSystemWatcher.Created -= FileSystemWatcher_Created;
                fileSystemWatcher.Renamed -= FileSystemWatcher_Renamed;
                fileSystemWatcher.Deleted -= FileSystemWatcher_Deleted;

                fileSystemWatcher.Dispose();
                fileSystemWatcher = null;
            }
        }
        /// <summary>
        /// Gets a database that inherits from IDatabase
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T GetDatabase<T>() where T : IDatabase
        {
            string path = GetDatabasePath(typeof(T));

            if (!string.IsNullOrEmpty(path))
            {
                T database = default(T);

                lock(lockObject)
                {
                    try
                    {
                        try
                        {
                            if(!mutex.IsNull())
                            {
                                mutex.WaitOne();
                            }
                            
                            database = path.Deserialize<T>();
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            if(!mutex.IsNull())
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

                    return database;
                }
            }
            else
            {
                return default(T);
            }
        }
        /// <summary>
        /// Gets a selection of items that inherit from ICollectable object from their database collection.
        /// If propertyName and propertyValue are null, then returns all items of the selected type T in the database.
        /// If propertyName and propertyValue are NOT null, then all items matching the query (propertyNames that have propertyValues as their value).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        private ICollection<T> Get<T>(string propertyName = null, object propertyValue = null, bool dummyParameter = false) where T : ICollectableObject
        {
            if (string.IsNullOrEmpty(propertyName) && !propertyValue.IsNull())
            {
                throw new Exception("If propertyName is null, then propertyValue must also be null.");
            }
          
            bool getAllItems = string.IsNullOrEmpty(propertyName) && propertyValue.IsNull();

            var _collectionName = GetCollectionName<T>();

            var _dbCollection = GetCollectionItems(_collectionName).Select(x => x.Deserialize<T>());

            ICollection<T> items = null;

            if (getAllItems)
            {
                items = _dbCollection.ToArray();
            }
            else
            {
                List<T> col = new List<T>();
                foreach (var item in _dbCollection)
                {
                    var prop = item.GetType().GetProperty(propertyName);
                    if (!prop.IsNull())
                    {
                        var propValue = prop.GetValue(item);
                        if (!propValue.IsNull())
                        {
                            if (prop.GetValue(item).ToString() == (propertyValue ?? "").ToString())
                            {
                                col.Add(item);
                            }
                        }
                    }
                }
                items = col.Any() ? col : null;
            }

            //A collection or null should be returned.
            return items;
        }
        /// <summary>
        /// Creates a new collection node inside the current database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void CreateCollection<T>() where T : ICollectableObject
        {
            //Creates a new collection inside the current XML      
            var _collectionName = GetCollectionName<T>();

            if(database is null)
            {
                throw new NullReferenceException("The current database is null or not initialized.");
            }

            const string XMLSCHEMAINSTANCE_ATB = "http://www.w3.org/2001/XMLSchema-instance";
            const string XMLSCHEMA_ATB = "http://www.w3.org/2001/XMLSchema";

            var element = new XElement(_collectionName);
            element.Add(new XAttribute(XNamespace.Xmlns + "xsi", XMLSCHEMAINSTANCE_ATB));
            element.Add(new XAttribute(XNamespace.Xmlns + "xsd", XMLSCHEMA_ATB));

            database.Root.Add(element);          
        }
        /// <summary>
        /// Checks if a collection exists in the database.
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        private bool CollectionExists(string collectionName)
        {
            if(database is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(collectionName))
            {
                throw new NullReferenceException("CollectionName cannot be empty.");
            }

            return database.Root.Elements().Any(x => x.Name == collectionName);
        }
        /// <summary>
        /// Returns a name for an internal XML collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private string GetCollectionName<T>()
        {
            return string.Format("{0}{1}", typeof(T).Name, COLLECTION_STR);
        }
        /// <summary>
        /// Returns a collection of xml elements.
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        private IEnumerable<XElement> GetCollectionItems(string collectionName)
        {
            return GetCollection(collectionName).Elements();
        }
        /// <summary>
        /// Returns a collection XML element.
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        private XElement GetCollection(string collectionName)
        {
            return database.Root.Elements().FirstOrDefault(x => x.Name == collectionName);
        }
        /// <summary>
        /// Gets a the next available ID in a collection of collectables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startID"></param>
        /// <returns></returns>
        private uint GetNextID<T>(uint startID = 0) where T : ICollectableObject
        {
            if (database is null)
            {
                throw new NullReferenceException("Could not load database file.");
            }

            var collectionName = GetCollectionName<T>();

            if (CollectionExists(collectionName))
            {
                //Gets all elements in the collection
                var _collection = GetCollectionItems(collectionName).Select(x => x.Deserialize<T>());

                if (_collection != null && _collection.Any())
                {
                    //Gets a list of used IDs
                    var idList = _collection.Select(x => x.EID);
                    if (idList != null && idList.Any())
                    {
                        //Gets a range of numbers
                        var range = NumericUtils.Range(startID, (uint)idList.Count() + 1);
                        return range.Except(idList).First();
                    }
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

            var _collectionName = GetCollectionName<T>();

            //var _collection = GetCollection(_collectionName);
            var identifiableObjects = GetCollectionItems(_collectionName).Select(x => x.Deserialize<T>());

            if (identifiableObjects.IsNull() || !identifiableObjects.Any())
            {
                return newGUID;
            }
            else
            {
                if (identifiableObjects.Any(collectableObject => collectableObject.GUID == newGUID))
                {
                    return GetNextGUID<T>();
                }
                else
                {
                    return newGUID;
                }
            }
        }
        /// <summary>
        /// Returns a properly enumerated collection of ICollectableObjects.
        /// </summary>
        /// <param name="collectableObjects"></param>
        /// <returns></returns>
        private ICollection<T> EnumerateCollection<T>(ICollection<T> collectableObjects, uint startIndex = 0) where T : ICollectableObject
        {
            collectableObjects.ForEach(iCollectableObject =>
            {
                iCollectableObject.EID = startIndex;
                startIndex++;
            });

            return collectableObjects;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Clears the current database handler. This causes the all internal properties to be null and the workspace to be deleted.
        /// </summary>
        public void Dispose()
        {
            StopMonitoringDirectory();
                      
            //Clear XDocument
            database = XMLDatatable.GetObject().Serialize();

            //Clears internal data
            CurrentWorkspace = null;
            lastFileChanged = null;
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
        /// Deletes a database file from the computer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public bool DropTable<T>() where T : ICollectableObject
        {
            if(database != null)
            {
                var _collectionName = GetCollectionName<T>();
                if (CollectionExists(_collectionName))
                {
                    GetCollection(_collectionName).Remove();
                    return !CollectionExists(_collectionName);
                }
            }

            return false;
        }       
       
        /// <summary>
        /// Gets a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<T> Get<T>(Func<T, bool> predicate = null) where T : ICollectableObject
        {
            var _collectionName = GetCollectionName<T>();

            var _dbCollection = GetCollectionItems(_collectionName).Select(x => x.Deserialize<T>());

            //A collection or null should be returned.
            return predicate is null ? _dbCollection : _dbCollection.Where(predicate);
        }
        /// <summary>
        /// Saves a selection of items in its respective database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Commit()
        {
            string filePath = Path.Combine(CurrentWorkspace, CurrentFileNameWithExtension);

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

                        database.Save(filePath);

                        //Will fire the database changed event when the FileSystemWatcher is unavailable.
                        //This should happen only when using Xamarin.Forms as there is no native support for that class.
                        if(fileSystemWatcher.IsNull())
                        {
                            OnDatabaseChanged?.Invoke(this, new OnDatabaseChangedEventArgs(CurrentFileName, DateTime.Now));
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
                database = XDocument.Load(fullFilePath);
            }
            else
            {
                //Create random instance of the database
                database = XMLDatatable.GetObject().Serialize();
            }

            //Stop monitoring the current workspace
            StopMonitoringDirectory();

            //Creates the workspace folder if it does not exist.
            if (!Directory.Exists(workspace))
            {
                Directory.CreateDirectory(workspace);
            }

            //Start monitoring the current workspace
            StartMonitoringDirectory(workspace);
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

            lock(lockObject)
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

            lock(lockObject)
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
                        if(!Directory.Exists(Path.Combine(tempFolder)))
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
                        if(File.Exists(fullFilePath))
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
        /// Updates a selection of items in its respective database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Update<T>(IEnumerable<T> items) where T : ICollectableObject, new()
        {
            Remove(items);
            Insert(items);
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
            if(items is null || !items.Any())
            {
                throw new NullReferenceException("No items to insert.");
            }

            var _collectionName = GetCollectionName<T>();

            if (!CollectionExists(_collectionName))
            {
                CreateCollection<T>();
            }

            var _collection = GetCollection(_collectionName);
            foreach(var item in items)
            {
                item.EID = GetNextID<T>();
                item.GUID = GetNextGUID<T>();

                var serializedItem = item.SerializeToXElement();
                _collection.Add(serializedItem);
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
                throw new NullReferenceException("No items to remove.");
            }

            var _collectionName = GetCollectionName<T>();

            if (CollectionExists(_collectionName))
            {
                var _itemsToRemove = items.Select(x => x.SerializeToXElement<T>());
                var _collection = GetCollectionItems(_collectionName);

                foreach (var _xmlItem in _collection)
                {
                    foreach (var _item in _itemsToRemove)
                    {
                        if(_xmlItem.ToString() == _item.ToString())
                        {
                            _xmlItem.Remove();
                        }
                    }
                }
            }
        }
      
        #endregion
    }
}
