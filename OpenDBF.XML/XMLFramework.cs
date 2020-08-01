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
        /// Object used for cross-thread protection.
        /// </summary>
        private static readonly object lockObject = new object();
        /// <summary>
        /// Mutex to protect file access from multiple processes.
        /// </summary>
        protected static Mutex mutex = null;
        /// <summary>
        /// Current instance of the XML database.
        /// </summary>
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

            var identifiableObjects = GetCollectionItems(_collectionName).Select(x => x.Deserialize<T>());

            if (identifiableObjects is null || identifiableObjects.Count() == 0)
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
     
        #endregion

        #region Public methods

        /// <summary>
        /// Clears the current database handler. This causes the all internal properties to be null and the workspace to be deleted.
        /// </summary>
        public void Dispose()
        {
            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        //Clear XDocument
                        database = XMLDatatable.GetObject().Serialize();

                        //Clears internal data
                        CurrentWorkspace = null;
                        CurrentFileName = string.Empty;
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
        public bool DropTable<T>() where T : ICollectableObject
        {
            bool rc = false;

            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        if (database != null)
                        {
                            var _collectionName = GetCollectionName<T>();
                            if (CollectionExists(_collectionName))
                            {
                                GetCollection(_collectionName).Remove();
                                rc = !CollectionExists(_collectionName);
                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
          
            return rc;
        }       
       
        /// <summary>
        /// Gets a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<T> Get<T>(Func<T, bool> predicate = null) where T : ICollectableObject
        {
            IEnumerable<T> output = null;

            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        var _collectionName = GetCollectionName<T>();

                        var _dbCollection = GetCollectionItems(_collectionName).Select(x => x.Deserialize<T>());

                        //A collection or null should be returned.
                        output = predicate is null ? _dbCollection : _dbCollection.Where(predicate);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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

            return output;           
        }
        /// <summary>
        /// Saves a selection of items in its respective database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Commit()
        {
            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        string filePath = Path.Combine(CurrentWorkspace, CurrentFileNameWithExtension);
                        database.Save(filePath);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

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

                        //Creates the workspace folder if it does not exist.
                        if (!Directory.Exists(workspace))
                        {
                            Directory.CreateDirectory(workspace);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
        /// Imports a zipped database file.
        /// </summary>
        /// <param name="fileToImport"></param>
        /// <param name="exportPath"></param>
        public void Unpack(string fileToImport, string exportPath)
        {
            lock(lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        //This stores the path where the file should be unzipped to,
                        //including any subfolders that the file was originally in.
                        string fileUnzipFullPath = string.Empty;

                        //This is the full name of the destination file including
                        //the path
                        string fileUnzipFullName = string.Empty;

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
                        if(mutex != null)
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
        ///  Exports a group of database files to a single zipped file.
        /// </summary>
        /// <param name="pathToSave">A folder where the database file will be created.</param>
        /// <param name="filename">A name for the database file.</param>
        /// <param name="fileExtension">An extension for the file.</param>
        public void Pack(string pathToSave, string filename, string fileExtension = ".db")
        {
            lock(lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        //Checks if the path to save ends with a slash and if not, adds it.
                        if (!pathToSave.Last().Equals('\\'))
                        {
                            pathToSave += '\\';
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
                    }
                    catch
                    {
                        //The exception is caught after the mutex is release.
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
        public void Update<T>(IEnumerable<T> items) where T : ICollectableObject
        {
            Remove(items);
            Insert(items);
        }

        /// <summary>
        /// Inserts a collection of items in the active database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Insert<T>(IEnumerable<T> items) where T : ICollectableObject
        {
            Insert(items.ToArray());
        }
        /// <summary>
        /// Inserts one of more items in the active database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Insert<T>(params T[] items) where T : ICollectableObject
        {
            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

                        if (items is null || !items.Any())
                        {
                            throw new NullReferenceException("No items to insert.");
                        }

                        var _collectionName = GetCollectionName<T>();

                        if (!CollectionExists(_collectionName))
                        {
                            CreateCollection<T>();
                        }

                        var _collection = GetCollection(_collectionName);
                        foreach (var item in items)
                        {
                            item.EID = GetNextID<T>();
                            item.GUID = GetNextGUID<T>();

                            var serializedItem = item.SerializeToXElement();
                            _collection.Add(serializedItem);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
        /// Removes a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Remove<T>(IEnumerable<T> items) where T : ICollectableObject
        {
            Remove(items.ToArray());
        }
        /// <summary>
        /// Removes a selection of items that inherit from ICollectable object from their database collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        public void Remove<T>(params T[] items) where T : ICollectableObject
        {
            lock (lockObject)
            {
                try
                {
                    try
                    {
                        if(mutex != null)
                        {
                            mutex.WaitOne();
                        }

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
                                    if (_xmlItem.ToString() == _item.ToString())
                                    {
                                        _xmlItem.Remove();
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        if(mutex != null)
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
      
        #endregion
    }
}
