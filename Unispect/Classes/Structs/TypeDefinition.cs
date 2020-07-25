using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Unispect
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct TypeDefinition // _MonoClassDef
    {
        #region Fields

        // For more information, see: https://github.com/Unity-Technologies/mono/blob/unity-2018.4-mbe/mono/metadata/class-internals.h
        [FieldOffset(0x0)] public ulong ElementClass; // MonoClass* element class for arrays and enum basetype for enums
        [FieldOffset(0x8)] public ulong CastClass; // MonoClass* used for subtype checks
        [FieldOffset(0x10)] public ulong SuperTypes; // MonoClass** for fast subtype checks
        [FieldOffset(0x18)] public ushort IDepth;
        [FieldOffset(0x1A)] public byte Rank; // array dimension

        //[FieldOffset(0x1B)] public byte undefined;
        [FieldOffset(0x1C)] public int InstanceSize; 

        // I won't implement a bitfield here, but I'll use the first byte for bit operations anyway
        [FieldOffset(0x20)] public byte BitByte;
        [FieldOffset(0x20)] public uint BitFields0; // Type storage bitfield
        [FieldOffset(0x24)] public uint BitFields1;
        [FieldOffset(0x28)] public uint BitFields2;
        [FieldOffset(0x2C)] public uint BitFields3;

        [FieldOffset(0x30)] public ulong Parent; // monoClass*
        [FieldOffset(0x38)] public ulong NestedIn; // monoClass*

        [FieldOffset(0x40)] public ulong Image; // monoImage*

        [FieldOffset(0x48)] public ulong NamePtr; // const char *name
        [FieldOffset(0x50)] public ulong NamespacePtr; // const char *name_space

        [FieldOffset(0x58)] public uint TypeToken;
        [FieldOffset(0x5C)] public int VTableSize;

        [FieldOffset(0x60)] public ushort InterfaceCount;
        [FieldOffset(0x64)] public uint InterfaceId;
        [FieldOffset(0x68)] public uint MaxInterfaceId;

        [FieldOffset(0x6c)] public ushort InterfaceOffsetsCount;

        [FieldOffset(0x70)] public ulong InterfacesPacked; // MonoClass**
        [FieldOffset(0x78)] public ulong InterfaceOffsetsPacked; //guint16*
        [FieldOffset(0x80)] public ulong InterfaceBitmap; //guint8*
        [FieldOffset(0x88)] public ulong Interfaces; // MonoClass**
        [FieldOffset(0x90)] public int Sizes; // union{ class_size, element_size, generic_param_token}

        [FieldOffset(0x98)] public ulong Fields; // MonoClassField*

        [FieldOffset(0xA0)] public ulong Methods; // MonoMethod**

        // Used as the type of the this argument and when passing the arg by value
        [FieldOffset(0xA8)] public MonoType ThisArg;
        [FieldOffset(0xB8)] public MonoType ByValArg;

        [FieldOffset(0xC8)] public ulong GcDesc; // MonoGCDescriptor  
        [FieldOffset(0xd0)] public ulong RuntimeInfo; // MonoClassRuntimeInfo  

        [FieldOffset(0xd8)] public ulong VTable; // MonoMethod**

        [FieldOffset(0xe0)] public ulong InfrequentData; //MonoPropertyBag  

        [FieldOffset(0xe8)] public ulong UnityUserData; // void*

        [FieldOffset(0xF0)] public int Flags;
        [FieldOffset(0xF4)] public int FirstMethodIdx;
        [FieldOffset(0xF8)] public int FirstFieldIdx;
        [FieldOffset(0xFC)] public int MethodCount;
        [FieldOffset(0x100)] public int FieldCount;

        [FieldOffset(0x108)] public ulong NextClassCache;

        #endregion

        public string Name
        {
            get
            {
                var cacheHash = NamePtr + NestedIn * (uint)ClassType;
                if (CacheStore.ClassNameCache.ContainsKey(cacheHash))
                    return CacheStore.ClassNameCache[cacheHash];

                var name = GetName();
                CacheStore.ClassNameCache.AddOrUpdate(cacheHash, name, (arg1, s) => s);
                return name;
            }
        }

        public string Namespace
        {
            get
            {
                if (CacheStore.ClassNamespaceCache.ContainsKey(NamePtr))
                    return CacheStore.ClassNamespaceCache[NamePtr];

                var name = GetNamespace();
                CacheStore.ClassNamespaceCache.AddOrUpdate(NamePtr, name, (arg1, s) => s);
                return name;
            }
        }



        public bool IsValueType => ((BitByte >> 2) & 1) == 0x1;
        public bool IsEnum => ((BitByte >> 3) & 1) == 0x1; // todo get enum values
        public bool IsInterface => ((BitByte >> 4) & 1) == 0x1; // blittable

        public UnknownPrefix ClassType
        {
            get
            {
                if (IsEnum) return UnknownPrefix.GEnum;
                if (IsValueType) return UnknownPrefix.GStruct;
                if (IsInterface) return UnknownPrefix.GInterface;
                return UnknownPrefix.GClass;
            }
        }

        public List<TypeDefinition> GetInterfaces()
        {
            var interfaces = new List<TypeDefinition>();
            if (Interfaces != 0 && InterfaceCount > 0)
            {
                for (uint i = 0; i < InterfaceCount; i++)
                {
                    var iface = Memory.Read<ulong>(Interfaces + i * 8);
                    var ifaceDef = Memory.Read<TypeDefinition>(iface);
                    interfaces.Add(ifaceDef);
                }
            }

            return interfaces;
        }

        public TypeDefinition? GetParent()
        {
            if (Parent == 0) return null;
            return Memory.Read<TypeDefinition>(Parent);
        }

        public List<TypeDefinition> GetSuperTypes()
        {
            //var parent = Memory.Read<TypeDefinition>(Parent);
            var superTypes = new List<TypeDefinition>();
            for (uint i = 0; i < IDepth; i++)
            {
                var super = Memory.Read<ulong>(SuperTypes + i * 8);
                var superDef = Memory.Read<TypeDefinition>(super);
                superTypes.Add(superDef);
            }

            return superTypes;
        }

        public string GetClassType()
        {
            if (IsEnum)
                return "Enum";

            if (IsValueType)
                return "Struct";

            if (IsInterface)
                return "Interface";

            return "Class";
        }

        public string GetFullName()
        {
            var nestedIn = NestedIn;
            var sb = new StringBuilder();

            var nestHierarchy = new List<string>();
            while (nestedIn != 0)
            {
                var nType = Memory.Read<TypeDefinition>(nestedIn);

                nestHierarchy.Add(nType.Name);

                nestedIn = nType.NestedIn;
            }

            nestHierarchy.Reverse();
            foreach (var nhName in nestHierarchy)
            {
                sb.Append(nhName + ".");
            }

            return $"{Namespace}.{(sb.Length > 0 ? sb.ToString().TrimEnd('.') + "." : "")}{Name}";
        }

        private string GetName()
        {
            if (NamePtr == 0)
                return "<NoName>";

            var b = Memory.Read(NamePtr, 1024);
            var code = b[0];
            if (code >= 0xE0)
            {
                var prefix = UnknownPrefix.GClass;

                if (IsEnum)
                    prefix = UnknownPrefix.GEnum;
                else if (IsValueType)
                    prefix = UnknownPrefix.GStruct;
                else if (IsInterface)
                    prefix = UnknownPrefix.GInterface;

                var unkTypeStr = b.ToUnknownClassString(prefix, TypeToken);
                return unkTypeStr;
                // Todo: add support for more general obfuscated names
                //Valid Names Match = @"^[a-zA-Z_<{$][a-zA-Z_0-9<>{}$.`-]*$"
            }

            var str = b.ToAsciiString();
            return str;
        }


        private string GetNamespace()
        {
            if (NamespacePtr == 0)
                return "<NoNamespace>";

            var b = Memory.Read(NamespacePtr, 1024);

            if (b[0] == 0)
                return "-";

            var str = b.ToAsciiString();

            return str;
        }

        public override string ToString()
        {
            return GetFullName();
        }

        public List<FieldDefinition> GetFields()
        {
            var fields = new List<FieldDefinition>();
            var fieldArrayBase = Fields;
            if (fieldArrayBase == 0)
            {
                if (Parent == 0) return null;
                goto checkParents;
            }

            for (var fIndex = 0u; fIndex < FieldCount; fIndex++)
            {
                // 0x20 == Marshal.SizeOf<FieldDefinition>();
                var field = Memory.Read<FieldDefinition>(fieldArrayBase + fIndex * 0x20);
                if (field.Type == 0)
                    break;

                fields.Add(field);
            }

        checkParents:
            if (Parent != 0)
            {
                // recursive
                var parent = Memory.Read<TypeDefinition>(Parent);
                var parentFields = parent.GetFields();
                if (parentFields != null)
                    fields.AddRange(parentFields);
            }

            var ret = fields.OrderBy(field => field.Offset).ToList();

            return ret;
        }

        public static MemoryProxy Memory => MemoryProxy.Instance;
    }
}