//
// Copyright (c) 2009-2012 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

//using System.Runtime.Remoting.Channels; //REFACTOR: Not being used.?
#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#else

#endif

namespace DLS.SQLiteUnity
{
    #region Classes

    /// <summary>
    /// Represents a parsed connection string.
    /// </summary>
    public class SQLiteConnectionString
	{
        #region Fields

        #region Private Fields

       
        #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
		static readonly string MetroStyleDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        #endif

        #endregion //END Region Private Fields

        #endregion //END Region Fields

        #region Properties

        public string ConnectionString { get; private set; }
		public string DatabasePath { get; private set; }
		public bool StoreDateTimeAsTicks { get; private set; }

        #endregion //END Region Properties

        #region Constructors

		public SQLiteConnectionString (string databasePath, bool storeDateTimeAsTicks)
		{
			ConnectionString = databasePath;
			StoreDateTimeAsTicks = storeDateTimeAsTicks;

            
            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
			DatabasePath = System.IO.Path.Combine (MetroStyleDataPath, databasePath);
            #else
            DatabasePath = databasePath;
            #endif

		}

        #endregion //END Region Constructors

	} //END Class SQLiteConnectionString

    #endregion // END Region Classes

} //END Namespace DLS.SQLiteUnity
