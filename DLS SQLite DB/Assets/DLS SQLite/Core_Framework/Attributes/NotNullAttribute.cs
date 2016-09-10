using System;

namespace DLS.SQLiteUnity
{
    [AttributeUsage (AttributeTargets.Property)]
    public class NotNullAttribute : Attribute
    {
    }
}