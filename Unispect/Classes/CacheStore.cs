using System.Collections.Concurrent;

namespace Unispect
{
    public static class CacheStore
    {
        public static ConcurrentDictionary<ulong, string> FieldNameCache = new ConcurrentDictionary<ulong, string>();
        public static ConcurrentDictionary<ulong, string> ClassNameCache = new ConcurrentDictionary<ulong, string>();
        public static ConcurrentDictionary<ulong, string> ClassNamespaceCache = new ConcurrentDictionary<ulong, string>();

        public static void Clear()
        {
            FieldNameCache.Clear();
            ClassNameCache.Clear();
            ClassNamespaceCache.Clear();
        }
    }
}