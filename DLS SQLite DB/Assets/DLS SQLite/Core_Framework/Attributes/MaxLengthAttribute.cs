using System;

namespace DLS.SQLiteUnity
{
    [AttributeUsage (AttributeTargets.Property)]
    public class MaxLengthAttribute : Attribute
    {
        public int Value { get; private set; }

        public MaxLengthAttribute (int length)
        {
            Value = length;
        }
    }
}