using System;

namespace DLS.SQLiteUnity
{
    public class SQLiteException : Exception
    {
        public SQLite3_DLL_Handler.Result Result { get; private set; }

        protected SQLiteException (SQLite3_DLL_Handler.Result r,string message) : base(message)
        {
            Result = r;
        }

        public static SQLiteException New (SQLite3_DLL_Handler.Result r, string message)
        {
            return new SQLiteException (r, message);
        }

    }
}