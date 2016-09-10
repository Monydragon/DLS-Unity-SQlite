using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace DLS.SQLiteUnity
{
    #region Classes

    /// <summary>
    /// Represents an open connection to a SQLite Database.
    /// </summary>
    [System.Serializable]
    public partial class SQLiteConnection : IDisposable
    {
        #region Debug

        #region Debug Tracing

        public bool Trace { get; set; }

        public delegate void TraceHandler(string message);
        public event TraceHandler TraceEvent;

        internal void InvokeTrace(string message)
        {
            if (TraceEvent != null) TraceEvent(message);
        }

        #endregion //END Region Debug Tracing

        #endregion //END Region Debug

        #region Local Structs
        private struct IndexedColumn
        {
            #region Fields

                #region Public Fields

                public int Order;
                public string ColumnName;

                #endregion //END Region Public Fields

            #endregion //END Region Fields
        }

        private struct IndexInfo
        {
            #region Fields

                #region Public Fields

                public string IndexName;
                public string TableName;
                public bool Unique;
                public List<IndexedColumn> Columns;

                #endregion //END Region Public Fields

            #endregion //END Region Fields
        }

        #endregion //END Region Local Structs

        #region Local Classes

        public class ColumnInfo : I_DB_Field
        {
           
            #region Properties
            public int ID { get; set; }

            [Column("name")]
            public string Name { get; set; }

            [Column("data")]
            public string Data { get; set; }

            public int notnull { get; set; }

            #endregion //END Region Properties

            #region Methods

            #region Public Methods

            public void AddData<T>(T _object) where T : I_DB_Data
            {
                throw new NotImplementedException();
            }

            public void SaveData<T>(T data) where T : I_DB_Data
            {
                throw new NotImplementedException();
            }

            public T LoadData<T>() where T : I_DB_Data
            {
                throw new NotImplementedException();
            }

            public IEnumerator GetEnumerator()
            {
                return (IEnumerator)this;
            }
            public override string ToString()
            {
                return Name;
            }

            #endregion //END Region Public Methods

            #endregion //End Region Methods

        }

        #endregion //END Region Local Classes 

        #region Fields

        #region Internal Fields

        internal static readonly IntPtr NullHandle = default(IntPtr);

        #endregion //END Region Internal Fields

        #region Public Fields
        //refactor: Check to see if these are still prevalent 
        public Dictionary<string, TableMapping> _mappings = null; //DLS Change To Public for testing
        public Dictionary<string, TableMapping> _tables = null;  //DLS Change To Public for testing

        // Dictionary of synchronization objects.
        //
        // To prevent Database disruption, a Database file must be accessed *synchronously*.
        // For the purpose we create synchronous objects for each Database file and store in the
        // static dictionary to share it among all connections.
        // The key of the dictionary is Database file path and its value is an object to be used
        // by lock() statement.
        //
        // Use case:
        // - Database file lock is done implicitly and automatically.
        // - To prepend deadlock, application may lock a Database file explicity by either way:
        //   - RunInTransaction(Action) locks the Database during the transaction (for insert/update)
        //   - RunInDatabaseLock(Action) similarly locks the Database but no transaction (for query)
        private static Dictionary<string, object> syncObjects = new Dictionary<string, object>();

        #endregion //END Region Public Fields

        #region Private Fields

        private bool _open;
        private TimeSpan _busyTimeout;
        private System.Diagnostics.Stopwatch _sw;
        private long _elapsedMilliseconds = 0;
        private int _transactionDepth = 0;
        private Random _rand = new Random();

        /// <summary>
        /// Used to list some code that we want the MonoTouch linker
        /// to see, but that we never want to actually execute.
        /// </summary>
#pragma warning disable 649
        private static bool _preserveDuringLinkMagic;
#pragma warning restore 649

        #endregion //END Region Private Fields

        #endregion //END Region Fields

        #region Properties

        public IntPtr Handle { get; private set; }

        public string DatabasePath { get; private set; }

        public bool TimeExecution { get; set; }

        public bool StoreDateTimeAsTicks { get; private set; }

        /// <summary>
        /// Gets the synchronous object, to be lock the Database file for updating.
        /// </summary>
        /// <value>The sync object.</value>
        public object SyncObject { get { return syncObjects[DatabasePath]; } }

        /// <summary>
        /// Sets a busy handler to sleep the specified amount of time when a table is locked.
        /// The handler will sleep multiple times until a total time of <see cref="BusyTimeout"/> has accumulated.
        /// </summary>
        public TimeSpan BusyTimeout
        {
            get { return _busyTimeout; }
            set
            {
                _busyTimeout = value;
                if (Handle != NullHandle) SQLite3_DLL_Handler.BusyTimeout(Handle, (int)_busyTimeout.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Returns the mappings from types to tables that the connection
        /// currently understands.
        /// </summary>
        public IEnumerable<TableMapping> TableMappings
        {
            get { return _tables != null ? _tables.Values : Enumerable.Empty<TableMapping>(); }
        }

        #endregion //END Region Properties

        #region Constructors
        /// <summary>
        /// Constructs a new SQLiteConnection and opens a SQLite Database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        /// Specifies the path to the Database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        /// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        /// absolutely do want to store them as Ticks in all new projects. The default of false is
        /// only here for backwards compatibility. There is a *significant* speed advantage, with no
        /// down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, bool storeDateTimeAsTicks = false)
        : this(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks)
        {

        }

        /// <summary>
        /// Constructs a new SQLiteConnection and opens a SQLite Database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        /// Specifies the path to the Database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        /// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        /// absolutely do want to store them as Ticks in all new projects. The default of false is
        /// only here for backwards compatibility. There is a *significant* speed advantage, with no
        /// down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, SQLiteOpenFlags openFlags, bool storeDateTimeAsTicks = false)
        {

            if (string.IsNullOrEmpty(databasePath)) { throw new ArgumentException("Must be specified", "databasePath"); }

            IntPtr handle;
            DatabasePath = databasePath;
            mayCreateSyncObject(databasePath);

           
            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
			SQLite3_DLL_Handler.SetDirectory(/*temp directory type*/2, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
            #endif


            #if SILVERLIGHT || USE_CSHARP_SQLITE
			var r = SQLite3_DLL_Handler.Open (databasePath, out handle, (int)openFlags, IntPtr.Zero);
            #else
            // open using the byte[]
            // in the case where the path may include Unicode
            // force open to using UTF-8 using sqlite3_open_v2
            var databasePathAsBytes = GetNullTerminatedUtf8(DatabasePath);
            var r = SQLite3_DLL_Handler.Open(databasePathAsBytes, out handle, (int)openFlags, IntPtr.Zero);
            #endif

            Handle = handle;
            if (r != SQLite3_DLL_Handler.Result.OK)
            {
                throw SQLiteException.New(r, String.Format("Could not open Database file: {0} ({1})", DatabasePath, r));
            }
            _open = true;

            StoreDateTimeAsTicks = storeDateTimeAsTicks;

            BusyTimeout = TimeSpan.FromSeconds(0.1);
        }

        static SQLiteConnection()
        {
            if (_preserveDuringLinkMagic)
            {
                var ti = new ColumnInfo();
                ti.Name = "magic";
            }
        }

        #endregion //END Region Constructors

        #region Methods

        #region Public Methods

        public void EnableLoadExtension(int onoff)
        {
            SQLite3_DLL_Handler.Result r = SQLite3_DLL_Handler.EnableLoadExtension(Handle, onoff);

            if (r != SQLite3_DLL_Handler.Result.OK)
            {
                string msg = SQLite3_DLL_Handler.GetErrmsg(Handle);
                throw SQLiteException.New(r, msg);
            }
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        /// The type whose mapping to the Database is returned.
        /// </param>         
        /// <param name="createFlags">
        /// Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>     
        /// <returns>
        /// The mapping represents the schema of the columns of the Database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            TableMapping map;

            if (_mappings == null) { _mappings = new Dictionary<string, TableMapping>();}

            if (!_mappings.TryGetValue(type.FullName, out map))
            {
                map = new TableMapping(type, createFlags);
                _mappings[type.FullName] = map;
            }

            return map;
        }

        //Added by DLS
        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        /// The type whose mapping to the Database is returned.
        /// </param>         
        /// <param name="createFlags">
        /// Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>     
        /// <returns>
        /// The mapping represents the schema of the columns of the Database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping(string tablename, Type type, CreateFlags createFlags = CreateFlags.None)
        {
            TableMapping map;

            if (_mappings == null) { _mappings = new Dictionary<string, TableMapping>();}

            if (!_mappings.TryGetValue(tablename, out map))
            {
                map = new TableMapping(tablename, type, createFlags);
                _mappings[tablename] = map;
            }

            return map;
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the Database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        /// <summary>
        /// Executes a "drop table" on the Database.  This is non-recoverable.
        /// </summary>
        public int DropTable<T>()
        {
            var map = GetMapping(typeof(T));

            var query = string.Format("drop table if exists \"{0}\"", map.TableName);

            return Execute(query);
        }

        //Added by DLS
        /// <summary>
        /// Executes a "drop table" on the Database.  This is non-recoverable.
        /// </summary>
        public int DropTable<T>(string tablename)
        {
            var query = string.Format("drop table if exists \"{0}\"", tablename);

            return Execute(query);
        }

        /// <summary>
        /// Executes a "create table if not exists" on the Database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// The number of entries added to the Database schema.
        /// </returns>
        public int CreateTable<T>(CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(typeof(T), createFlags);
        }

        //Added by DLS
        /// <summary>
        /// Executes a "create table if not exists" on the Database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// The number of entries added to the Database schema.
        /// </returns>
        public int CreateTable<T>(string TableName, CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(TableName, typeof(T), createFlags);
        }

        //Added by DLS
        /// <summary>
        /// Executes a "create table if not exists" on the Database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a Database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>  
        /// <returns>
        /// The number of entries added to the Database schema.
        /// </returns>
        public int CreateTable(string tablename, Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            TableMapping map;

            if (_tables == null) { _tables = new Dictionary<string, TableMapping>();}

            if (!_tables.TryGetValue(tablename, out map))
            {
                map = GetMapping(ty, createFlags);
                _tables.Add(tablename, map);
            }

            var query = "create table if not exists \"" + tablename + "\"(\n";
            var decls = map.Columns.Select(p => Object_Relational_Mapping.SqlDecl(p, StoreDateTimeAsTicks));
            var decl = string.Join(",\n", decls.ToArray());
            query += decl;
            query += ")";

            var count = Execute(query);

            if (count == 0)
            { //Possible bug: This always seems to return 0?
                // Table already exists, migrate it
                MigrateTable(tablename, map);
            }

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in map.Columns)
            {
                foreach (var i in c.Indices)
                {
                    IndexInfo iinfo;
                    var iname = i.Name ?? tablename + "_" + c.Name;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = tablename,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                        UnityEngine.Debug.Log("INDEX:?: " + indexes.Count);
                    }

                    if (i.Unique != iinfo.Unique)
                    {
                        throw new Exception("All the columns in an index must have the same value for their Unique property");
                    }

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                string[] columnNames = new string[index.Columns.Count];

                if (index.Columns.Count == 1)
                {
                    columnNames[0] = index.Columns[0].ColumnName;
                }
                else
                {
                    index.Columns.Sort((lhs, rhs) => 
                    {
                        return lhs.Order - rhs.Order;
                    });

                    for (int i = 0, end = index.Columns.Count; i < end; ++i)
                    {
                        columnNames[i] = index.Columns[i].ColumnName;
                    }
                }
                count += CreateIndex(indexName, index.TableName, columnNames, index.Unique);
            }

            return count;
        }

        /// <summary>
        /// Executes a "create table if not exists" on the Database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a Database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>  
        /// <returns>
        /// The number of entries added to the Database schema.
        /// </returns>
        public int CreateTable(Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            TableMapping map;

            if (_tables == null)
            {
                _tables = new Dictionary<string, TableMapping>();
            }

            if (!_tables.TryGetValue(ty.FullName, out map))
            {
                map = GetMapping(ty, createFlags);
                _tables.Add(ty.FullName, map);
            }

            var query = "create table if not exists \"" + map.TableName + "\"(\n";
            var decls = map.Columns.Select(p => Object_Relational_Mapping.SqlDecl(p, StoreDateTimeAsTicks));
            var decl = string.Join(",\n", decls.ToArray());
            query += decl;
            query += ")";

            var count = Execute(query);

            if (count == 0)
            { //Possible bug: This always seems to return 0?
                // Table already exists, migrate it
                MigrateTable(map);
            }

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in map.Columns)
            {
                foreach (var i in c.Indices)
                {
                    var iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>(),
                        };

                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                    {
                        throw new Exception("All the columns in an index must have the same value for their Unique property");
                    }

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                string[] columnNames = new string[index.Columns.Count];

                if (index.Columns.Count == 1)
                {
                    columnNames[0] = index.Columns[0].ColumnName;
                }
                else
                {
                    index.Columns.Sort((lhs, rhs) => 
                    {
                        return lhs.Order - rhs.Order;
                    });

                    for (int i = 0, end = index.Columns.Count; i < end; ++i)
                    {
                        columnNames[i] = index.Columns[i].ColumnName;
                    }
                }

                UnityEngine.Debug.Log("INDEX:?: " + indexes.Count);
                count += CreateIndex(indexName, index.TableName, columnNames, index.Unique);
            }

            return count;
        }

        /// <summary>
        /// Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the Database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string[] columnNames, bool unique = false)
        {
            const string sqlFormat = "create {2} index if not exists \"{3}\" on \"{0}\"(\"{1}\")";
            var sql = String.Format(sqlFormat, tableName, string.Join("\", \"", columnNames), unique ? "unique" : "", indexName);
            return Execute(sql);
        }

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the Database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(indexName, tableName, new string[] { columnName }, unique);
        }

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="tableName">Name of the Database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(tableName + "_" + columnName, tableName, columnName, unique);
        }

        /// <summary>
        /// Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="tableName">Name of the Database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string[] columnNames, bool unique = false)
        {
            return CreateIndex(tableName + "_" + string.Join("_", columnNames), tableName, columnNames, unique);
        }

        /// <summary>
        /// Creates an index for the specified object property.
        /// e.g. CreateIndex<Client>(c => c.Name);
        /// </summary>
        /// <typeparam name="T">Type to reflect to a Database table.</typeparam>
        /// <param name="property">Property to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public void CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false)
        {
            MemberExpression mx;

            if (property.Body.NodeType == ExpressionType.Convert)
            {
                mx = ((UnaryExpression)property.Body).Operand as MemberExpression;
            }
            else
            {
                mx = (property.Body as MemberExpression);
            }

            var propertyInfo = mx.Member as PropertyInfo;

            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }

            var propName = propertyInfo.Name;

            var map = GetMapping<T>();
            var colName = map.FindColumnWithPropertyName(propName).Name;

            CreateIndex(map.TableName, colName, unique);
        }

        public List<ColumnInfo> GetTableInfo(string tableName)
        {
            var query = "pragma table_info(\"" + tableName + "\")";
            return Query<ColumnInfo>(query);
        }

        /// <summary>
        /// Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        /// in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        /// A <see cref="SQLiteCommand"/>
        /// </returns>
        public SQLiteCommand CreateCommand(string cmdText, params object[] ps)
        {
            if (!_open) throw SQLiteException.New(SQLite3_DLL_Handler.Result.Error, "Cannot create commands from unopened Database");

            var cmd = NewCommand();
            cmd.CommandText = cmdText;

            foreach (var o in ps)
            {
                cmd.Bind(o);
            }

            return cmd;
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// Use this method instead of Query when you don't expect rows back. Such cases include
        /// INSERTs, UPDATEs, and DELETEs.
        /// You can set the Trace or TimeExecution properties of the connection
        /// to profile execution.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// The number of rows modified in the Database as a result of this execution.
        /// </returns>
        public int Execute(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (_sw == null) { _sw = new Stopwatch();}

                _sw.Reset();
                _sw.Start();
            }

            var r = cmd.ExecuteNonQuery();

            if (TimeExecution)
            {
                _sw.Stop();
                _elapsedMilliseconds += _sw.ElapsedMilliseconds;
                Debug.WriteLine(string.Format("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds, _elapsedMilliseconds / 1000.0));
            }

            return r;
        }

        public T ExecuteScalar<T>(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (_sw == null) { _sw = new Stopwatch();}

                _sw.Reset();
                _sw.Start();
            }

            var r = cmd.ExecuteScalar<T>();

            if (TimeExecution)
            {
                _sw.Stop();
                _elapsedMilliseconds += _sw.ElapsedMilliseconds;
                Debug.WriteLine(string.Format("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds, _elapsedMilliseconds / 1000.0));
            }

            return r;
        }

        //Added by DLS
        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="tablename">The name of the table.</param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string tablename, string query, params object[] args) where T : I_DB_Field
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<T>(tablename);
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string query, params object[] args) where T : I_DB_Field
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<T>();
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// The enumerator will call sqlite3_step on each call to MoveNext, so the Database
        /// connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : I_DB_Field
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<T>();
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the specified mapping. This function is
        /// only used by libraries in order to query the Database via introspection. It is
        /// normally not used.
        /// </summary>
        /// <param name="map">
        /// A <see cref="TableMapping"/> to use to convert the resulting rows
        /// into objects.
        /// </param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<object> Query(TableMapping map, string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<object>(map);
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the specified mapping. This function is
        /// only used by libraries in order to query the Database via introspection. It is
        /// normally not used.
        /// </summary>
        /// <param name="map">
        /// A <see cref="TableMapping"/> to use to convert the resulting rows
        /// into objects.
        /// </param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// The enumerator will call sqlite3_step on each call to MoveNext, so the Database
        /// connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable<object> DeferredQuery(TableMapping map, string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<object>(map);
        }

        //Added by DLS
        /// <summary>
        /// Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// A queryable object that is able to translate Where, OrderBy, and Take
        /// queries into native SQL.
        /// </returns>
        public TableQuery<T> Table<T>(string tablename) where T : I_DB_Field
        {
            return new TableQuery<T>(tablename, this, typeof(T));
        }

        //Added by DLS
        /// <summary>
        /// Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// A queryable object that is able to translate Where, OrderBy, and Take
        /// queries into native SQL.
        /// </returns>
        //Test: Test - Unsure if it works.
        public TableQuery<T> Table<T, F>(T tablename, F tablefield) where T : I_DB_Data where F : I_DB_Field
        {
            return new TableQuery<T>(tablename.GetType().Name, this, typeof(T));
        }

        //Added by DLS
        /// <summary>
        /// Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// A queryable object that is able to translate Where, OrderBy, and Take
        /// queries into native SQL.
        /// </returns>
        //Test: Test - Unsure if it works.
        public TableQuery<T> Table<T>(string tablename, T tablefield) where T : I_DB_Field
        {
            return new TableQuery<T>(tablename, this, typeof(T));
        }

        /// <summary>
        /// Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// A queryable object that is able to translate Where, OrderBy, and Take
        /// queries into native SQL.
        /// </returns>
        public TableQuery<T> Table<T>() where T : I_DB_Field
        {
            return new TableQuery<T>(this);
        }

        //Added by DLS
        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        public T Get<T>(string tablename, T pk) where T : I_DB_Field
        {
            var map = GetMapping(tablename, typeof(T));
            return Query<T>(tablename, map.GetByPrimaryKeySql, pk.ID).First();
        }

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        public T Get<T>(T pk) where T : I_DB_Field
        {
            var map = GetMapping(typeof(T));
            return Query<T>(map.GetByPrimaryKeySql, pk).First();
        }

        //Added by DLS
        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        public T Get<T>(string tablename, Expression<Func<T, bool>> predicate) where T : I_DB_Field
        {
            return Table<T>(tablename).Where(predicate).First();
        }

        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        public T Get<T>(Expression<Func<T, bool>> predicate) where T : I_DB_Field
        {
            return Table<T>().Where(predicate).First();
        }

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key or null
        /// if the object is not found.
        /// </returns>
        public T Find<T>(object pk) where T : I_DB_Field
        {
            var map = GetMapping(typeof(T));
            return Query<T>(map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <param name="map">
        /// The TableMapping used to identify the object type.
        /// </param>
        /// <returns>
        /// The object with the given primary key or null
        /// if the object is not found.
        /// </returns>
        public object Find(object pk, TableMapping map)
        {
            return Query(map, map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate or null
        /// if the object is not found.
        /// </returns>
        public T Find<T>(Expression<Func<T, bool>> predicate) where T : I_DB_Field
        {
            return Table<T>().Where(predicate).FirstOrDefault();
        }

        /// <summary>
        /// Whether <see cref="BeginTransaction"/> has been called and the Database is waiting for a <see cref="Commit"/>.
        /// </summary>
        public bool IsInTransaction
        {
            get { return _transactionDepth > 0; }
        }

        /// <summary>
        /// Begins a new transaction. Call <see cref="Commit"/> to end the transaction.
        /// </summary>
        /// <example cref="System.InvalidOperationException">Throws if a transaction has already begun.</example>
        public void BeginTransaction()
        {
            // The BEGIN command only works if the transaction stack is empty, 
            //    or in other words if there are no pending transactions. 
            // If the transaction stack is not empty when the BEGIN command is invoked, 
            //    then the command fails with an error.
            // Rather than crash with an error, we will just ignore calls to BeginTransaction
            //    that would result in an error.
            if (Interlocked.CompareExchange(ref _transactionDepth, 1, 0) == 0)
            {
                try
                {
                    Execute("begin transaction");
                }
                catch (Exception ex)
                {
                    var sqlExp = ex as SQLiteException;
                    if (sqlExp != null)
                    {
                        // It is recommended that applications respond to the errors listed below 
                        //    by explicitly issuing a ROLLBACK command.
                        // TODO: This rollback failsafe should be localized to all throw sites.
                        switch (sqlExp.Result)
                        {
                            case SQLite3_DLL_Handler.Result.IOError:
                            case SQLite3_DLL_Handler.Result.Full:
                            case SQLite3_DLL_Handler.Result.Busy:
                            case SQLite3_DLL_Handler.Result.NoMem:
                            case SQLite3_DLL_Handler.Result.Interrupt:

                            RollbackTo(null, true);
                            break;
                        }
                    }
                    else
                    {
                        // Call decrement and not VolatileWrite in case we've already 
                        //    created a transaction point in SaveTransactionPoint since the catch.
                        Interlocked.Decrement(ref _transactionDepth);
                    }

                    throw sqlExp; //added sqlExp throw it was an empty throw before.
                }
            }
            else
            {
                // Calling BeginTransaction on an already open transaction is invalid
                throw new InvalidOperationException("Cannot begin a transaction while already in a transaction.");
            }
        }

        /// <summary>
        /// Creates a savepoint in the Database at the current point in the transaction timeline.
        /// Begins a new transaction if one is not in progress.
        /// 
        /// Call <see cref="RollbackTo"/> to undo transactions since the returned savepoint.
        /// Call <see cref="Release"/> to commit transactions after the savepoint returned here.
        /// Call <see cref="Commit"/> to end the transaction, committing all changes.
        /// </summary>
        /// <returns>A string naming the savepoint.</returns>
        public string SaveTransactionPoint()
        {
            int depth = Interlocked.Increment(ref _transactionDepth) - 1;
            string retVal = "S" + _rand.Next(short.MaxValue) + "D" + depth;

            try
            {
                Execute("savepoint " + retVal);
            }
            catch (Exception ex)
            {
                var sqlExp = ex as SQLiteException;
                if (sqlExp != null)
                {
                    // It is recommended that applications respond to the errors listed below 
                    //    by explicitly issuing a ROLLBACK command.
                    // TODO: This rollback failsafe should be localized to all throw sites.
                    switch (sqlExp.Result)
                    {
                        case SQLite3_DLL_Handler.Result.IOError:
                        case SQLite3_DLL_Handler.Result.Full:
                        case SQLite3_DLL_Handler.Result.Busy:
                        case SQLite3_DLL_Handler.Result.NoMem:
                        case SQLite3_DLL_Handler.Result.Interrupt:

                        RollbackTo(null, true);
                        break;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref _transactionDepth);
                }

                throw sqlExp; //added sqlExp throw it was an empty throw before.
            }

            return retVal;
        }

        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="BeginTransaction"/> or <see cref="SaveTransactionPoint"/>.
        /// </summary>
        public void Rollback()
        {
            RollbackTo(null, false);
        }

        /// <summary>
        /// Rolls back the savepoint created by <see cref="BeginTransaction"/> or SaveTransactionPoint.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint"/>.  If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback"/></param>
        public void RollbackTo(string savepoint)
        {
            RollbackTo(savepoint, false);
        }

        /// <summary>
        /// Releases a savepoint returned from <see cref="SaveTransactionPoint"/>.  Releasing a savepoint 
        ///    makes changes since that savepoint permanent if the savepoint began the transaction,
        ///    or otherwise the changes are permanent pending a call to <see cref="Commit"/>.
        /// 
        /// The RELEASE command is like a COMMIT for a SAVEPOINT.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to release.  The string should be the result of a call to <see cref="SaveTransactionPoint"/></param>
        public void Release(string savepoint)
        {
            DoSavePointExecute(savepoint, "release ");
        }

        /// <summary>
        /// Commits the transaction that was begun by <see cref="BeginTransaction"/>.
        /// </summary>
        public void Commit()
        {
            if (Interlocked.Exchange(ref _transactionDepth, 0) != 0) Execute("commit");

            // Do nothing on a commit with no open transaction
        }

        /// <summary>
        /// Executes <param name="action"> within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
        /// exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
        /// is rethrown.
        /// </summary>
        /// <param name="action">
        /// The <see cref="Action"/> to perform within a transaction. <param name="action"> can contain any number
        /// of operations on the connection but should never call <see cref="BeginTransaction"/> or
        /// <see cref="Commit"/>.
        /// </param>
        public void RunInTransaction(Action action)
        {
            try
            {
                lock (syncObjects[DatabasePath])
                {
                    var savePoint = SaveTransactionPoint();
                    action();
                    Release(savePoint);
                }
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        /// Executes <param name="action"> while blocking other threads to access the same Database.
        /// </summary>
        /// <param name="action">
        /// The <see cref="Action"/> to perform within a lock.
        /// </param>
        public void RunInDatabaseLock(Action action)
        {
            lock (syncObjects[DatabasePath])
            {
                action();
            }
        }

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int InsertAll(System.Collections.IEnumerable objects)
        {
            var c = 0;
            RunInTransaction(() => 
            {
                foreach (var r in objects)
                {
                    c += Insert(r);
                }
            });

            return c;
        }

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int InsertAll(System.Collections.IEnumerable objects, string extra)
        {
            var c = 0;
            RunInTransaction(() => 
            {
                foreach (var r in objects)
                {
                    c += Insert(r, extra);
                }
            });

            return c;
        }

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int InsertAll(System.Collections.IEnumerable objects, Type objType)
        {
            var c = 0;
            RunInTransaction(() => 
            {
                foreach (var r in objects)
                {
                    c += Insert(r, objType);
                }
            });

            return c;
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(string tablename, object obj)
        {
            if (obj == null) { return 0;}

            return Insert(tablename, obj, "", obj.GetType());
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj)
        {
            if (obj == null) { return 0;}

            return Insert(obj, "", obj.GetType());
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// If a UNIQUE constraint violation occurs with
        /// some pre-existing object, this function deletes
        /// the old object.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int InsertOrReplace(object obj)
        {
            if (obj == null) { return 0;}

            return Insert(obj, "OR REPLACE", obj.GetType());
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, Type objType)
        {
            return Insert(obj, "", objType);
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// If a UNIQUE constraint violation occurs with
        /// some pre-existing object, this function deletes
        /// the old object.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int InsertOrReplace(object obj, Type objType)
        {
            return Insert(obj, "OR REPLACE", objType);
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, string extra)
        {
            if (obj == null) { return 0;}

            return Insert(obj, extra, obj.GetType());
        }

        //ADDED BY DLS
        public object GetNestedPropValue(object obj, string name)
        {
            foreach (string part in name.Split('.'))
            {
                if (obj == null) { return null;}

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);

                if (info == null) { return null;}

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        //ADDED by DLS
        public PropertyInfo[] GetValidProperties(PropertyInfo[] property_infos)
        {
            List<PropertyInfo> valid_props = new List<PropertyInfo>();

            foreach (var property in property_infos)
            {
                if (property.CanWrite && property.GetCustomAttributes(typeof (IgnoreAttribute), true).Any() == false)
                {
                    valid_props.Add(property);
                }
            }
            return valid_props.ToArray();
        }

        //ADDED BY DLS
        public PropertyInfo[] GetValidProperties(object obj)
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            List<PropertyInfo> valid_properties = new List<PropertyInfo>();
            foreach (var property in properties)
            {
                if (property.CanWrite && property.GetCustomAttributes(typeof (IgnoreAttribute), true).Any() == false && property.Name != "ID")
                {
                    valid_properties.Add(property);
                }
            }
            return valid_properties.ToArray();
        }

        //ADDED BY DLS
        public PropertyInfo[] GetValidProperties(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            List<PropertyInfo> valid_properties = new List<PropertyInfo>();
            foreach (var property in properties)
            {
                if (property.CanWrite && property.GetCustomAttributes(typeof(IgnoreAttribute), true).Any() == false)
                {
                    valid_properties.Add(property);
                }
            }
            return valid_properties.ToArray();
        }



        public bool isValidProperty(PropertyInfo property_info)
        {
//            Type type = property_info.PropertyType;

            if (property_info.GetCustomAttributes(typeof(IgnoreAttribute), true).Any() || property_info.CanWrite == false) { return false;}

            return true;
        }

        public object[] GetPropertyValues(string tablename, object obj, string extra)
        {
            if (obj == null) { return null; }
            var map = GetMapping(tablename, obj.GetType());
            var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;
            var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
            int nested = 0;
            int nested_props_count = 0;
            int index_offset = 0;
            PropertyInfo[] properties = GetValidProperties(obj);
            List<object> values = new List<object>();
            object val = new object();

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].GetCustomAttributes(typeof(NestedPropertyAttribute), true).Any())
                {
                    var nested_properties = GetValidProperties(properties[i].PropertyType);
                    ++nested;
                    foreach (var nested_property in nested_properties)
                    {
                        var nested_obj = GetNestedPropValue(obj,properties[i].Name);
                        val = nested_property.GetValue(nested_obj, null);
 //                        var val = nested_obj.GetType().GetProperty(cols[p].PropertyName).GetValue(nested_obj, null);
                        values.Add(val);
                        ++nested_props_count;
                        //                        values.Add(nested_property.GetValue(obj, null));
                    }
                }
                else if (properties[i].GetCustomAttributes(typeof(NestedPropertyAttribute), true).Any() == false)
                {
                    index_offset = i + nested_props_count - nested;
                    val = cols[index_offset].GetValue(obj);
                    values.Add(val);
                }
            }


//            foreach (var prop in properties)
//            {
//                if (prop.GetCustomAttributes(typeof (NestedPropertyAttribute), true).Any())
//                {
//                    var nested_properties = GetValidProperties(prop.GetType());
//                    foreach (var nested_property in nested_properties)
//                    {
//                        values.Add(nested_property.GetValue(obj,null));
//                    }
//                }
//                else
//                {
//                    val = cols[col_index].GetValue(obj);
//                    values.Add(val);
//                }
//                ++col_index;
//            }
            return values.ToArray();
        }


        //Added by DLS
        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(string tablename, object obj, string extra, Type objType)
        {
            if (obj == null || objType == null) { return 0; }

            var map = GetMapping(tablename, objType);

#if NETFX_CORE
			if (map.PK != null && map.PK.IsAutoGuid)
			{
				// no GetProperty so search our way up the inheritance chain till we find it
				PropertyInfo prop;
				while (objType != null)
				{
					var info = objType.GetTypeInfo();
					prop = info.GetDeclaredProperty(map.PK.PropertyName);
					if (prop != null) 
					{
						if (prop.GetValue(obj, null).Equals(Guid.Empty)){ prop.SetValue(obj, Guid.NewGuid(), null); }
						break; 
					}

					objType = info.BaseType;
				}
			}
#else
            if (map.PK != null && map.PK.IsAutoGuid)
            {
                var prop = objType.GetProperty(map.PK.PropertyName);

                if (prop != null)
                {
                    //if (prop.GetValue(obj, null).Equals(Guid.Empty)) { 
                    if (prop.GetGetMethod().Invoke(obj, null).Equals(Guid.Empty)) { prop.SetValue(obj, Guid.NewGuid(), null); }

                }
            }
            #endif

            var vals = GetPropertyValues(tablename, obj, extra);
//            var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;
//
//            var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
//            var vals = new object[cols.Length];
//
//            //ADDED DLS
//            var props = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
//            int nest_count = 0;
//            int total_nested_props = 0;
//
//            for (var i = 0; i < vals.Length; i++)
//            {
//                if (i >= props.Length)
//                {
//                    break;
//                }
//
//                var is_nested = props[i].GetCustomAttributes(typeof(NestedPropertyAttribute), true).Any();
//
//                if (is_nested)
//                {
//                    var nested_props = props[i].PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
//                    var valid_nested_props = GetValidProperties(nested_props);
//                    var nested_obj = GetNestedPropValue(obj, props[i].Name);
//                    for (int p = i + nest_count; p < i + valid_nested_props.Length + nest_count; p++)
//                    {
//                        //                         //bug: fix index to handle more than 1 nested handling per class. (Fixed?)
//                        var val = nested_obj.GetType().GetProperty(cols[p].PropertyName).GetValue(nested_obj, null);
//                        UnityEngine.Debug.Log("Lol?");
//                        vals[p] = val;
//                        ++total_nested_props;
//
//                    }
//                    ++nest_count;
//
//                    UnityEngine.Debug.Log(props[i].Name + " Is Nested");
//                }
//                //DLS ADD END
//                else
//                {
//                    var modified_index = i + total_nested_props - nest_count;
////                    var modified_index = i + nest_count;
////                    UnityEngine.Debug.Log( "PROVIDED TYPE: " + props[modified_index].PropertyType.Name  + " \nEXPECTED TYPE: " + cols[modified_index].ColumnType.Name );
//
//                    vals[modified_index] = cols[modified_index].GetValue(obj);
//                }
//            }

            var insertCmd = map.GetInsertCommand(this, extra);
            int count;

            try
            {
                count = insertCmd.ExecuteNonQuery(vals);
            }
            catch (SQLiteException ex)
            {

                if (SQLite3_DLL_Handler.ExtendedErrCode(this.Handle) == SQLite3_DLL_Handler.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex.Result, ex.Message, map, obj);
                }
                throw;
            }

            if (map.HasAutoIncPK)
            {
                var id = SQLite3_DLL_Handler.LastInsertRowid(Handle);
                map.SetAutoIncPK(obj, id);
            }

            return count;
        }


        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, string extra, Type objType)
        {
            if (obj == null || objType == null)
            {
                return 0;
            }


            var map = GetMapping(objType);

#if NETFX_CORE
			if (map.PK != null && map.PK.IsAutoGuid)
			{
				// no GetProperty so search our way up the inheritance chain till we find it
				PropertyInfo prop;
				while (objType != null)
				{
					var info = objType.GetTypeInfo();
					prop = info.GetDeclaredProperty(map.PK.PropertyName);
					if (prop != null) 
					{
						if (prop.GetValue(obj, null).Equals(Guid.Empty))
						{
							prop.SetValue(obj, Guid.NewGuid(), null);
						}
						break; 
					}

					objType = info.BaseType;
				}
			}
#else
            if (map.PK != null && map.PK.IsAutoGuid)
            {
                var prop = objType.GetProperty(map.PK.PropertyName);
                if (prop != null)
                {
                    //if (prop.GetValue(obj, null).Equals(Guid.Empty)) { 
                    if (prop.GetGetMethod().Invoke(obj, null).Equals(Guid.Empty))
                    {
                        prop.SetValue(obj, Guid.NewGuid(), null);
                    }
                }
            }
#endif


            var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

            var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
            var vals = new object[cols.Length];
            for (var i = 0; i < vals.Length; i++)
            {
                vals[i] = cols[i].GetValue(obj);
            }

            var insertCmd = map.GetInsertCommand(this, extra);
            int count;

            try
            {
                count = insertCmd.ExecuteNonQuery(vals);
            }
            catch (SQLiteException ex)
            {

                if (SQLite3_DLL_Handler.ExtendedErrCode(this.Handle) == SQLite3_DLL_Handler.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex.Result, ex.Message, map, obj);
                }
                throw;
            }

            if (map.HasAutoIncPK)
            {
                var id = SQLite3_DLL_Handler.LastInsertRowid(Handle);
                map.SetAutoIncPK(obj, id);
            }

            return count;
        }

        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return Update(obj, obj.GetType());
        }

        //Added by DLS
        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update(string tablename, object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return Update(tablename, obj, obj.GetType());
        }

        //Added by DLS
        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update(string tablename, object obj, Type objType)
        {
            int rowsAffected = 0;
            if (obj == null || objType == null)
            {
                return 0;
            }

            var map = GetMapping(tablename, objType);

            var pk = map.PK;

            if (pk == null)
            {
                throw new NotSupportedException("Cannot update " + tablename + ": it has no PK");
            }

            var cols = from p in map.Columns
                       where p != pk
                       select p;
            var vals = from c in cols
                       select c.GetValue(obj);
            var ps = new List<object>(vals);
            ps.Add(pk.GetValue(obj));
            var q = string.Format("update \"{0}\" set {1} where {2} = ? ", tablename, string.Join(",", (from c in cols
                                                                                                        select "\"" + c.Name + "\" = ? ").ToArray()), pk.Name);

            try
            {
                rowsAffected = Execute(q, ps.ToArray());
            }
            catch (SQLiteException ex)
            {

                if (ex.Result == SQLite3_DLL_Handler.Result.Constraint && SQLite3_DLL_Handler.ExtendedErrCode(this.Handle) == SQLite3_DLL_Handler.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex, map, obj);
                }

                throw ex;
            }

            return rowsAffected;
        }

        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update(object obj, Type objType)
        {
            int rowsAffected = 0;
            if (obj == null || objType == null)
            {
                return 0;
            }

            var map = GetMapping(objType);

            var pk = map.PK;

            if (pk == null)
            {
                throw new NotSupportedException("Cannot update " + map.TableName + ": it has no PK");
            }

            var cols = from p in map.Columns
                       where p != pk
                       select p;

            var vals = from c in cols
                       select c.GetValue(obj);

            var ps = new List<object>(vals);
            ps.Add(pk.GetValue(obj));

            var q = string.Format("update \"{0}\" set {1} where {2} = ? ", map.TableName, string.Join(",", (from c in cols
                                                                                                            select "\"" + c.Name + "\" = ? ").ToArray()), pk.Name);

            try
            {
                rowsAffected = Execute(q, ps.ToArray());
            }
            catch (SQLiteException ex)
            {
                if (ex.Result == SQLite3_DLL_Handler.Result.Constraint && SQLite3_DLL_Handler.ExtendedErrCode(this.Handle) == SQLite3_DLL_Handler.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex, map, obj);
                }

                throw ex;
            }

            return rowsAffected;
        }

        /// <summary>
        /// Updates all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int UpdateAllFields(System.Collections.IEnumerable objects)
        {
            var c = 0;
            RunInTransaction(() => {
                foreach (var r in objects)
                {
                    c += Update(r);
                }
            });
            return c;
        }

        //Added by DLS
        /// <summary>
        /// Updates all specified objects on the table which name is provided.
        /// </summary>
        /// <param name="objects">
        /// A List of <see cref="I_DB_Field"/> to insert.
        /// </param>
        /// <param name="tablename">
        /// the table name where to insert the objects
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int UpdateAllFields(string tablename, IEnumerable<I_DB_Field> objects)
        {
            var c = 0;
            RunInTransaction(() => {
                foreach (var r in objects)
                {
                    c += Update(tablename, r);
                }
            });
            return c;
        }


        //Added by DLS
        /// <summary>
        /// Updates all the fields with the data provided. based on the field type provided
        /// </summary>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> Type.</typeparam>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type.</typeparam>
        /// <param name="data">The Data to Update.</param>
        /// <param name="field_type">The Fields to Update.</param>
        /// <returns>Returns the Amount updated.</returns>
        //Test: Test to make sure it's working.
        public int UpdateAllFields<F, T>(T data, F field_type) where T : I_DB_Data where F : I_DB_Field
        {
            var c = 0;
            var table_name = data.GetType().Name;
            //            UnityEngine.Debug.Log($"Table: {table_name} Field Count:{Table<F>(table_name).Count()} Field Type:{field_type.GetType().Name}");
            RunInTransaction(() => {
                for (int i = 0; i < Table<F>(table_name).Count(); i++)
                {
                    UnityEngine.Debug.Log("update #" + i + 1);
                    c += Update(table_name, field_type);
                }
            });
            return c;
        }

        //Added by DLS
        /// <summary>
        /// Updates all the fields with the data provided. for the Default Field Type: <see cref="Base_Field_Structure"/>
        /// </summary>
        /// <typeparam name="T">The Type of Data</typeparam>
        /// <param name="data">The Database Data based on <see cref="I_DB_Data"/></param>
        /// <returns>Returns the Amount updated</returns>
        //Test: Test to make sure it's working.
        public int UpdateAllFields<T>(T data) where T : I_DB_Data
        {
            var c = 0;
            var table_name = data.GetType().Name;
            UnityEngine.Debug.Log("Field Count: " + Table<Base_Field_Structure>(table_name).Count());
            RunInTransaction(() => {
                for (int i = 0; i < Table<Base_Field_Structure>(table_name).Count(); i++)
                {
                    c += Update(table_name);
                }
            });
            return c;
        }

        //Added by DLS
        /// <summary>
        /// Deletes the given object from the Database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        /// The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows deleted.
        /// </returns>
        public int Delete(string tablename, object objectToDelete)
        {
            var map = GetMapping(tablename, objectToDelete.GetType());
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + tablename + ": it has no PK");
            }
            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", tablename, pk.Name);
            return Execute(q, pk.GetValue(objectToDelete));
        }

        //Added by DLS
        /// <summary>
        /// Deletes the object on the table name provided and the ID provided.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public int Delete(string tablename, int ID)
        {
            var q = string.Format("delete from \"{0}\" where \"ID\" = ?", tablename);
            UnityEngine.Debug.Log("QUERY: " + q);
            return Execute(q, ID);
        }

        public int Delete(I_DB_Data database_object)
        {
            var q = string.Format("DELETE from \"{0}\" where \"Name\" = ?", database_object.GetType().Name);
            UnityEngine.Debug.Log("QUERY: " + q + " ? = " + database_object.Name);
            return Execute(q, database_object.Name);
        }

        public int Delete(string tablename, I_DB_Data database_object)
        {
            var q = string.Format("DELETE from \"{0}\" where \"Name\" = ?", tablename);
            UnityEngine.Debug.Log("QUERY: " + q + " ? = " + database_object.Name);
            return Execute(q, database_object.Name);
        }

        public int Delete(string tablename, string name)
        {
            var q = string.Format("DELETE from \"{0}\" where \"Name\" = ?", tablename);
            UnityEngine.Debug.Log("QUERY: " + q + " ? = " + name);
            return Execute(q, name);
        }

        /// <summary>
        /// Deletes the given object from the Database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        /// The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows deleted.
        /// </returns>
        public int Delete(object objectToDelete)
        {
            var map = GetMapping(objectToDelete.GetType());
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }
            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, pk.GetValue(objectToDelete));
        }

        /// <summary>
        /// Deletes the object with the specified primary key.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key of the object to delete.
        /// </param>
        /// <returns>
        /// The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        /// The type of object.
        /// </typeparam>
        public int Delete<T>(object primaryKey)
        {
            var map = GetMapping(typeof(T));
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }
            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, primaryKey);
        }

        /// <summary>
        /// Deletes all the objects from the specified table.
        /// WARNING WARNING: Let me repeat. It deletes ALL the objects from the
        /// specified table. Do you really want to do that?
        /// </summary>
        /// <returns>
        /// The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        /// The type of objects to delete.
        /// </typeparam>
        public int DeleteAll<T>()
        {
            var map = GetMapping(typeof(T));
            var query = string.Format("delete from \"{0}\"", map.TableName);
            return Execute(query);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            if (_open && Handle != NullHandle)
            {
                try
                {
                    if (_mappings != null)
                    {
                        foreach (var sqlInsertCommand in _mappings.Values)
                        {
                            sqlInsertCommand.Dispose();
                        }
                    }
                    var r = SQLite3_DLL_Handler.Close(Handle);
                    if (r != SQLite3_DLL_Handler.Result.OK)
                    {
                        string msg = SQLite3_DLL_Handler.GetErrmsg(Handle);
                        throw SQLiteException.New(r, msg);
                    }
                }
                finally
                {
                    Handle = NullHandle;
                    _open = false;
                }
            }
        }

        #endregion //End Region Public Methods

        #region Protected Methods

        /// <summary>
        /// Creates a new SQLiteCommand. Can be overridden to provide a sub-class.
        /// </summary>
        /// <seealso cref="SQLiteCommand.OnInstanceCreated"/>
        protected virtual SQLiteCommand NewCommand()
        {
            return new SQLiteCommand(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Close();
        }

        #endregion //End Region Protected Methods

        #region Private Methods

        private void mayCreateSyncObject(string databasePath)
        {
            if (!syncObjects.ContainsKey(databasePath))
            {
                syncObjects[databasePath] = new object();
            }
        }

        private static byte[] GetNullTerminatedUtf8(string s)
        {
            var utf8Length = System.Text.Encoding.UTF8.GetByteCount(s);
            var bytes = new byte[utf8Length + 1];
            utf8Length = System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            return bytes;
        }

        //Added by DLS
        private void MigrateTable(string tablename, TableMapping map)
        {
            var existingCols = GetTableInfo(tablename);

            var toBeAdded = new List<TableMapping.Column>();

            foreach (var p in map.Columns)
            {
                var found = false;
                foreach (var c in existingCols)
                {
                    found = (string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (found)
                        break;
                }
                if (!found)
                {
                    toBeAdded.Add(p);
                }
            }

            foreach (var p in toBeAdded)
            {
                var addCol = "alter table \"" + tablename + "\" add column " + Object_Relational_Mapping.SqlDecl(p, StoreDateTimeAsTicks);
                Execute(addCol);
            }
        }

        private void MigrateTable(TableMapping map)
        {
            var existingCols = GetTableInfo(map.TableName);

            var toBeAdded = new List<TableMapping.Column>();

            foreach (var p in map.Columns)
            {
                var found = false;
                foreach (var c in existingCols)
                {
                    found = (string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (found)
                        break;
                }
                if (!found)
                {
                    toBeAdded.Add(p);
                }
            }

            foreach (var p in toBeAdded)
            {
                var addCol = "alter table \"" + map.TableName + "\" add column " + Object_Relational_Mapping.SqlDecl(p, StoreDateTimeAsTicks);
                Execute(addCol);
            }
        }


        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="BeginTransaction"/>.
        /// </summary>
        /// <param name="noThrow">true to avoid throwing exceptions, false otherwise</param>
        private void RollbackTo(string savepoint, bool noThrow)
        {
            // Rolling back without a TO clause rolls backs all transactions 
            //    and leaves the transaction stack empty.   
            try
            {
                if (String.IsNullOrEmpty(savepoint))
                {
                    if (Interlocked.Exchange(ref _transactionDepth, 0) > 0)
                    {
                        Execute("rollback");
                    }
                }
                else {
                    DoSavePointExecute(savepoint, "rollback to ");
                }
            }
            catch (SQLiteException)
            {
                if (!noThrow)
                    throw;

            }
            // No need to rollback if there are no transactions open.
        }

        private void DoSavePointExecute(string savepoint, string cmd)
        {
            // Validate the savepoint
            int firstLen = savepoint.IndexOf('D');
            if (firstLen >= 2 && savepoint.Length > firstLen + 1)
            {
                int depth;
                if (Int32.TryParse(savepoint.Substring(firstLen + 1), out depth))
                {
                    // TODO: Mild race here, but inescapable without locking almost everywhere.
                    if (0 <= depth && depth < _transactionDepth)
                    {
#if NETFX_CORE
						Volatile.Write (ref _transactionDepth, depth);
#elif SILVERLIGHT
						_transactionDepth = depth;
#else
                        Thread.VolatileWrite(ref _transactionDepth, depth);
#endif
                        Execute(cmd + savepoint);
                        return;
                    }
                }
            }

            throw new ArgumentException("savePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savePoint");
        }

        #endregion //End Region Private Methods

        #endregion //End Region Methods

        #region Deconstructors
        ~SQLiteConnection()
        {
            Dispose(false);
        }

        #endregion //END Region Deconstructors

    } //END Partial Class SQLiteConnection

    #endregion // END Region Classes

} //END Namespace DLS.SQLiteUnity