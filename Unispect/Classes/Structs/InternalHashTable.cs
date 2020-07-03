namespace Unispect
{
    public struct InternalHashTable //MonoInternalHashTable
    {
        public ulong HashFunc;      // GHashFunc*
        public ulong KeyExtract;    // MonoInternalHashKeyExtractFunc*
        public ulong NextValue;     // MonoInternalHashNextValueFunc*
        public int Size;            // gint
        public int NumEntries;      // gint
        public ulong Table;         // gpointer*
    }
}