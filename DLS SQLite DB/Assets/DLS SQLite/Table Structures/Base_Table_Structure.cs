using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLS.SQLiteUnity
{
    [System.Serializable]
    public class Base_Table_Structure : I_DB_Table
    {
        protected string _table_name;
        protected Dictionary<string, I_DB_Field> _fields = new Dictionary<string, I_DB_Field>();
        protected SQLDatabase _database = new SQLDatabase();

        public string Name { get { return _table_name; } private set { _table_name = value; } }
        public Dictionary<string, I_DB_Field> Fields { get { return _fields; } private set { _fields = value; } }

        public SQLDatabase Database { get { return _database; } private set { _database = value; } }

        public Base_Table_Structure()
        {
            
        }

        public Base_Table_Structure(string tablename, SQLDatabase database)
        {
            _table_name = tablename;
            _database = database;
        }

        public Base_Table_Structure(string tablename, string database_name, string database_root_path = "DEFAULT", string database_file_extension = ".db", bool create_database = true)
        {
            _table_name = tablename;
            if (create_database)
            {
                _database.CreateTable(tablename, database_name, database_root_path, database_file_extension);
            }
            else
            {
                _database.OpenDatabase(database_name, database_root_path, database_file_extension);
            }

            if (!_database.Tables.ContainsKey(tablename))
            {
                _database.Tables.Add(tablename, this);
            }
        }

        public bool RenameTable(string name)
        {
            _database.OpenDatabase(_database.Database_Name,_database.Database_Root_Path,_database.Database_File_Extension);
            var field = _database.Tables[_table_name];
            _database.Tables.Remove(_table_name);
            var q = "ALTER TABLE " + _table_name + " RENAME TO " + name;
            _database.Connection.Execute(q);
            _database.Tables.Add(name,field);
            _database.CloseDatabase();
            return true;
        }

        public bool AddField(I_DB_Data data)
        {
            if (Fields.ContainsKey(data.Name))
            {
                return false;
            }
            var field = new Base_Field_Structure(data);
            _fields.Add(data.Name,field);
            return _database.AddFieldWithData(_table_name, data, field);
        }
        public bool AddField(I_DB_Data data, I_DB_Field row_field)
        {
            if (Fields.ContainsKey(data.Name))
            {
                return false;
            }
            row_field.AddData(data);
            _fields.Add(data.Name, row_field);
            return _database.AddFieldWithData(_table_name, data);
        }

        public bool DeleteField(I_DB_Data data)
        {
            if (!Fields.ContainsKey(data.Name))
            {
                return false;
            }
            _fields.Remove(data.Name);
            return _database.DeleteField(_table_name,data);
        }

        public bool DeleteField(int id)
        {

            var field = _fields.First(x => x.Value.ID == id);
            if (!Fields.ContainsKey(field.Key))
            {
                return false;
            }
            _fields.Remove(field.Key);
            return _database.DeleteField(_table_name, field.Value.ID);
        }

        public bool DeleteField(string field_name)
        {

            if (!Fields.ContainsKey(field_name))
            {
                return false;
            }
            _fields.Remove(field_name);
            return _database.DeleteField(_table_name,field_name);
        }

        public T GetField<T>(string field_name) where T : I_DB_Field
        {
            return _database.GetField<T>(_table_name, field_name);
        }

        public T GetField<T,D>(D data, T field_type) where T : I_DB_Field where D : I_DB_Data
        {
            return _database.GetField(_table_name,data.Name,field_type);
        }

        public T GetData<T>(T data) where T : I_DB_Data
        {
            return _database.GetData<T>(_table_name,data.Name);
        }

    }
}