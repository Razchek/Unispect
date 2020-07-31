using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unispect
{
    [Serializable]
    public class TypeDefWrapper
    {
        public TypeDefinition InnerDefinition;

        private string ReplaceGenericDefs(string name)
        {
            // Todo find generic params for monoclass defs
            throw new NotImplementedException();
            // We're doing this in the wrapper because we don't want to affect field data
            var genericIndexOf = name.IndexOf('`');
            if (genericIndexOf < 0) return name;

            if (int.TryParse(name.Substring(genericIndexOf + 1, 1), out var paramCount))
            {
                var ret = "T";
                if (paramCount > 1)
                {
                    for (var i = 1; i < paramCount; i++)
                        ret += $", T{i}";
                }

                return $"{name.Replace($"`{paramCount}", $"<{ret }")}>";
            }

            return name;
        }

        public TypeDefWrapper(TypeDefinition typeDef, bool isExtended = false/*, bool getSubField = true*/)
        {
            InnerDefinition = typeDef;

            FullName = InnerDefinition.GetFullName();

            Name = InnerDefinition.Name;
            Namespace = InnerDefinition.Namespace;
            ClassType = InnerDefinition.GetClassType();

            var parent = InnerDefinition.GetParent();
            if (parent.HasValue)
            {
                Parent = new TypeDefWrapper(parent.Value, true);
                ParentName = Parent.Name;
            }

            if (isExtended)
                return;

            var fields = InnerDefinition.GetFields();
            // Todo: if 'FieldTypeDefinition' gets used elsewhere, consider re-implementing the following:
            //if (fields != null)
            //{
            //    foreach (var field in fields)
            //    {
            //        var fdw = new FieldDefWrapper(field, getSubField);
            //        if (fdw.Name == "<ErrorReadingField>") continue;
            //        Fields.Add(fdw);
            //    }
            //}
            if (fields != null)
                Fields.AddRange(fields.Select(field => (FieldDefWrapper)field)
                    .Where(w => w.Name != "<ErrorReadingField>"));
            // bug Skipping invalid fields until I solve the issue

            var interfaces = InnerDefinition.GetInterfaces();
            if (interfaces != null)
            {
                foreach (var iface in interfaces)
                {
                    Interfaces.Add(new TypeDefWrapper(iface, true));
                }
            }
            InterfacesText = Interfaces.Aggregate("", (current, iface) => current + $", {iface.Name}");

            foreach (var f in Fields)
            {
                f.Parent = this;

                // If the type is ValueType and the field is not static, then we need to shift the offset back by 0x10.
                // I'm not sure why, but in all my tests this has been validated.
                if (InnerDefinition.IsValueType)
                {
                    if (!f.HasValue)
                    {
                        f.Offset -= 0x10;
                    }
                }

                //if (f.HasValue)
                //{
                //    //if (f.ValueTypeShort == "S") f.GetValue();
                //}
            }
        }

        public string ClassType { get; }

        public string Namespace { get; }

        public string Name { get; }

        public string FullName { get; }
        public TypeDefWrapper Parent { get; }
        public string ParentName { get; }

        public List<FieldDefWrapper> Fields { get; } = new List<FieldDefWrapper>();
        public List<TypeDefWrapper> Interfaces { get; } = new List<TypeDefWrapper>();

        public string InterfacesText { get; }

        public static implicit operator TypeDefWrapper(TypeDefinition typeDef)
        {
            return new TypeDefWrapper(typeDef);
        }

        #region Formatters
        public string ToTreeString(bool skipValueTypes = true)
        {
            var sb = new StringBuilder();
            sb.Append($"[{ClassType}] ");
            sb.Append(FullName);

            var parent = Parent;
            if (parent != null)
            {
                sb.Append($" : {parent.Name}");
                var interfaceList = Interfaces;
                if (interfaceList.Count > 0)
                {
                    foreach (var iface in interfaceList)
                    {
                        sb.Append($", {iface.Name}");
                    }
                }
            }

            sb.AppendLine();

            foreach (var field in Fields)
            {
                if (skipValueTypes && field.HasValue)
                    continue;

                var fieldName = field.Name;
                var fieldType = field.FieldType;
                sb.AppendLine(field.HasValue
                    ? $"    [{field.Offset:X2}][{field.ConstantValueTypeShort}] {fieldName} : {fieldType}"
                    : $"    [{field.Offset:X2}] {fieldName} : {fieldType}");
            }

            return sb.ToString();
        }

        public string ToCSharpString(string ptrName = "ulong", bool skipValueTypes = true)
        {
            var sb = new StringBuilder();

            sb.Append($"public struct {Name}");

            var parent = Parent;
            if (parent != null)
            {
                sb.Append($" // {FullName} : {parent.Name}");
                var interfaceList = Interfaces;
                if (interfaceList.Count > 0)
                {
                    foreach (var iface in interfaceList)
                    {
                        sb.Append($", {iface.Name}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("{");

            foreach (var field in Fields)
            {
                if (skipValueTypes && field.HasValue)
                    continue;

                var fieldName = field.Name;
                var fieldType = field.FieldType;

                var isPointer = field.IsPointer || fieldType == "String";

                sb.AppendLine(isPointer
                    ? $"    [FieldOffset(0x{field.Offset:X2})] public {ptrName} {fieldName}; // {fieldType.GetSimpleTypeKeyword()}"
                    : $"    [FieldOffset(0x{field.Offset:X2})] public {fieldType.GetSimpleTypeKeyword()} {fieldName};");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
        #endregion
    }
}