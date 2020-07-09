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
        }

        public string Name { get; }

        public string FieldType { get; }

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
            return $"[{Offset:X2}] {Name} : {FieldType}";
        }

        public int GetValue()
        {
            // Todo solve and implement 
            throw new NotImplementedException();
            var type = System.Type.GetType(FieldType);

            var t = MemoryProxy.Instance.Read<MonoType>(InnerDefinition.Type);
            var isStatic = t.Attributes & 0x10;

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