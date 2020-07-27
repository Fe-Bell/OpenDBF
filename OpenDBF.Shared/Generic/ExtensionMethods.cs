using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenDBF.Shared.Generic
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Deletes the contents of a directory.
        /// </summary>
        /// <param name="directory"></param>
        public static void Clean(this DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }

        /// <summary>
        /// Return true if the object is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNull<T>(this T value)
        {
            return value == null;
        }

        /// <summary>
        /// Enables "foreach" looping for ObservableCollections.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var cur in enumerable)
            {
                action(cur);
            }
        }

        /// <summary>
        /// Removes a selected list of items from a collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <param name="predicate"></param>
        public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            List<T> list = GetList(collection);

            if (!list.IsNull())
            {
                list.RemoveAll(new Predicate<T>(predicate));
            }
            else
            {
                List<T> itemsToDelete = collection.Where(predicate).ToList();

                foreach (var item in itemsToDelete)
                {
                    collection.Remove(item);
                }
            }
        }

        /// <summary>
        /// Returns a list from an ICollection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        private static List<T> GetList<T>(ICollection<T> collection)
        {
            return collection as List<T>;
        }

        /// <summary>
        /// Converts enumerable to ObservableCollection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> coll)
        {
            var c = new ObservableCollection<T>();
            foreach (var e in coll)
            {
                c.Add(e);
            }
            return c;
        }
    }
}
