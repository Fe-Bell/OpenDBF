using System;
using System.Collections.Generic;

namespace OpenDBF.Shared.Generic
{
    public static class ExtensionMethods
    {              
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
    }
}
