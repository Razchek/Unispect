using System;

namespace Unispect
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UnispectPluginAttribute : Attribute
    {
        public UnispectPluginAttribute()
        {
        }

        public UnispectPluginAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }
}