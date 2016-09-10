using System;

namespace DLS.SQLiteUnity
{
    [AttributeUsage (AttributeTargets.Property)]
    public class UniqueAttribute : IndexedAttribute
    {
        public override bool Unique {
            get { return true; }
            set { /* throw?  */ }
        }
    }
}