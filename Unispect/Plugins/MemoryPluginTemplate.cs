using System;
using Unispect;

namespace MyMemoryNameSpace
{
    [UnispectPlugin]
    public sealed class MyMemoryPlugin : MemoryProxy
    {
        public MyMemoryPlugin()
        {
            //  Perform any preliminary initialization you need here.

            // Display messages in Unispect's log.
            Log.Add("Looking good, captain.");
        }

        public override ModuleProxy GetModule(string moduleName)
        {
            // This method is only used to get the base address and size in memory of 'moduleName' (mono-2.0-bdwgc.dll by default)
            // The inspector target is obtained by using that module.
            throw new NotImplementedException();
            var baseAddress = 0ul;
            var size = 0x00767000;
            return new ModuleProxy(moduleName, baseAddress, size);
        }

        public override bool AttachToProcess(string handle)
        {
            // Attach to the process so that the two Read functions are able to interface with the process.
            // The argument: handle (string) will be the text from Unispect's "Process Handle" text box.
            throw new NotImplementedException();
        }

        public override byte[] Read(ulong address, int length)
        {
            // This handles reading bytes into a byte array.

            // If your implementation provides a byte pointer (byte*) then consider using
            // Marshal.Copy(new IntPtr(bytePtr), buffer, 0, size) 
            // You should also be able to use IntPtr instead of byte* in your import definition-
            // -so you could remove the IntPtr constructor.
            // Marshal.Copy(bytePtr, buffer, 0, size) 
            throw new NotImplementedException();
        } 

        public override void Dispose()
        {
            // Cleanup. Close native handles, free unmanaged memory, or anything else the garbage collector won't see.
        }
    }

}