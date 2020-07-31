using System;
using System.Linq;

namespace Unispect
{
    [Serializable]
    public class FieldDefWrapper
    {
        public FieldDefinition InnerDefinition;

        public FieldDefWrapper(FieldDefinition fieldDef/*, bool getFieldTypeDef = true*/)
        {
            InnerDefinition = fieldDef;

            Name = InnerDefinition.Name;

            FieldType = InnerDefinition.GetFieldTypeString();

            // Todo: if 'FieldTypeDefinition' gets used elsewhere, consider re-implementing the following:
            //if (getFieldTypeDef)
            //{
            //    var fdType = InnerDefinition.GetFieldType();
            //    if (fdType.HasValue)
            //        FieldTypeDefinition = new TypeDefWrapper(fdType.Value, getSubField: false);
            //}

            Offset = InnerDefinition.Offset;

            var typeCode = InnerDefinition.TypeCode;
            
            var isPointer = false;
            switch (typeCode)
            {
                case TypeEnum.Class:
                case TypeEnum.SzArray:
                case TypeEnum.GenericInst:
                    isPointer = true;
                    break;
            }

            IsPointer = isPointer;
            IsValueType = typeCode == TypeEnum.ValueType;

            HasValue = InnerDefinition.HasValue(out var valType);
            ConstantValueType = HasValue ? $" [{valType}]": "";

        }

        public string Name { get; }

        public string FieldType { get; }
        
        public bool HasValue { get; }
        public string ConstantValueType { get; }
        public string ConstantValueTypeShort => HasValue ? $"{ConstantValueType[2]}" : "";

        public bool IsValueType { get; }
        public bool IsPointer { get; }

        public TypeDefWrapper FieldTypeDefinition { get; }

        public TypeDefWrapper Parent { get; set; }

        public int Offset { get; set; }

        public string OffsetHex => $"[{Offset:X2}]";

        public static implicit operator FieldDefWrapper(FieldDefinition fieldDef)
        {
            return new FieldDefWrapper(fieldDef);
        }

        public override string ToString()
        {
            return $"[{Offset:X2}]{ConstantValueTypeShort} {Name} : {FieldType}";
        }

        public int GetValue()
        {
            // Todo solve and implement 
            throw new NotImplementedException();
            var type = System.Type.GetType(FieldType);
            var fieldType = MemoryProxy.Instance.Read<MonoType>(InnerDefinition.Type);
            
            var vTable = MemoryProxy.Instance.Read<ulong>(Parent.InnerDefinition.RuntimeInfo+8);

            var staticFieldsOffset = (uint)Parent.InnerDefinition.VTableSize * 8 + 0x40;

            if (MemoryProxy.Instance.Read<ulong>(vTable+staticFieldsOffset) > 0)
            {
                return 1;
            }

            return 0 ;
            if (type != null)
            {
                var mem = typeof(BasicMemory);
                var method = mem.GetMethods().First(m => m.IsGenericMethod);
                var castedMethod = method.MakeGenericMethod(type);
                var x = castedMethod.Invoke(MemoryProxy.Instance, new object[] { Parent.InnerDefinition.VTable + 0x18, 0 });

            }
            return 0;
        }
    }
}