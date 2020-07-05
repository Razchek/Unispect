using System;
using System.Runtime.InteropServices;

namespace Unispect
{
    public abstract class MemoryProxy : IDisposable
    {
        public static MemoryProxy Instance { get; set; }

        protected MemoryProxy()
        {
            Instance = this;
        }

        public abstract ModuleProxy GetModule(string moduleName);

        public abstract bool AttachToProcess(string handle);

        public abstract byte[] Read(ulong address, int length);

        internal T Read<T>(ulong address, int length = 0)
        {
            // This can be sped up dramatically by using unsafe code and a memory pool.
            // I might do that at a later date.

            if (length == 0)
                length = Marshal.SizeOf<T>();

            var bytes = Read(address, length);

            if (bytes == null)
            {
                return default;
                //throw new AccessViolationException($"Unable to read memory at [0x{address:X16}]");
            }

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public virtual void Dispose()
        {
        }
    }
}