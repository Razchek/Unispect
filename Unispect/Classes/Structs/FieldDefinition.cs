using System;
using System.Runtime.InteropServices;

namespace Unispect
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct FieldDefinition
    {
        public ulong Type;
        public ulong NamePtr;
        public ulong Parent;
        public int Offset;
        private int pad0; // align(8)

        public string Name
        {
            get
            {
                if (CacheStore.FieldNameCache.ContainsKey(NamePtr + Type))
                    return CacheStore.FieldNameCache[NamePtr + Type];

                var name = GetName();
                CacheStore.FieldNameCache.AddOrUpdate(NamePtr, name, (arg1, s) => s);
                return name;
            }
        }

        private string GetName()
        {
            if (NamePtr < 0x10000000 || Offset > 0x2000)
                return "<ErrorReadingField>";

            var b = Memory.Read(NamePtr, 1024);

            if (b == null)
                return "<ErrorReadingField>";

            var code = b[0];
            if (code < 32 || code > 126) // Printable Ascii
            {
                var fieldType = GetFieldTypeString();
                if (fieldType == null)
                    return "<ErrorReadingField>";

                var dotIndex = fieldType.LastIndexOf('.') + 1;
                var subType = dotIndex >= 0
                    ? fieldType.Substring(dotIndex)
                    : fieldType;
                //return $"{GetFieldType().ToLower().Replace(".", "_")}_0x{Offset:X2}";
                return $"{subType.LowerChar().FormatFieldText()}" +
                       $"_0x{Offset:X2}";
            }

            var str = b.ToAsciiString();
            return str;
        }

        public override string ToString()
        {
            return Name;
        }

        public string GetFieldTypeString()
        {
            var monoType = Memory.Read<MonoType>(Type);

            var typeCode = (TypeEnum)(0xFF & (monoType.Attributes >> 16));
            switch (typeCode)
            {
                case TypeEnum.Class:
                case TypeEnum.SzArray:
                case TypeEnum.GenericInst:
                case TypeEnum.ValueType:
                    var typeDef = Memory.Read<TypeDefinition>(Memory.Read<ulong>(monoType.Data));
                    var name = typeDef.GetFullName();

                    if (typeCode == TypeEnum.GenericInst)
                    {
                        // If the field type is a generic instance, grab the generic parameters
                        var genericIndexOf = name.IndexOf('`');
                        if (genericIndexOf >= 0)
                        {
                            // GenericInstance 
                            var paramCountStr = name.Substring(genericIndexOf + 1);
                            if (int.TryParse(paramCountStr, out var pCount))
                            {
                                var genericParams = "";

                                var monoGenericClass = Memory.Read<MonoGenericClass>(monoType.Data);
                                var monoGenericInst =
                                    Memory.Read<MonoGenericInstance>(monoGenericClass.Context.ClassInstance);

                                for (uint i = 0; i < pCount; i++)
                                {
                                    var subType = MemoryProxy.Instance.Read<MonoType>(monoGenericInst.MonoTypes[i]);
                                    var subTypeCode = subType.GetTypeCode();

                                    // todo maybe make this method recursive to reduce both nesting and code clones
                                    // that will also allow for generic nests to be defined e.g. Root<Nested<int, Nested2<int, int>>, long>

                                    switch (subTypeCode)
                                    {
                                        case TypeEnum.Class:
                                        case TypeEnum.SzArray:
                                        case TypeEnum.GenericInst:
                                        case TypeEnum.ValueType:
                                            var subTypeDef =
                                                Memory.Read<TypeDefinition>(Memory.Read<ulong>(subType.Data));
                                            var subName = subTypeDef.Name;
                                            genericParams += $"{subName}, ";
                                            break;
                                        default:
                                            genericParams += $"{Enum.GetName(typeof(TypeEnum), subTypeCode)}, ";
                                            break;
                                    }

                                }

                                genericParams = genericParams.TrimEnd(',', ' ');

                                name = name.Replace($"`{paramCountStr}", $"<{genericParams}>");
                            }
                        }
                    }

                    if (typeCode == TypeEnum.SzArray)
                    {
                        name += "[]";
                    }

                    return name;

                default:
                    return Enum.GetName(typeof(TypeEnum), typeCode);
            }
        }

        public TypeDefinition? GetFieldType()
        {
            var monoType = Memory.Read<MonoType>(Type);

            var typeCode = (TypeEnum)(0xFF & (monoType.Attributes >> 16));
            switch (typeCode)
            {
                case TypeEnum.Class:
                case TypeEnum.SzArray:
                case TypeEnum.GenericInst: // todo check generic types
                case TypeEnum.ValueType:
                    var typeDef = Memory.Read<TypeDefinition>(Memory.Read<ulong>(monoType.Data));

                    return typeDef;
            }

            return null;
        }

        public static MemoryProxy Memory => MemoryProxy.Instance;
    }
}