// -ReSharper disable UnusedMember.Global
// todo: move all structs into their own class files maybe
using System;
using System.IO;
using Newtonsoft.Json;

namespace Unispect
{
    // Structures and offsets created manually with references to the module memory, IDA disassembly with pdb and:
    // https://github.com/Unity-Technologies/mono/blob/unity-2018.4-mbe
    // Updated for v2022

    internal static class Offsets
    {
        public static int ImageDosHeaderELfanew = 0x3c; // PtrToPEHeader -> e_lfanew
        //public const int ImageNtHeadersSignature = 0x0;
        //public const int ImageNtHeadersMachine = 0x4;
        public static int ImageNtHeadersExportDirectoryAddress = 0x88;
        public static int ImageExportDirectoryNumberOfFunctions = 0x14;
        public static int ImageExportDirectoryAddressOfFunctions = 0x1c;
        public static int ImageExportDirectoryAddressOfNames = 0x20;

        // These could be a part of structs, but for convenience I'll leave them here.
        public static int ImageClassCache = 0x4D0;       // MonoImage.ClassCache
        public static int DomainDomainAssemblies = 0xA0; // MonoDomain.DomainAssemblies
        public static int AssemblyImage = 0x60;          // MonoAssembly.MonoImage
        public static int ClassNextClassCache = 0x108;   // MonoClassDef.NextClassCache

        private static int HexToInt(string hex) => Convert.ToInt32(hex, 16);

        // Will return null if the load fails
        public static string Load(string filename, bool testLoad = false)
        {
            try
            {
                string json = File.ReadAllText(filename);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);

                ImageDosHeaderELfanew = HexToInt(jsonObj.ImageDosHeaderELfanew.Value);
                ImageNtHeadersExportDirectoryAddress = HexToInt(jsonObj.ImageNtHeadersExportDirectoryAddress.Value);
                ImageExportDirectoryNumberOfFunctions = HexToInt(jsonObj.ImageExportDirectoryNumberOfFunctions.Value);
                ImageExportDirectoryAddressOfFunctions = HexToInt(jsonObj.ImageExportDirectoryAddressOfFunctions.Value);
                ImageExportDirectoryAddressOfNames = HexToInt(jsonObj.ImageExportDirectoryAddressOfNames.Value);
                ImageClassCache = HexToInt(jsonObj.ImageClassCache.Value);
                DomainDomainAssemblies = HexToInt(jsonObj.DomainDomainAssemblies.Value);
                AssemblyImage = HexToInt(jsonObj.AssemblyImage.Value);
                ClassNextClassCache = HexToInt(jsonObj.ClassNextClassCache.Value);
                string targetTitle = jsonObj.TargetTitle;

                if (!testLoad) Log.Add($"Mono offsets loaded successfully from {filename}");
                return targetTitle;
            }
            catch (Exception ex)
            {
                Log.Add($"Error loading Mono offsets from {filename}: {ex.Message}");
                if (!testLoad)
                {
                    Log.Add("Loading default values for Unity v2022");
                    LoadDefaultValues();
                }
                return null;
            }
        }

        public static void LoadDefaultValues()
        {
            ImageDosHeaderELfanew = 0x3c;
            ImageNtHeadersExportDirectoryAddress = 0x88;
            ImageExportDirectoryNumberOfFunctions = 0x14;
            ImageExportDirectoryAddressOfFunctions = 0x1c;
            ImageExportDirectoryAddressOfNames = 0x20;
            ImageClassCache = 0x4d0;
            DomainDomainAssemblies = 0xa0;
            AssemblyImage = 0x60;
            ClassNextClassCache = 0x108;
        }
    }
}