// -ReSharper disable UnusedMember.Global
// todo: move all structs into their own class files maybe
namespace Unispect
{
    // Structures and offsets created manually with references to the module memory, IDA disassembly with pdb and:
    // https://github.com/Unity-Technologies/mono/blob/unity-2018.4-mbe

    internal static class Offsets
    {
        public const int ImageDosHeaderELfanew = 0x3c; // PtrToPEHeader -> e_lfanew
        //public const int ImageNtHeadersSignature = 0x0;
        //public const int ImageNtHeadersMachine = 0x4;
        public const int ImageNtHeadersExportDirectoryAddress = 0x88;
        public const int ImageExportDirectoryNumberOfFunctions = 0x14;
        public const int ImageExportDirectoryAddressOfFunctions = 0x1c;
        public const int ImageExportDirectoryAddressOfNames = 0x20;

        // These could be a part of structs, but for convenience I'll leave them here.
        public const int ImageClassCache = 0x4C0;       // MonoImage.ClassCache
        public const int DomainDomainAssemblies = 0xC8; // MonoDomain.DomainAssemblies
        public const int AssemblyImage = 0x60;          // MonoAssembly.MonoImage
        public const int ClassNextClassCache = 0x108;   // MonoClassDef.NextClassCache
    }
}