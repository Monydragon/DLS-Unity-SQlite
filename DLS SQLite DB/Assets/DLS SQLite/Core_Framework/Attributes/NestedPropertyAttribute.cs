using System;

namespace DLS.SQLiteUnity
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NestedPropertyAttribute : Attribute
    {
        public NestedPropertyAttribute() { }

    }
}