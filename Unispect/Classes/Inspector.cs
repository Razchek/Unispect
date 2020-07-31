using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unispect
{
    // Todo add support for il2cpp ?
    [Serializable]
    public sealed class Inspector : Progress<float>, IDisposable
    {
        private MemoryProxy _memory;
        private float _progressTotal;
        private float ProgressTotal
        {
            get => _progressTotal;
            set
            {
                _progressTotal = value;
                OnReport(_progressTotal);
            }
        }

        private ConcurrentDictionary<ulong, TypeDefinition> _typeDefinitions
            = new ConcurrentDictionary<ulong, TypeDefinition>();

        public List<TypeDefWrapper> TypeDefinitions { get; private set; } = new List<TypeDefWrapper>();

        // If you add any progress lengths, you should increase this.
        // Every progress task represents 1 length
        private const int TotalProgressLength = 9;

        protected override void OnReport(float value)
        {
            value /= TotalProgressLength;
            base.OnReport(value);
        }

        public void DumpTypes(string fileName, Type memoryProxyType,
            bool verbose = true,
            string processHandle = "SomeGame",
            //string monoModuleName = "mono-2.0-bdwgc.dll",
            string moduleToDump = "Assembly-CSharp")
        {
            Log.Add($"Initializing memory proxy of type '{memoryProxyType.Name}'");
            _memory = (MemoryProxy)Activator.CreateInstance(memoryProxyType);

            ProgressTotal += 0.16f;

            Log.Add($"Attaching to process '{processHandle}'");
            var success = _memory.AttachToProcess(processHandle);

            if (!success)
                throw new Exception("Could not attach to the remote process.");

            ProgressTotal += 0.16f;

            //Log.Add($"Obtaining {monoModuleName} module details");
            var monoModule = GetMonoModule(out var monoModuleName);
            if (monoModule == null)
            {
                throw new NotSupportedException();
            }

            Log.Add($"Module {monoModule.Name} loaded. " +
                    $"(BaseAddress: 0x{monoModule.BaseAddress:X16})");

            ProgressTotal += 0.16f;

            Log.Add($"Copying {monoModuleName} module to local memory {(monoModule.Size / (float)0x100000):###,###.00}MB");
            var monoDump = _memory.Read(monoModule.BaseAddress, monoModule.Size);

            ProgressTotal += 0.16f;

            Log.Add($"Traversing PE of {monoModuleName}");
            var rdfa = GetRootDomainFunctionAddress(monoDump, monoModule);

            ProgressTotal += 0.16f;

            Log.Add($"Getting MonoImage address for {moduleToDump}");
            var monoImage = GetAssemblyImageAddress(rdfa, moduleToDump); // _MonoImage of moduleToDump (Assembly-CSharp)

            ProgressTotal += 0.16f;

            _typeDefinitions = GetRemoteTypeDefinitions(monoImage);

            Log.Add("Propogating types and fields");
            PropogateTypes();

            // If this is true, then the user does not want to save to file
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                DumpToFile(fileName, verbose, false);
                ProgressTotal += 0.15f;
                SaveTypeDefDb(processHandle, moduleToDump);
            }

            OnReport(TotalProgressLength); // Set to 100%

            Log.Add("Operation completed successfully.");
        }

        public void SaveTypeDefDb(string processHandle, string moduleToDump)
        {
            Log.Add("Saving Type Definition database");
            //Log.Add("Compressing Type Definition database");

            if (!System.IO.Directory.Exists("TypeDbs"))
                System.IO.Directory.CreateDirectory("TypeDbs");

            // Todo if we plan on storing multiple, perhaps make it a cyclic storage system.
            //var fileName = $"{processHandle} {moduleToDump} ({DateTime.Now.ToFileTime():X8}).gz";
            //var fileName = $"{processHandle} {moduleToDump}.gz";
            var fileName = $"{processHandle} {moduleToDump}.utd";
            //Serializer.SaveCompressed($"TypeDbs\\{fileName.SanitizeFileName().ToLower()}", TypeDefinitions);
            Serializer.Save($"TypeDbs\\{fileName.SanitizeFileName().ToLower()}", TypeDefinitions);
        }

        private ModuleProxy GetMonoModule(out string moduleName)
        {
            //Log.Add("Looking for the mono module (mono, mono-2.0-bdwgc)");
            Log.Add("Looking for the mono module (mono-2.0-bdwgc)");
            var module = _memory.GetModule("mono-2.0-bdwgc.dll");
            if (module != null)
            {
                moduleName = "Found mono-2.0-bdwgc.dll";
                return module;
            }

            // Currently unsupported.
            // todo: return to this when dynamic structures are implemented and consider adding support
            //module = _memory.GetModule("mono.dll");
            //if (module != null)
            //{
            //    moduleName = "mono.dll";
            //    return module;
            //}

            moduleName = "";
            return null;
        }

        private void PropogateTypes()
        {
            var typeDefWrappers = new List<TypeDefWrapper>();
            var progressIncrement = 1f / _typeDefinitions.Count * 3f;
            foreach (var t in _typeDefinitions.AsParallel())
            {
                ProgressTotal += progressIncrement;

                var typeDef = t.Value;
                typeDefWrappers.Add(new TypeDefWrapper(typeDef));
            }

            Log.Add("Sorting type definitions by path");

            TypeDefinitions = new List<TypeDefWrapper>(typeDefWrappers.OrderBy(wrapper => wrapper.FullName));
        }

        public void DumpToFile(string fileName, bool verbose = true, List<TypeDefWrapper> tdlToDump = null)
        {
            DumpToFile(fileName, verbose, true, tdlToDump);
        }

        private void DumpToFile(string fileName, bool verbose, bool adjustProgressIncr, List<TypeDefWrapper> tdlToDump = null)
        {
            // ****************** Formatting below
            Log.Add("Formatting dump");
            var sb = new StringBuilder();

            if (tdlToDump == null)
                tdlToDump = TypeDefinitions;

            var progressIncrement = 1f / tdlToDump.Count * (adjustProgressIncr
                                        ? TotalProgressLength
                                        : 2);

            sb.AppendLine($"Generated by Unispect v{Utilities.CurrentVersion} - by Razchek {Utilities.GithubLink}");
            sb.AppendLine();
            sb.AppendLine("S = Static");
            sb.AppendLine("C = Constant");
            sb.AppendLine();

            foreach (var typeDef in tdlToDump)
            {
                // Progress 1 
                ProgressTotal += progressIncrement;

                if (verbose)
                    sb.Append($"[{typeDef.ClassType}] ");
                sb.Append(typeDef.FullName);
                if (verbose)
                {
                    //sb.AppendLine($" [{typeDef.GetClassType()}]");
                    var parent = typeDef.Parent;
                    if (parent != null)
                    {
                        sb.Append($" : {parent.Name}");
                        var interfaceList = typeDef.Interfaces;
                        if (interfaceList.Count > 0)
                        {
                            foreach (var iface in interfaceList)
                            {
                                sb.Append($", {iface.Name}");
                            }
                        }
                    }
                }

                sb.AppendLine();

                var fields = typeDef.Fields;
                if (fields == null)
                    continue;

                foreach (var field in fields)
                {
                    if (field.Offset > 0x2000)
                        continue;

                    var fieldName = field.Name;
                    var fieldType = field.FieldType;
                    sb.AppendLine(field.HasValue
                        ? $"    [{field.Offset:X2}][{field.ConstantValueTypeShort}] {fieldName} : {fieldType}"
                        : $"    [{field.Offset:X2}] {fieldName} : {fieldType}");
                }
            }

            System.IO.File.WriteAllText(fileName, sb.ToString());

            Log.Add($"Your definitions and offsets dump was saved to: {fileName}");
        }

        private ConcurrentDictionary<ulong, TypeDefinition> GetRemoteTypeDefinitions(ulong monoImageAddress)
        {
            var classCache = _memory.Read<InternalHashTable>(monoImageAddress + Offsets.ImageClassCache);
            var typeDefs = new Dictionary<ulong, TypeDefinition>();

            Log.Add($"Processing {classCache.Size} classes. This may take some time.");

            // Multiplying this by two will make it use two progress lengths.
            // Since it does a lot of the hard work, I think it fits nicely.
            var progressIncrement = 1f / classCache.Size * 2;


            for (var i = 0u; i < classCache.Size; i++)
            {
                // Progress 0
                ProgressTotal += progressIncrement;

                for (var d = _memory.Read<ulong>(classCache.Table + i * 8);
                    d != 0;
                    d = _memory.Read<ulong>(d + Offsets.ClassNextClassCache))
                {
                    var typeDef = _memory.Read<TypeDefinition>(d);
                    typeDefs.Add(d, typeDef);
                }
            }

            return new ConcurrentDictionary<ulong, TypeDefinition>(typeDefs);
        }

        private ulong GetAssemblyImageAddress(ulong rootDomainFunctionAddress, string name = "Assembly-CSharp")
        {
            var relativeOffset = _memory.Read<uint>(rootDomainFunctionAddress + 3);      // mov rax, 0x004671B9
            var domainAddress = relativeOffset + rootDomainFunctionAddress + 7;     // rdfa + 0x4671C0 // RootDomain (Unity Root Domain)

            var domain = _memory.Read<ulong>(domainAddress);

            var assemblyArrayAddress = _memory.Read<ulong>(domain + Offsets.DomainDomainAssemblies);
            for (var assemblyAddress = assemblyArrayAddress;
                assemblyAddress != 0;
                assemblyAddress = _memory.Read<ulong>(assemblyAddress + 0x8))
            {
                var assembly = _memory.Read<ulong>(assemblyAddress);
                var assemblyNameAddress = _memory.Read<ulong>(assembly + 0x10);
                var assemblyName = _memory.Read(assemblyNameAddress, 1024).ToAsciiString();
                if (assemblyName != name)
                    continue;

                return _memory.Read<ulong>(assembly + Offsets.AssemblyImage);
            }

            throw new InvalidOperationException($"Unable to find assembly '{name}'");
        }

        private static ulong GetRootDomainFunctionAddress(byte[] moduleDump, ModuleProxy monoModuleInfo)
        {
            // Traverse the PE header to get mono_get_root_domain
            var startIndex = moduleDump.ToInt32(Offsets.ImageDosHeaderELfanew);

            var exportDirectoryIndex = startIndex + Offsets.ImageNtHeadersExportDirectoryAddress;
            var exportDirectory = moduleDump.ToInt32(exportDirectoryIndex);

            var numberOfFunctions = moduleDump.ToInt32(exportDirectory + Offsets.ImageExportDirectoryNumberOfFunctions);
            var functionAddressArrayIndex = moduleDump.ToInt32(exportDirectory + Offsets.ImageExportDirectoryAddressOfFunctions);
            var functionNameArrayIndex = moduleDump.ToInt32(exportDirectory + Offsets.ImageExportDirectoryAddressOfNames);

            Log.Add($"e_lfanew: 0x{startIndex:X4}, Export Directory Entry: 0x{exportDirectory:X4}");
            Log.Add("Searching exports for 'mono_get_root_domain'");
            var rootDomainFunctionAddress = 0ul;

            Parallel.ForEach(Utilities.Step(0, numberOfFunctions * 4, 4), (functionIndex, state) =>
            {
                var functionNameIndex = moduleDump.ToInt32(functionNameArrayIndex + functionIndex);
                var functionName = moduleDump.ToAsciiString(functionNameIndex);

                if (functionName != "mono_get_root_domain")
                    return;

                //var realIndex = functionIndex / 4;
                var rva = moduleDump.ToInt32(functionAddressArrayIndex + functionIndex);
                rootDomainFunctionAddress = monoModuleInfo.BaseAddress + (ulong)rva;

                state.Stop();
            }
            );

            if (rootDomainFunctionAddress == 0)
            {
                throw new InvalidOperationException("Failed to find mono_get_root_domain function.");
            }
            Log.Add($"Function 'mono_get_root_domain' found. (Address: {rootDomainFunctionAddress:X16})");
            return rootDomainFunctionAddress;
        }

        public void Dispose()
        {
            _memory?.Dispose();
        }
    }
}