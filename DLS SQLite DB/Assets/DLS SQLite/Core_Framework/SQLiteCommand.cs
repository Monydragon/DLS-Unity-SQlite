using System;
using System.Collections.Generic;
using System.Linq;

namespace DLS.SQLiteUnity
{
    #region Classes

    public partial class SQLiteCommand
    {
        #region Local Classes

        private class Binding
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public int Index { get; set; }
        }

        #endregion //END Region Local Classes  

        #region Fields

        #region Private Fields

        private SQLiteConnection _conn;
        private List<Binding> _bindings;

        #endregion //END Region Private Fields

        #endregion //END Region Fields

        #region Properties
        public string CommandText { get; set; }

        #endregion //END Region Properties

        #region Constructors

        internal SQLiteCommand(SQLiteConnection conn)
        {
            _conn = conn;
            _bindings = new List<Binding>();
            CommandText = "";
        }

        #endregion //END Region Constructors

        #region Methods

        #region Public Methods
        public int ExecuteNonQuery()
        {
            if (_conn.Trace) { _conn.InvokeTrace("Executing: " + this); }

            var r = SQLite3_DLL_Handler.Result.OK;
            lock (_conn.SyncObject)
            {
                var stmt = Prepare();
                r = SQLite3_DLL_Handler.Step(stmt);
                Finalize(stmt);
            }

            if (r == SQLite3_DLL_Handler.Result.Done)
            {
                int rowsAffected = SQLite3_DLL_Handler.Changes(_conn.Handle);
                return rowsAffected;
            }

            if (r == SQLite3_DLL_Handler.Result.Error)
            {
                string msg = SQLite3_DLL_Handler.GetErrmsg(_conn.Handle);
                throw SQLiteException.New(r, msg);
            }

            if (r == SQLite3_DLL_Handler.Result.Constraint)
            {
                if (SQLite3_DLL_Handler.ExtendedErrCode(_conn.Handle) == SQLite3_DLL_Handler.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(r, SQLite3_DLL_Handler.GetErrmsg(_conn.Handle));
                }
            }

            throw SQLiteException.New(r, r.ToString());
        }
        public IEnumerable<T> ExecuteDeferredQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T)));
        }

         //Added by DLS
        public List<T> ExecuteQuery<T>(string tablename)
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(tablename, typeof(T))).ToList();
        }

        public List<T> ExecuteQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T))).ToList();
        }

        public List<T> ExecuteQuery<T>(TableMapping map)
        {
            return ExecuteDeferredQuery<T>(map).ToList();
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            if (_conn.Trace) { _conn.InvokeTrace("Executing Query: " + this); }

            lock (_conn.SyncObject)
            {
                var stmt = Prepare();
                try
                {
                    var cols = new TableMapping.Column[SQLite3_DLL_Handler.ColumnCount(stmt)];

                    for (int i = 0; i < cols.Length; i++)
                    {
                        var name = SQLite3_DLL_Handler.ColumnName16(stmt, i);
                        cols[i] = map.FindColumn(name);
                    }

                    while (SQLite3_DLL_Handler.Step(stmt) == SQLite3_DLL_Handler.Result.Row)
                    {
                        var obj = Activator.CreateInstance(map.MappedType);

                        for (int i = 0; i < cols.Length; i++)
                        {
                            if (cols[i] == null)
                            {
                                continue;
                            }

                            var colType = SQLite3_DLL_Handler.ColumnType(stmt, i);
                            var val = ReadCol(stmt, i, colType, cols[i].ColumnType);
                            cols[i].SetValue(obj, val);
                        }

                        OnInstanceCreated(obj);

                        yield return (T)obj;
                    }
                }
                finally
                {
                    SQLite3_DLL_Handler.Finalize(stmt);
                }
            }
        }

        public T ExecuteScalar<T>()
        {
            if (_conn.Trace) { _conn.InvokeTrace("Executing Query: " + this); }

            T val = default(T);

            lock (_conn.SyncObject)
            {
                var stmt = Prepare();

                try
                {
                    var r = SQLite3_DLL_Handler.Step(stmt);
                    if (r == SQLite3_DLL_Handler.Result.Row)
                    {
                        var colType = SQLite3_DLL_Handler.ColumnType(stmt, 0);
                        val = (T)ReadCol(stmt, 0, colType, typeof(T));
                    }
                    else if (r == SQLite3_DLL_Handler.Result.Done)
                    {

                    }
                    else
                    {
                        throw SQLiteException.New(r, SQLite3_DLL_Handler.GetErrmsg(_conn.Handle));
                    }
                }
                finally
                {
                    Finalize(stmt);
                }
            }

            return val;
        }

        public void Bind(string name, object val)
        {
            _bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        public override string ToString()
        {
            var parts = new string[1 + _bindings.Count];
            parts[0] = CommandText;
            var i = 1;

            foreach (var b in _bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }

            return string.Join(Environment.NewLine, parts);
        }

        #endregion //END Region Public Methods

        #region Protected Methods

        /// <summary>
        /// Invoked every time an instance is loaded from the Database.
        /// </summary>
        /// <param name='obj'>
        /// The newly created object.
        /// </param>
        /// <remarks>
        /// This can be overridden in combination with the <see cref="SQLiteConnection.NewCommand"/>
        /// method to hook into the life-cycle of objects.
        ///
        /// Type safety is not possible because MonoTouch does not support virtual generic methods.
        /// </remarks>
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

        #endregion //END Region Protected Methods

        #region Private Methods
        private IntPtr Prepare()
        {
            var stmt = SQLite3_DLL_Handler.Prepare2(_conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }

        private void Finalize(IntPtr stmt)
        {
            SQLite3_DLL_Handler.Finalize(stmt);
        }

        private void BindAll(IntPtr stmt)
        {
            int nextIdx = 1;

            foreach (var b in _bindings)
            {
                if (b.Name != null) { b.Index = SQLite3_DLL_Handler.BindParameterIndex(stmt, b.Name); }
                else { b.Index = nextIdx++; }

                BindParameter(stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks);
            }
        }

        //todo: Add more handling for more Data Types.
        private object ReadCol(IntPtr stmt, int index, SQLite3_DLL_Handler.ColType type, Type clrType)
        {
            if (type == SQLite3_DLL_Handler.ColType.Null) { return null; }

            //Text Handling
            if (clrType == typeof(string)) { return SQLite3_DLL_Handler.ColumnString(stmt, index); }

            //Number Handling
            if (clrType == typeof(int)) { return SQLite3_DLL_Handler.ColumnInt(stmt, index); }
            if (clrType == typeof(float)) { return (float)SQLite3_DLL_Handler.ColumnDouble(stmt, index); }
            if (clrType == typeof(double)) { return SQLite3_DLL_Handler.ColumnDouble(stmt, index); }
            if (clrType == typeof(decimal)) { return (decimal)SQLite3_DLL_Handler.ColumnDouble(stmt, index); }
            //Less Common Types
            if (clrType == typeof(sbyte)) { return (sbyte)SQLite3_DLL_Handler.ColumnInt(stmt, index); }
            if (clrType == typeof(byte)) { return (byte)SQLite3_DLL_Handler.ColumnInt(stmt, index); }
            if (clrType == typeof(short)) { return (short)SQLite3_DLL_Handler.ColumnInt(stmt, index); }
            if (clrType == typeof(ushort)) { return (ushort)SQLite3_DLL_Handler.ColumnInt(stmt, index); }
            if (clrType == typeof(uint)) { return (uint)SQLite3_DLL_Handler.ColumnInt64(stmt, index); }
            if (clrType == typeof(long)) { return SQLite3_DLL_Handler.ColumnInt64(stmt, index); }

            //Bool Handling
//            if (clrType == typeof(bool)) { return SQLite3_DLL_Handler.ColumnInt(stmt, index) == 1; } //todo: Change to string handling.
//            if (clrType == typeof(bool)) { return SQLite3_DLL_Handler.ColumnString(stmt, index); } //test: Test to make sure this works.
            if (clrType == typeof (bool))
            {
                
                bool val = (SQLite3_DLL_Handler.ColumnString(stmt, index) == Convert.ToString(true)) ? true : false;
                return val;
            } //test: Test to make sure this works.

            //DateTime / Time Handling 
            if (clrType == typeof(TimeSpan)) { return new TimeSpan(SQLite3_DLL_Handler.ColumnInt64(stmt, index)); }
            if (clrType == typeof(DateTime))
            {
                if (_conn.StoreDateTimeAsTicks) { return new DateTime(SQLite3_DLL_Handler.ColumnInt64(stmt, index)); }
                var text = SQLite3_DLL_Handler.ColumnString(stmt, index);
                return DateTime.Parse(text);
            }
            if (clrType == typeof(DateTimeOffset)) { return new DateTimeOffset(SQLite3_DLL_Handler.ColumnInt64(stmt, index), TimeSpan.Zero); }

            //Byte Array (File Stream Data and such) Handling
            if (clrType == typeof(byte[])) { return SQLite3_DLL_Handler.ColumnByteArray(stmt, index); }

            //GUID Handling
            if (clrType == typeof(Guid)) { return new Guid(SQLite3_DLL_Handler.ColumnString(stmt, index)); }

            //Enum Handling
            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
//            if (clrType.GetTypeInfo().IsEnum) { return SQLite3_DLL_Handler.ColumnInt(stmt, index); } //todo: Change to string handling.
            if (clrType.GetTypeInfo().IsEnum) { return SQLite3_DLL_Handler.ColumnString(stmt, index); } //test: Test to make sure this works.
            #else
//            if (clrType.IsEnum) { return SQLite3_DLL_Handler.ColumnInt(stmt, index); } //todo: Change to string handling.
//            if (clrType.IsEnum) { return SQLite3_DLL_Handler.ColumnString(stmt, index); }//test: Test to make sure this works.
            if (clrType.IsEnum) { return Enum.Parse(clrType, SQLite3_DLL_Handler.ColumnString(stmt, index)); }//test: Test to make sure this works.
            #endif

            //throws error if no matching types.
            throw new NotSupportedException("Don't know how to read " + clrType);
        }

#endregion //END Region Private Methods

#region Internal Methods

        internal static IntPtr NegativePointer = new IntPtr(-1);

        internal static void BindParameter(IntPtr stmt, int index, object value, bool storeDateTimeAsTicks)
        {
            if (value == null) { SQLite3_DLL_Handler.BindNull(stmt, index); }
            else
            {
                //Text Handling
                if (value is string) { SQLite3_DLL_Handler.BindText(stmt, index, (string)value, -1, NegativePointer); }

                //Number Handling
                else if (value is int) { SQLite3_DLL_Handler.BindInt(stmt, index, (int)value); }
                else if (value is byte || value is ushort || value is sbyte || value is short) { SQLite3_DLL_Handler.BindInt(stmt, index, Convert.ToInt32(value)); }
                else if (value is uint || value is long) { SQLite3_DLL_Handler.BindInt64(stmt, index, Convert.ToInt64(value)); }
                else if (value is float || value is double || value is decimal) { SQLite3_DLL_Handler.BindDouble(stmt, index, Convert.ToDouble(value)); }

                //Bool Handling
                else if (value is bool)
                {
                    UnityEngine.Debug.Log("Convert Bool Value: " + Convert.ToString(value));
                    SQLite3_DLL_Handler.BindText(stmt, index, Convert.ToString(value), -1, NegativePointer);
                } //todo: handle as string values.
//					SQLite3_DLL_Handler.BindInt (stmt, index, (bool)value ? 1 : 0); //OLD
//                  SQLite3_DLL_Handler.BindText(stmt, index, (bool)value ? "TRUE" : "FALSE",-1,NegativePointer); //experimental //test: get value back from db!
                
                //DateTime / Time Handling 
                else if (value is TimeSpan) { SQLite3_DLL_Handler.BindInt64(stmt, index, ((TimeSpan)value).Ticks); }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks) { SQLite3_DLL_Handler.BindInt64(stmt, index, ((DateTime)value).Ticks); }
                    else { SQLite3_DLL_Handler.BindText(stmt, index, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"), -1, NegativePointer); }
                }
                else if (value is DateTimeOffset) { SQLite3_DLL_Handler.BindInt64(stmt, index, ((DateTimeOffset)value).UtcTicks); }

                //Byte Array (File Stream Data and such) Handling
                else if (value is byte[]) { SQLite3_DLL_Handler.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer); }

                //GUID Handling
                else if (value is Guid) { SQLite3_DLL_Handler.BindText(stmt, index, ((Guid)value).ToString(), 72, NegativePointer); }

                //Enum Handling
                #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
                else if (value.Get().GetTypeInfo().IsEnum) { SQLite3_DLL_Handler.BindText(stmt, index, Convert.ToString(value), -1, NegativePointer); } //test: get value back from db!
                #else
                else if (value.GetType().IsEnum) { SQLite3_DLL_Handler.BindText(stmt, index, Convert.ToString(value), -1, NegativePointer); } //test: get value back from db!
//                else if (value.GetType().IsEnum) { SQLite3_DLL_Handler.BindInt(stmt, index, Convert.ToInt32(value)); } //todo:Change to string handling.
                #endif
//              SQLite3_DLL_Handler.BindInt (stmt, index, Convert.ToInt32 (value)); //old

                //Experimental List Handling
                else if (value is List<string>)
                {
                    List<string> value_list = (List<string>)value;
                    string vals = String.Empty;

                    for (int i = 0; i < value_list.Count; i++)
                    {
                        vals += value_list[i];

                        if (i != value_list.Count - 1) { vals += ","; }

                    }

                    SQLite3_DLL_Handler.BindText(stmt, index, vals, -1, NegativePointer);
                }

                //End Experimental List Handling
                else { throw new NotSupportedException("Cannot store type: " + value.GetType()); }
            }
        }

#endregion //END Region Internal Methods

#endregion //End Region Methods

    } //END Class SQLiteCommand

#endregion //END Region Classes

} //END Namespace DLS.SQLiteUnity