using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Unispect
{
    [UnispectPlugin]
    public sealed class BasicMemory : MemoryProxy
    {
        #region DllImports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, ulong lpAddress, byte[] buffer, int size, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);
        #endregion

        #region Constants

        private const int ProcessVmAll = ProcessVmOperation | ProcessVmRead | ProcessVmWrite;
        private const int ProcessVmOperation = 0x0008;
        private const int ProcessVmRead = 0x0010;
        private const int ProcessVmWrite = 0x0020;

        #endregion

        public Process ManagedProcessHandle;
        public IntPtr NativeProcessHandle;

        public override ModuleProxy GetModule(string moduleName)
        {
            if (ManagedProcessHandle == null) throw new Exception("Not currently attached to a process.");

            ProcessModule resultModule = null;
            foreach (ProcessModule pm in ManagedProcessHandle.Modules.AsParallel())
            {
                if (pm.ModuleName.EndsWith(moduleName))
                {
                    resultModule = pm;
                    break;
                }
            }

            if (resultModule == null)
                return null;

            return new ModuleProxy(resultModule.ModuleName, (ulong)resultModule.BaseAddress.ToInt64(),
                resultModule.ModuleMemorySize);
        }

        public override bool AttachToProcess(string handle)
        {
            var procName = (string)handle;
            var procList = Process.GetProcessesByName(procName);

            if (procList.Length == 0)
                throw new Exception("Process not found.");

            ManagedProcessHandle = procList[0];
            NativeProcessHandle = OpenProcess(ProcessVmAll, false, ManagedProcessHandle.Id);

            return true;
        }

        public override byte[] Read(ulong address, int length)
        {
            return ReadMemory(address, length);
        }

        public byte[] ReadMemory(ulong address, int length)
        {
            var bytesRead = 0;
            var buffer = new byte[length];
            var success = ReadProcessMemory(NativeProcessHandle, address, buffer, length, ref bytesRead);

            if (success && bytesRead > 0)
            {
                return buffer;
            }

            return null;
        }

        public override void Dispose()
        {
            // Cleanup
            CloseHandle(NativeProcessHandle);
        }
    }
}