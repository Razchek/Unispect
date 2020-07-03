using System;
using System.Collections.Generic;
using System.Linq;
using MahApps.Metro.Converters;

namespace Unispect
{
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

        public TypeDefWrapper(TypeDefinition typeDef, bool isExtended = false)
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

    }
}