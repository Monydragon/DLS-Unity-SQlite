using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DLS.SQLiteUnity
{

    #region Classes

    public class TableMapping
    {

        #region Local Classes

        public class Column
        {
            #region Fields

            #region Private Fields

            private PropertyInfo _prop;

            #endregion //END Region Private Fields

            #endregion //END Region Fields

            #region Properties

            public string Name { get; private set; }

            public string PropertyName { get { return _prop.Name; } }

            public Type ColumnType { get; private set; }

            public string Collation { get; private set; }

            public bool IsAutoInc { get; private set; }
            public bool IsAutoGuid { get; private set; }

            public bool IsPK { get; private set; }
            public IEnumerable<IndexedAttribute> Indices { get; set; }

            public bool IsNullable { get; private set; }

            public int? MaxStringLength { get; private set; }

            #endregion //END Region Properties

            #region Constructors

            public Column(PropertyInfo prop, CreateFlags createFlags = CreateFlags.None)
            {
                var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

                _prop = prop;
                Name = colAttr == null ? prop.Name : colAttr.Name;
                //If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
                ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                Collation = Object_Relational_Mapping.Collation(prop);

                IsPK = Object_Relational_Mapping.IsPK(prop) ||
                       (((createFlags & CreateFlags.ImplicitPK) == CreateFlags.ImplicitPK) &&
                        string.Compare(prop.Name, Object_Relational_Mapping.ImplicitPkName, StringComparison.OrdinalIgnoreCase) == 0);

                var isAuto = Object_Relational_Mapping.IsAutoInc(prop) || (IsPK && ((createFlags & CreateFlags.AutoIncPK) == CreateFlags.AutoIncPK));
                IsAutoGuid = isAuto && ColumnType == typeof(Guid);
                IsAutoInc = isAuto && !IsAutoGuid;

                Indices = Object_Relational_Mapping.GetIndices(prop);
                if (!Indices.Any()
                    && !IsPK
                    && ((createFlags & CreateFlags.ImplicitIndex) == CreateFlags.ImplicitIndex)
                    && Name.EndsWith(Object_Relational_Mapping.ImplicitIndexSuffix, StringComparison.OrdinalIgnoreCase)
                    )
                {
                    Indices = new IndexedAttribute[] { new IndexedAttribute() };
                }
                IsNullable = !(IsPK || Object_Relational_Mapping.IsMarkedNotNull(prop));
                MaxStringLength = Object_Relational_Mapping.MaxStringLength(prop);
            }

            #endregion //END Region Constructors

            #region Methods

            #region Public Methods

            public void SetValue(object obj, object val)
            {
//                var val2 = _prop.GetGetMethod(); //refactor:DEBUG ONLY
                _prop.SetValue(obj, val, null);
            }

            public object GetValue(object obj)
            {
//                var val = _prop.GetGetMethod(); //refactor:DEBUG ONLY
                return _prop.GetGetMethod().Invoke(obj, null);
            }

            #endregion //END Region Public Methods

            #endregion //End Region Methods

        } //END Class Column

        #endregion //END Region Local Classes  

        #region Fields

        #region Private Fields

        private Column _autoPk;
        private Column[] _insertColumns;
        private Column[] _insertOrReplaceColumns;
        private PreparedSqlLiteInsertCommand _insertCommand;
        private string _insertCommandExtra;

        #endregion //END Region Private Fields

        #endregion //END Region Fields

        #region Properties

        public Type MappedType { get; private set; }

        public string TableName { get; private set; }

        public Column[] Columns { get; private set; }

        public Column PK { get; private set; }

        public string GetByPrimaryKeySql { get; private set; }

        public bool HasAutoIncPK { get; private set; }

        public Column[] InsertColumns
        {
            get
            {
                if (_insertColumns == null)
                {
                    _insertColumns = Columns.Where(c => !c.IsAutoInc).ToArray();
                }
                return _insertColumns;
            }
        }

        public Column[] InsertOrReplaceColumns
        {
            get
            {
                if (_insertOrReplaceColumns == null)
                {
                    _insertOrReplaceColumns = Columns.ToArray();
                }
                return _insertOrReplaceColumns;
            }
        }



        #endregion //END Region Properties

        #region Constructors

        public TableMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            MappedType = type;

           
//            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
//			var tableAttr = (TableAttribute)System.Reflection.CustomAttributeExtensions
//				.GetCustomAttribute(type.GetTypeInfo(), typeof(TableAttribute), true);
//            #else
//            var tableAttr = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();
//            #endif
//
//            TableName = tableAttr != null ? tableAttr.Name : MappedType.Name;

           
            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
            			var props = from p in MappedType.GetRuntimeProperties()
						where ((p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic) || (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic))
						select p;
            #else
            var props = MappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            #endif

            var cols = new List<Column>();

            foreach (var p in props)
            {
                var ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Any();
                var is_nested_prop = p.GetCustomAttributes(typeof(NestedPropertyAttribute), true).Any();

                if (p.CanWrite && !ignore)
                {
                    //Added by DLS (EXPERIMENTAL)

                    if (is_nested_prop)
                    {
                        var nested_props = p.PropertyType.GetProperties();

                        for (int i = 0; i < nested_props.Length; i++)
                        {
                            var nested_ignore = nested_props[i].GetCustomAttributes(typeof(IgnoreAttribute), true).Any();

                            if (nested_props[i].CanWrite && !nested_ignore)
                            {
                                cols.Add(new Column(nested_props[i], createFlags));
                            }
                        }
                    }
                    else
                    {
                        cols.Add(new Column(p, createFlags));
                    }
                }
            }

            Columns = cols.ToArray();

            foreach (var c in Columns)
            {
                if (c.IsAutoInc && c.IsPK) { _autoPk = c; }
                if (c.IsPK) { PK = c; }
            }

            HasAutoIncPK = _autoPk != null;

            if (PK != null) { GetByPrimaryKeySql = string.Format("select * from \"{0}\" where \"{1}\" = ?", TableName, PK.Name); }

            // People should not be calling Get/Find without a PK
            else { GetByPrimaryKeySql = string.Format("select * from \"{0}\" limit 1", TableName); }
        }

        //Added by DLS
        public TableMapping(string tablename, Type type, CreateFlags createFlags = CreateFlags.None)
        {
            MappedType = type;

           
//            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
//			var tableAttr = (TableAttribute)System.Reflection.CustomAttributeExtensions
//				.GetCustomAttribute(type.GetTypeInfo(), typeof(TableAttribute), true);
//            #else
//            var tableAttr = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();
//            #endif

            TableName = tablename;


            #if NETFX_CORE
            var props = from p in MappedType.GetRuntimeProperties()
                        where ((p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic) || (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic))
                        select p;
            #else
            var props = MappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            #endif

            var cols = new List<Column>();

            foreach (var p in props)
            {
                var ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Any();
                var is_nested_prop = p.GetCustomAttributes(typeof(NestedPropertyAttribute), true).Any();

                if (p.CanWrite && !ignore)
                {
                    //Added by DLS (EXPERIMENTAL)

                    if (is_nested_prop)
                    {
                        var nested_props = p.PropertyType.GetProperties();
                        for (int i = 0; i < nested_props.Length; i++)
                        {
                            var nested_ignore = nested_props[i].GetCustomAttributes(typeof(IgnoreAttribute), true).Any();

                            if (nested_props[i].CanWrite && !nested_ignore)
                            {
                                cols.Add(new Column(nested_props[i], createFlags));
                            }
                        }

                    }
                    //END ADD BY DLS
                    else
                    {
                        cols.Add(new Column(p, createFlags));
                    }
                }
            }

            Columns = cols.ToArray();

            foreach (var c in Columns)
            {
                if (c.IsAutoInc && c.IsPK) { _autoPk = c; }
                if (c.IsPK) { PK = c; }
            }

            HasAutoIncPK = _autoPk != null;

            if (PK != null) { GetByPrimaryKeySql = string.Format("select * from \"{0}\" where \"{1}\" = ?", TableName, PK.Name); }
            // People should not be calling Get/Find without a PK
            else { GetByPrimaryKeySql = string.Format("select * from \"{0}\" limit 1", TableName); }
        }

        #endregion //END Region Constructors

        #region Methods

        #region Public Methods

        public void SetAutoIncPK (object obj, long id)
        {
            if (_autoPk != null) { _autoPk.SetValue(obj, Convert.ChangeType(id, _autoPk.ColumnType, null)); }
        }

        public Column FindColumnWithPropertyName (string propertyName)
        {
            var exact = Columns.FirstOrDefault (c => c.PropertyName == propertyName);
            return exact;
        }

        public Column FindColumn (string columnName)
        {
            var exact = Columns.FirstOrDefault (c => c.Name == columnName);
            return exact;
        }

        public PreparedSqlLiteInsertCommand GetInsertCommand(SQLiteConnection conn, string extra)
        {
            if (_insertCommand == null)
            {
                _insertCommand = CreateInsertCommand(conn, extra);
                _insertCommandExtra = extra;
            }
            else if (_insertCommandExtra != extra)
            {
                _insertCommand.Dispose();
                _insertCommand = CreateInsertCommand(conn, extra);
                _insertCommandExtra = extra;
            }
            return _insertCommand;
        }

        #endregion //END Region Public Methods

        #region Protected Methods

        protected internal void Dispose()
        {
            if (_insertCommand != null)
            {
                _insertCommand.Dispose();
                _insertCommand = null;
            }
        }

        #endregion //END Region Protected Methods

        #region Private Methods

        private PreparedSqlLiteInsertCommand CreateInsertCommand(SQLiteConnection conn, string extra)
        {
            var cols = InsertColumns;
            string insertSql;

            if (!cols.Any() && Columns.Count() == 1 && Columns[0].IsAutoInc) { insertSql = string.Format("insert {0} into \"{1}\" default values", extra, TableName); }
            else
            {
                var replacing = string.Compare (extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

                if (replacing) { cols = InsertOrReplaceColumns; }

                insertSql = string.Format("insert {3} into \"{0}\"({1}) values ({2})", TableName,
                    string.Join(",", (from c in cols
                        select "\"" + c.Name + "\"").ToArray()),
                    string.Join(",", (from c in cols
                        select "?").ToArray()), extra);
            }

            var insertCommand = new PreparedSqlLiteInsertCommand(conn) {CommandText = insertSql};
            return insertCommand;
        }

        #endregion //END Region Private Methods

        #endregion //End Region Methods

    } //END Class TableMapping

    #endregion // END Region Classes

} //END Namespace DLS.SQLiteUnity