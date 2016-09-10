using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DLS.SQLiteUnity
{
    /// <summary>
    /// This is the SQL Database. You pass in a Database Name to Create/Load you can pass open flags accordingly to open/create/read the database.
    /// </summary>
    [System.Serializable]
    public class SQLDatabase
    {
        //Handles the Database Properties.
        #region Variables & Properties 

        protected SQLiteConnection _Connection;
        protected SQLiteOpenFlags _Database_Flags = SQLiteOpenFlags.ReadWrite;
        [SerializeField]
        protected string _Database_Name;
        protected string _Database_Root_Path;
        protected string _Database_File_Extension = ".db";
        protected string _Database_Full_Path;
        //Read only should prevent the dictionary from being reinitialized
        protected readonly Dictionary<string, I_DB_Table> _Tables = new Dictionary<string, I_DB_Table>();
        /// <summary>
        /// The Database Connection Handler (<see cref="SQLiteConnection"/>)
        /// </summary>
        public SQLiteConnection Connection { get { return _Connection; } private set { _Connection = value; } }

        /// <summary>
        /// These Database Flags are used to indicate the current database connection mode.
        /// </summary>
        public SQLiteOpenFlags Database_Flags { get { return _Database_Flags; } set { _Database_Flags = value; } }

        /// <summary>
        /// Name of the Database.
        /// </summary>
        public string Database_Name { get { return _Database_Name; } private set { _Database_Name = value; } }

        /// <summary>
        /// The Root path for the Database to be Save/Loaded from.
        /// </summary>
        public string Database_Root_Path { get { return _Database_Root_Path; } private set { _Database_Root_Path = value; } }

        /// <summary>
        /// The Full path including the file extension to the Database file.
        /// </summary>
        public string Database_Full_Path { get { return _Database_Full_Path; } private set { _Database_Full_Path = value; } }

        /// <summary>
        /// The Full path including the file extension to the Database file.
        /// </summary>
        public string Database_File_Extension { get { return _Database_File_Extension; } private set { _Database_File_Extension = value; } }

        /// <summary>
        /// Dictionary of tables.
        /// </summary>
        public Dictionary<string, I_DB_Table> Tables { get { return _Tables; } }

        /// <summary>
        /// Gets the int type amount of all the tables in the Database.
        /// </summary>
        public int TableCount
        {
            get { return Connection.TableMappings.Count(); }
        }

        #endregion // End Region Variables & Properties.

        // Handles the Constructors. These Constructors Create the Connection to the Database.
        // If Specific Options are Assigned it will also create the database or just read it.
        // should work on all supported platforms. Windows/Mac/Linux/Android/IOS/WindowsStore (possibly others as well?)
        #region Constructors

        /// <summary>
        /// Default Constructor does NOT prepare the Database or create it.
        /// </summary>
        public SQLDatabase()
        {

        }

        /// <summary>
        /// Will Connect to the Database provided and if <see cref="_create_database"/> is true it will create the database. 
        /// </summary>
        /// <param name="_database_name">The name of the Database</param>
        /// <param name="_create_database">When Enabled will create the database. otherwise it will only open</param>
        public SQLDatabase(string _database_name, bool _create_database = true, SQLiteOpenFlags _flags = SQLiteOpenFlags.ReadWrite)
        {
            PrepareDatabase(_database_name, _flags);
            if (_create_database)
            {
                CreateDatabase(_database_name);
            }
            else
            {
                OpenDatabase(_database_name);
            }
        }
        /// <summary>
        /// Will Connect to the database provided. Defaults will set the Database Root Path to <see cref="_Database_Root_Path"/> and the default file extension defined <see cref="Database_File_Extension"/>
        /// </summary>
        /// <param name="_database_name">The name of the Database to Connect to or Create</param>
        /// <param name="_database_root_path">The Root Folder path where the database will be stored.</param>
        /// <param name="_database_file_extension">The File Extension for the Database.</param>
        /// <param name="_database_flags">These flags are used for how the database is accessed. Read, Read/Write, Write etc..</param>
        /// <param name="_create_database">Toggles whether a database is created on new instance.</param>
        //Test: Debug this to make sure it's working.
        public SQLDatabase(string _database_name, string _database_root_path = "DEFAULT", string _database_file_extension = ".db", SQLiteOpenFlags _database_flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, bool _create_database = true)
        {
            PrepareDatabase(_database_name, _database_root_path, _database_file_extension, _database_flags);
            if (_create_database)
            {
                CreateDatabase(_database_name, _database_root_path, _database_file_extension);
            }
            else
            {
                OpenDatabase(_database_name, _database_root_path, _database_file_extension, _database_flags);
            }

        }

        #endregion // End Region Constructors.

        //Handles Creating/Modifying/Deleting Databases
        #region Database Handling

        //Handles setting all the protected variables for the database name, path, location, extension, and open flags.
        #region Prepare Database


        /// <summary>
        /// This will prep the database and set the defaults for <see cref="_Database_Root_Path"/>, <see cref="_Database_File_Extension"/>
        /// </summary>
        /// <param name="_name">The name of the Database.</param>
        /// <param name="_flags">The Read/Write Accessibility for <see cref="SQLiteOpenFlags"/></param>
        public bool PrepareDatabase(string _name, SQLiteOpenFlags _flags)
        {
            if (_name.Length <= 0) { return false; }
            _Database_Name = _name.NoSpecialCharacters();
            _Database_Flags = _flags;
            _Database_Full_Path = Database_Root_Path + Path.DirectorySeparatorChar + _Database_Name + Database_File_Extension;
            return true;
        }

        /// <summary>
        /// This will prep the database and set everything manually.
        /// </summary>
        /// <param name="_name">The name of the database.</param>
        /// <param name="_root_path">The Root folder for the database.</param>
        /// <param name="_extension">The file extension for the database</param>
        /// <param name="_flags"></param>
        public bool PrepareDatabase(string _name, string _root_path = "DEFAULT", string _extension = ".db", SQLiteOpenFlags _flags = SQLiteOpenFlags.ReadWrite)
        {
            if (_name.Length <= 0) { return false; }
            _Database_Name = _name.NoSpecialCharacters(); // TODO: Make sure to Include Text Extension Methods in library.
            _Database_Root_Path = (_root_path == "DEFAULT") ? _Database_Root_Path = Application.streamingAssetsPath : _root_path;
            _Database_File_Extension = (_extension != "db") ? _extension : _Database_File_Extension;
            _Database_Flags = _flags;
            _Database_Full_Path = Database_Root_Path + Path.DirectorySeparatorChar + _Database_Name + Database_File_Extension;
            return true;
        }

        #endregion //End Region Prepare Database

        #region Create Database

        public bool CreateDatabase(string _database_name, string _path = "DEFAULT", string _extension = ".db")
        {
            try
            { //Try Block Started
                if (PrepareDatabase(_database_name, _path, _extension) == false)
                {
                    Debug.LogError("Unable to Create database Prepare Database failed. Because the prepare failed. Please make sure the Database has a Valid Name, Path, Extension.");
                    return false;
                }
                Debug.Log("DB Full path: " + Database_Full_Path);


                if (!Directory.Exists(Database_Root_Path))
                {
                    Directory.CreateDirectory(Database_Root_Path);
                    Debug.Log("Created Directory: " + _Database_Root_Path);
                }

                if (!File.Exists(Database_Full_Path))
                {
#if UNITY_ANDROID
                        var loadDb = new WWW($"jar:file://{_Database_Full_Path}");
                        while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
                                                    // then save to Application.persistentDataPath
                        File.WriteAllBytes(_Database_Full_Path, loadDb.bytes);
#else
                    File.Create(_Database_Full_Path).Close();
                    string log = string.Format("The Database: {0} created at {1}", _Database_Name, _Database_Full_Path);
                    Debug.Log(log);
#endif
                }

                //Tries to establish a connection the the database
            } // Try Block Ended

            catch (System.Exception e)
            {
                string log = string.Format("The Database: {0} created at {1} EXCEPTION:\n\n {2}", _Database_Name, _Database_Full_Path, e);
                Debug.LogError(log);
                return false;
            }

            return true;
        }


        #endregion //End Region Create Database

        #region Open Database

        /// <summary>
        /// Opens the Database to begin database interactions.
        /// </summary>
        /// <param name="_database_name">The name of the Databse</param>
        /// <param name="_path">The Root Path of the Database</param>
        /// <param name="_extension">The File Extension of the Database</param>
        /// <param name="_flags">The Read/Write Flags</param>
        /// <returns>Returns True if Successfully opened.</returns>
        public bool OpenDatabase(string _database_name, string _path = "DEFAULT", string _extension = ".db", SQLiteOpenFlags _flags = SQLiteOpenFlags.ReadWrite)
        {
            try
            { //Try Block Started
                if (PrepareDatabase(_database_name, _path, _extension, _flags) == false)
                {
                    Debug.LogError("Unable to Open database Prepare Database failed. Because the prepare failed. Please make sure the Database has a Valid Name, Path, Extension.");
                    return false;
                }
                Debug.Log("Trying to Open the Database: " + _Database_Name);
                //Tries to establish a connection the the database
                Connection = new SQLiteConnection(_Database_Full_Path, _Database_Flags);
                if (Connection != null)
                {
                    Debug.Log("Connection Successful: " + _database_name + " at " + _Database_Full_Path);
                }
                else
                {
                    Debug.LogError("Failed to access the Database:" + Database_Name + " at " + Database_Full_Path);
                }
            } // Try Block Ended

            catch (System.Exception e)
            {
                Debug.LogError("Failed to access the Database: " + Database_Name + " at " + Database_Full_Path + " EXCEPTION:\n\n " + e);
                return false;
            }

            return true;
        }

        #endregion //End Region Open Database

        #region Close Database

        /// <summary>
        /// Closes the currently opened database.
        /// </summary>
        /// <returns>Returns true if database is successfully closed.</returns>
        public bool CloseDatabase()
        {
            try
            { //Try Block Started
                Connection.Close();
            } // Try Block Ended

            catch (System.Exception e)
            {
                Debug.LogError("Failed to close the Database\n\n" + e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Closes the currently opened database.
        /// </summary>
        /// <returns>Returns true if database is successfully closed.</returns>
        public bool CloseDatabase(string _database_name, string _path = "DEFAULT", string _extension = ".db")
        {
            OpenDatabase(_database_name, _path, _extension);
            try
            { //Try Block Started
                Connection.Close();
            } // Try Block Ended

            catch (System.Exception e)
            {
                Debug.LogError("Failed to close the Database\n\n" + e);
                return false;
            }

            return true;
        }

        #endregion

        #region Delete Database

        public bool DeleteDatabase(string _name, string _root_path = "DEFAULT", string _extension = ".db")
        {
            try
            { //Try Block Started
                PrepareDatabase(_name, _root_path, _extension);
                File.Delete(_Database_Full_Path);

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            } // Try Block Ended

            catch (System.Exception e)
            {
                Debug.LogError("Failed to Delete the Database\n\n" + e);
                return false;
            }

            return true;
        }


        #endregion // End Region Delete Database

        #endregion //End Region Database Handling

        //Handles Database Tables and Assessors.
        #region Table Handling

        //Methods handle getting table information such as Index Id's and other information.
        #region Table Info

        /// <summary>
        /// get the last primary key ID. Which should always return the Last ID value. on the specified type and table name provided by (<see cref="_tablename"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for getting the type of the table field. </param>
        /// <returns>Returns the value of last row ID or the row count.</returns>
        //Test: Test this to make sure it's working.
        //        public int GetPKIndex<T,D>(D object_data, T field_type) where T : I_DB_Field where D : I_DB_Data
        //        {
        //            OpenDatabase(_Database_Name);
        //            var table_name = object_data.GetType().Name;
        //            var table = Connection.Table(table_name, field_type);
        //            AddField(object_data);
        //            DeleteField(object_data);
        ////            table.Connection.Insert(object_data.GetType().Name, field_type);
        ////            table.Connection.Delete(object_data.GetType().Name, field_type);
        //            var PK_count = table.Count();
        //            int index = Connection.GetPkIndex();
        //            Debug.Log($"Table Name: {table_name} Row Count: {table.Count()} PK Count: {PK_count} Last Index: {Connection.GetPkIndex() }");
        ////            int index = (Connection.GetPkIndex() < PK_count) ? PK_count : Connection.GetPkIndex();
        //            Debug.Log($"Fixed Index: {index}");
        //            CloseDatabase(); //TEST: Test this.
        //            return index;
        //        }

        #endregion //End Region Table Info

        #region Create Table

        public bool CreateTable(string tablename, string _database_name, string _path = "DEFAULT", string _extension = ".db")
        {
            try
            {
                if (CreateDatabase(_database_name, _path, _extension))
                {
                    OpenDatabase(_database_name, _path, _extension);
                    if (_Connection == null)
                    {
                        Debug.LogError("Connection is NULL");
                        return false;
                    }
                    CreateTable(tablename, this);
                    CloseDatabase();
                }

            }

            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Table\n\n" + e);
                return false;
            }

            return true;
        }


        /// <summary>
        /// This will create a table with the specified name provided by (<see cref="_tablename"/>) with the default field structure: (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <returns>Returns true if the Table is successfully created!</returns>
        //Test: Test this to make sure it's working. (Completed Tests: Windows Editor Environment.)
        public bool CreateTable(string _tablename, SQLDatabase database)
        {
            try
            {
                var table = new Base_Table_Structure(_tablename, database);

                if (!_Tables.ContainsKey(_tablename))
                {
                    _Tables.Add(_tablename, table);
                }

                OpenDatabase(_Database_Name, _Database_Root_Path, _Database_File_Extension);
                if (_Connection == null)
                {
                    Debug.LogError("Connection is NULL");

                    return false;
                }
                _Connection.CreateTable<Base_Field_Structure>(_tablename);
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                //                CloseDatabase();
                Debug.LogError("Failed to Create the Table\n\n" + e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will create a table with the specified name provided by (<see cref="_tablename"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. defaults to <see cref="Base_Field_Structure"/> if null. </param>
        /// <returns>Returns true if the Table is successfully created!</returns>
        //Test: Test this to make sure it's working. (Completed Tests: Windows Editor Environment.)
        public bool CreateTable<T>(string _tablename, T _field_structure) where T : I_DB_Field
        {
            try
            {
                OpenDatabase(_Database_Name);
                if (!_Tables.ContainsKey(_tablename))
                {
                    _Tables.Add(_tablename, new Base_Table_Structure(_tablename, this));
                }

                if (_field_structure == null)
                {
                    Connection.CreateTable<T>(_tablename);
                }
                else
                {
                    Connection.CreateTable<T>(_tablename);
                }
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                Debug.LogError("Failed to Create the Table\n\n" + e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will create a table with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_database_data">Uses the Database Data to get the name of the table</param>
        /// <returns>Returns true if the Table is successfully created</returns>
        //Test: Test this to make sure it's working. (Completed Tests: Windows Editor Environment.)
        public bool CreateTable<T>(T _database_data) where T : I_DB_Data
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                var table = new Base_Table_Structure(_tablename, this);
                //                OpenDatabase(_Database_Name);
                if (!_Tables.ContainsKey(_tablename))
                {
                    _Tables.Add(_tablename, table);
                }
                Connection.CreateTable<Base_Field_Structure>(_tablename);
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                Debug.LogError("Failed to Create the Table\n\n" + e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will create a table with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_database_data">Uses the Database Data to get the name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. defaults to <see cref="Base_Field_Structure"/> if null. </param>
        /// <returns>Returns true if the Table is successfully replaced/created</returns>
        //Test: Test this to make sure it's working. (Completed Tests: Windows Editor Environment.)
        public bool CreateTable<D, T>(D _database_data, T _field_structure) where D : I_DB_Data where T : I_DB_Field
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                if (!_Tables.ContainsKey(_tablename))
                {
                    _Tables.Add(_tablename, new Base_Table_Structure(_tablename, this));
                }
                if (_field_structure == null) { Connection.CreateTable<Base_Field_Structure>(_tablename); }
                else { Connection.CreateTable<T>(_tablename); }
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                string log = string.Format("Failed to Create the Table:{0} using the {1}  Type. \n Stacktrace: \n{2}", _tablename, _field_structure.GetType().Name, e);
                Debug.LogError(log);
                return false;
            }
            return true;
        }

        #endregion // End Region Create Table

        #region Replace Table

        /// <summary>
        /// This will Replace or Create a table with the specified name provided by (<see cref="_tablename"/>) with the default field structure: (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <returns>Returns true if the Table is successfully created!</returns>
        //Test: Test this to make sure it's working.
        public bool ReplaceTable(string _tablename)
        {
            try
            {
                OpenDatabase(_Database_Name);
                Connection.DropTable<Base_Field_Structure>(_tablename);
                Connection.CreateTable<Base_Field_Structure>(_tablename);
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                string log = string.Format("Failed to Create/Replace the Table:{0} using the {1}  Type. \n Stacktrace: \n{2}", _tablename, typeof(Base_Field_Structure).Name.GetType().Name, e);
                Debug.LogError(log);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will Replace or Create a table with the specified name provided by (<see cref="_tablename"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) the table field type. defaults to <see cref="Base_Field_Structure"/> if null. </param>
        /// <returns>Returns true if the Table is successfully Replaced/Created!</returns>
        //Test: Test this to make sure it's working.
        public bool ReplaceTable<T>(string _tablename, T _field_structure) where T : I_DB_Field
        {
            try
            {
                OpenDatabase(_Database_Name);
                if (_field_structure == null)
                {
                    Connection.DropTable<Base_Field_Structure>(_tablename);
                    Connection.CreateTable<Base_Field_Structure>(_tablename);
                }
                else
                {
                    Connection.DropTable<T>(_tablename);
                    Connection.CreateTable<T>(_tablename);
                }
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                string log = string.Format("Failed to Replace/Create the Table:{0} using the {1}  Type. \n Stacktrace: \n{2}", _tablename, _field_structure.GetType().Name, e);
                Debug.LogError(log);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will Replace or Create a table with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_database_data">Uses the Database Data to get the name of the table</param>
        /// <returns>Returns true if the Table is successfully created</returns>
        //Test: Test this to make sure it's working.
        public bool ReplaceTable<T>(T _database_data) where T : I_DB_Data
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                Connection.DropTable<Base_Field_Structure>(_tablename);
                Connection.CreateTable<Base_Field_Structure>(_tablename);
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                string log = string.Format("Failed to Replace/Create the Table:{0} using the {1}  Type. \n Stacktrace: \n{2}", _tablename, typeof(Base_Field_Structure).Name.GetType().Name, e);
                Debug.LogError(log);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will create a table with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_database_data">Uses the Database Data to get the name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. defaults to <see cref="Base_Field_Structure"/> if null. </param>
        /// <returns>Returns true if the Table is successfully replaced/created</returns>
        //Test: Test this to make sure it's working.
        public bool ReplaceTable<D, T>(D _database_data, T _field_structure) where D : I_DB_Data where T : I_DB_Field
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                if (_field_structure == null)
                {
                    Connection.DropTable<Base_Field_Structure>(_tablename);
                    Connection.CreateTable<Base_Field_Structure>(_tablename);
                }
                else
                {
                    Connection.DropTable<T>(_tablename);
                    Connection.CreateTable<T>(_tablename);
                }
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                string log = string.Format("Failed to Create/Replace the Table:{0} using the {1}  Type. \n Stacktrace: \n{2}", _tablename, _field_structure.GetType().Name, e);
                Debug.LogError(log);
                return false;
            }
            return true;
        }

        #endregion //End Region Replace Table

        #region Delete Table

        /// <summary>
        /// This will Delete the table with the specified name provided by (<see cref="_tablename"/>) with the default field structure: (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <returns>Returns true if the Table is successfully Deleted!</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteTable(string _tablename)
        {
            try
            {
                OpenDatabase(_Database_Name);
                Connection.DropTable<Base_Field_Structure>(_tablename);
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                Debug.LogError(" Failed Operation:\n\n\n" + e);

                //                string log = string.Format("Failed to Delete the Table:{0} using the {1}  Type. \n Stacktrace: \n{2}", _tablename, _field_structure.GetType().Name, e);
                //                Debug.LogError(log);
                //                Debug.LogError($"Failed to Delete the Table:{_tablename} using the {nameof(Base_Field_Structure)}  Type. \n Stacktrace: \n{e}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will Delete the table with the specified name provided by (<see cref="_tablename"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <param name="_tablename">The name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. defaults to <see cref="Base_Field_Structure"/> if null. </param>
        /// <returns>Returns true if the Table is successfully Deleted!</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteTable<T>(string _tablename, T _field_structure) where T : I_DB_Field
        {
            try
            {
                OpenDatabase(_Database_Name);
                if (_field_structure == null) { Connection.DropTable<Base_Field_Structure>(_tablename); }
                else { Connection.DropTable<T>(_tablename); }
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                Debug.LogError("Failed to Delete the Table\n\n" + e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will Delete the table with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_database_data">Uses the Database Data to get the name of the table</param>
        /// <returns>Returns true if the Table is successfully Deleted!</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteTable<T>(T _database_data) where T : I_DB_Data
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                Connection.DropTable<Base_Field_Structure>(_tablename);
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                Debug.LogError("Failed to Delete the Table\n\n" + e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This will Delete the table with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_database_data">Uses the Database Data to get the name of the table</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. defaults to <see cref="Base_Field_Structure"/> if null. </param>
        /// <returns>Returns true if the Table is successfully Deleted!</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteTable<D, T>(D _database_data, T _field_structure) where D : I_DB_Data where T : I_DB_Field
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                if (_field_structure == null) { Connection.DropTable<Base_Field_Structure>(_tablename); }
                else { Connection.DropTable<T>(_tablename); }
                CloseDatabase();
            }
            catch (System.Exception e)
            {
                CloseDatabase();
                Debug.LogError("Failed to Delete the Table\n\n" + e);
                return false;
            }
            return true;
        }


        #endregion // End Region Delete Table

        #endregion //END Region Table Handling

        //Handles Database Rows (Fields)
        #region Field Handling (Rows)

        #region Add Field

        //        /// <summary>
        //        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        //        /// </summary>
        //        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        //        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        //        /// <returns>Returns true if the field is successfully Added.</returns>
        //        //Test: Test this to make sure it's working.
        //        public bool AddField<T>(T _database_) where T : I_DB_Field
        //        {
        //            var _tablename = _database_data.GetType().Name;
        //            try
        //            {
        //                OpenDatabase(_Database_Name);
        //                Connection.Insert(_tablename, field);
        //            }
        //            catch (System.Exception e)
        //            {
        //                Debug.LogError("Failed to Create the Field\n\n" + e);
        //                return false;
        //            }
        //            CloseDatabase();
        //            return true;
        //        }


        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public bool AddFieldWithData<T>(T _database_data) where T : I_DB_Data
        {
            var _tablename = _database_data.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                var field = new Base_Field_Structure(_database_data);
                Connection.Insert(_tablename, field);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// Adds field with the specified name and field class.
        /// </summary>
        /// <param name="_tablename"></param>
        /// <param name="_database_field"></param>
        /// <returns></returns>
        //Test: Test this to make sure it's working.
        public bool AddField(string _tablename, object _database_field)
        {
           var name = _database_field.GetType().GetProperty("Name").GetGetMethod().Invoke(_database_field,null);
            try
            {
                OpenDatabase(_Database_Name);
                Connection.Insert(_tablename, _database_field);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Field: " + name +" TYPE: " + _database_field.GetType().Name + " \n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public bool AddFieldWithData<T>(string _tablename, T _database_data) where T : I_DB_Data
        {
            try
            {
                OpenDatabase(_Database_Name);
                var field = new Base_Field_Structure(_database_data);
                Connection.Insert(_tablename, field);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public bool AddFieldWithData<T, F>(string _tablename, T _database_data, F _field) where T : I_DB_Data where F : I_DB_Field
        {
//            var _fieldtype = _field.GetType().Name;
            try
            {
                _field.AddData(_database_data);
                OpenDatabase(_Database_Name);
                //                var field = new Base_Field_Structure(_database_data);
                Connection.Insert(_tablename, _field);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        /// <returns>Returns true if the Field is successfully Added</returns>
        //Test: Test this to make sure it's working.
        public bool AddFieldWithData<T, F>(T _database_data, F _field_structure) where T : I_DB_Data where F : I_DB_Field
        {
            var _tablename = _database_data.GetType().Name;
//            var _fieldtype = _field_structure.GetType().Name;
            try
            {
                OpenDatabase(_Database_Name);
                //                _database_data.ID = Connection.SetPkID(_database_data, _field_structure) + 1;
                _field_structure.Name = _database_data.Name;
                _field_structure.SaveData(_database_data);
                Connection.Insert(_tablename, _field_structure);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        #endregion //END Region Add Field

        #region Update Field

        /// <summary>
        /// This will Update a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Updated.</returns>
        //Test: Test this to make sure it's working.
        public bool UpdateField<T>(T _database_data) where T : I_DB_Data
        {
            var _tablename = _database_data.GetType().Name;
            var field = new Base_Field_Structure(_database_data);
            field.Name = _database_data.Name;
            try
            {
                OpenDatabase(_Database_Name);
                Connection.Update(_tablename, field);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Create the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// This will Update a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        /// <returns>Returns true if the Field is successfully Updated</returns>
        //Test: Test this to make sure it's working.
        public bool UpdateField<T, F>(T _database_data, F _field_structure) where T : I_DB_Data where F : I_DB_Field
        {
            var _tablename = _database_data.GetType().Name;
//            var _fieldtype = _field_structure.GetType().Name;
            _field_structure.Name = _database_data.Name;
            try
            {
                OpenDatabase(_Database_Name);
                //                _database_data.ID = GetPKIndex(_database_data, _field_structure) + 1;
                _field_structure.Name = _database_data.Name;
                _field_structure.SaveData(_database_data);
                Connection.Update(_tablename, _field_structure);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Update the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// Updates all the Rows (Fields) with the field structure provided.
        /// </summary>
        /// <typeparam name="T">The Database Data that used <see cref="I_DB_Data"/></typeparam>
        /// <typeparam name="F">An IEnumerable of the type <see cref="I_DB_Field"/> </typeparam>
        /// <param name="_database_data">The Database Data</param>
        /// <param name="_field_structures">The Field Structure IEnumerable collection.</param>
        /// <returns>Returns true if all fields were successfully updated.</returns>
        //Test: Test this to make sure it's working. UNTESTED?
        public bool UpdateAllFields<T>(T _database_data) where T : I_DB_Data
        {
            var _field = new Base_Field_Structure();
//            var _fieldtype = _database_data.GetType().Name;
//            int count = 0;
            try
            {
                OpenDatabase(_Database_Name);
                Connection.UpdateAllFields(_database_data, _field);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Update the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// Updates all the Rows (Fields) with the field structure provided.
        /// </summary>
        /// <typeparam name="T">The Database Data that used <see cref="I_DB_Data"/></typeparam>
        /// <typeparam name="F">An IEnumerable of the type <see cref="I_DB_Field"/> </typeparam>
        /// <param name="_database_data">The Database Data</param>
        /// <param name="_field_structures">The Field Structure IEnumerable collection.</param>
        /// <returns>Returns true if all fields were successfully updated.</returns>
        //Test: Test this to make sure it's working. UNTESTED?
        public bool UpdateAllFields<T, F>(T _database_data, F _field_structure) where T : I_DB_Data where F : I_DB_Field
        {
//            var _tablename = _database_data.GetType().Name;
//            var _fieldtype = _field_structure.GetType().Name;
            int count = 0;
            try
            {
                OpenDatabase(_Database_Name);
                count = Connection.UpdateAllFields(_database_data, _field_structure);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Update: " + count + " Fields\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        #endregion //END Region Update Field

        #region Delete Field

        //        /// <summary>
        //        /// This will Delete a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        //        /// </summary>
        //        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        //        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        //        /// <returns>Returns true if the field is successfully Deleted.</returns>
        //        //Test: Test this to make sure it's working.
        //        public bool DeleteField<T>(T _database_data) where T : I_DB_Data
        //        {
        //            var _tablename = _database_data.GetType().Name;
        //            var _fieldtype = nameof(Base_Field_Structure);
        //            var field = new Base_Field_Structure(_database_data);
        //            try
        //            {
        //                OpenDatabase(_Database_Name);
        //                Connection.Delete(_tablename, field);
        //            }
        //            catch (System.Exception e)
        //            {
        //                Debug.LogError($"Failed to Delete the field:{field.Name} using the {_fieldtype}  Type. \n Stacktrace: \n{e}");
        //                return false;
        //            }
        //            CloseDatabase();
        //            return true;
        //        }
        //
        //        /// <summary>
        //        /// This will Delete a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        //        /// </summary>
        //        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        //        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        //        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        //        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        //        /// <returns>Returns true if the Field is successfully Deleted</returns>
        //        //Test: Test this to make sure it's working.
        //        public bool DeleteField<T, F>(T _database_data, F _field_structure) where T : I_DB_Data where F : I_DB_Field
        //        {
        //            var _tablename = _database_data.GetType().Name;
        //            var _fieldtype = _field_structure.GetType().Name;
        //            try
        //            {
        //                OpenDatabase(_Database_Name);
        ////                Connection.Delete(_database_data, _field_structure);
        //            }
        //            catch (System.Exception e)
        //            {
        //                Debug.LogError($"Failed to Delete the field:{_field_structure.Name} using the {_fieldtype}  Type. \n Stacktrace: \n{e}");
        //                return false;
        //            }
        //            CloseDatabase();
        //            return true;
        //        }


        /// <summary>
        /// This will Delete a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        /// <returns>Returns true if the Field is successfully Deleted</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteField(string tablename, int id)
        {
            try
            {
                OpenDatabase(_Database_Name);
                Connection.Delete(tablename, id);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Delete the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }


        /// <summary>
        /// This will Delete a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        /// <returns>Returns true if the Field is successfully Deleted</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteField(string tablename, string name)
        {
            try
            {
                OpenDatabase(_Database_Name);
                Connection.Delete(tablename, name);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Delete the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }
        /// <summary>
        /// This will Delete a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        /// <returns>Returns true if the Field is successfully Deleted</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteField(string tablename, I_DB_Data data)
        {
            try
            {
                OpenDatabase(_Database_Name);
                Connection.Delete(tablename, data);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Delete the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        /// <summary>
        /// This will Delete a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        /// <returns>Returns true if the Field is successfully Deleted</returns>
        //Test: Test this to make sure it's working.
        public bool DeleteField(I_DB_Data database_data)
        {
            try
            {
                OpenDatabase(_Database_Name);
                Connection.Delete(database_data);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to Delete the Field\n\n" + e);
                return false;
            }
            CloseDatabase();
            return true;
        }

        #endregion //END Region Delete Field

        #region Get Field

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public Base_Field_Structure GetField(string tablename, string name)
        {
            OpenDatabase(_Database_Name);
            var field = Connection.Table(tablename, new Base_Field_Structure()).FirstOrDefault(x => x.Name == name);
            CloseDatabase();
            return field;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public Base_Field_Structure GetField(string tablename, int id)
        {
            OpenDatabase(_Database_Name);
            var field = Connection.Table(tablename, new Base_Field_Structure()).FirstOrDefault(x => x.ID == id);
            CloseDatabase();
            return field;
        }


        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public Base_Field_Structure GetField(I_DB_Data data, int id)
        {
            OpenDatabase(_Database_Name);
            var field = Connection.Table(data.GetType().Name, new Base_Field_Structure()).FirstOrDefault(x => x.ID == id);
            CloseDatabase();
            return field;
        }


        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public F GetField<F>(string tablename, string name, F field) where F : I_DB_Field
        {
            OpenDatabase(_Database_Name);
            field = Connection.Table(tablename, field).FirstOrDefault(x => x.Name == name);
            CloseDatabase();
            return field;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public F GetField<F>(string tablename, string name) where F : I_DB_Field
        {
            OpenDatabase(_Database_Name);
            var field = Connection.Table(tablename, default(F)).FirstOrDefault(x => x.Name == name);
            CloseDatabase();
            return field;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public F GetField<D, F>(D data, F field, string name) where F : I_DB_Field where D : I_DB_Data
        {
            OpenDatabase(_Database_Name);
            field = Connection.Table(data.GetType().Name, field).FirstOrDefault(x => x.Name == name);
            CloseDatabase();
            return field;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public T GetData<T>(string tablename, string name)
        {
            OpenDatabase(_Database_Name);
            var data = JsonUtility.FromJson<T>(Connection.Table(tablename, new Base_Field_Structure()).FirstOrDefault(x => x.Name == name).Data);
            CloseDatabase();
            return data;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public T GetData<T>(string tablename, int id)
        {
            OpenDatabase(_Database_Name);
            var data = JsonUtility.FromJson<T>(Connection.Table(tablename, new Base_Field_Structure()).FirstOrDefault(x => x.ID == id).Data);
            CloseDatabase();
            return data;
        }

        /// <summary>
        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="Base_Field_Structure"/>) as a (<see cref="I_DB_Field"/>)
        /// </summary>
        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        /// <returns>Returns true if the field is successfully Added.</returns>
        //Test: Test this to make sure it's working.
        public D GetData<D, F>(string tablename, F field, string name) where F : I_DB_Field where D : I_DB_Data
        {
            OpenDatabase(_Database_Name);
            var data = Connection.Table(tablename, field).FirstOrDefault(x => x.Name == name).LoadData<D>();
            CloseDatabase();
            return data;
        }

        //        /// <summary>
        //        /// This will add a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        //        /// </summary>
        //        /// <typeparam name="T">The <see cref="I_DB_Data"/> Type being stored.</typeparam>
        //        /// <typeparam name="F">The <see cref="I_DB_Field"/> field structure</typeparam>
        //        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        //        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        //        /// <returns>Returns true if the Field is successfully Added</returns>
        //        //Test: Test this to make sure it's working.
        //        public T GetField<T, F>(T _database_data, F _field_structure) where T : I_DB_Data where F : I_DB_Field
        //        {
        //            var _tablename = _database_data.GetType().Name;
        //            var _fieldtype = _field_structure.GetType().Name;
        //            try
        //            {
        //                OpenDatabase(_Database_Name);
        //                var field = Connection.Table(_database_data,_field_structure).FirstOrDefault(x=> x.Name == _database_data.Name);
        //                CloseDatabase();
        //                return field;
        //            }
        //            catch (System.Exception e)
        //            {
        //                Debug.LogError($"Failed to Get the field:{_field_structure.Name} using the {_fieldtype}  Type. \n Stacktrace: \n{e}");
        //                return default(T);
        //            }
        //        }

        #endregion //END Region Add Field


        //        /// <summary>
        //        /// This will get a Row (Field) with the specified name provided by (<see cref="_database_data"/>) with the type provided by (<see cref="_field_structure"/>) as a (<see cref="I_DB_Field"/>)
        //        /// </summary>
        //        /// <typeparam name="T"></typeparam>
        //        /// <param name="_database_data">Uses the Database Data to get the data to store</param>
        //        /// <param name="_field_structure">The (<see cref="I_DB_Field"/>) to use for creating the table field. </param>
        //        /// <returns>Returns the <see cref="I_DB_Data"/> type <see cref="F"/> if the Field is successfully accessed</returns>
        //        //Test: Test this to make sure it's working.
        //        public T GetField<T, F>(T _database_data, F _field_structure = default(F)) where T : I_DB_Data where F : I_DB_Field
        //        {
        //            System.Type _type = typeof(Base_Field_Structure);
        //            TableQuery<T> table = null;
        //            var _tablename = _database_data.GetType().Name;
        //            I_DB_Field field = default(F);
        //            try
        //            {
        //                _database_data.ID = GetPKIndex(_database_data, _field_structure) + 1;
        //                if (_field_structure == null)
        //                {
        //                    table = Connection.Table<Base_Field_Structure>(_tablename) as TableQuery<T>;
        //                    table.Connection.CreateTable<Base_Field_Structure>(_tablename);
        //                    field = new Base_Field_Structure();
        //                }
        //                else
        //                {
        //                    _type = typeof(F);
        //                    table = Connection.Table<F>(_tablename) as TableQuery<T>;
        //                    table.Connection.CreateTable<F>(_tablename);
        //                    field = default(F);
        //                }
        //            }
        //            catch (System.Exception e)
        //            {
        //                if (_field_structure != null) { Debug.LogError($"Failed to Get the field:{field.Name} using the {_type}  Type. \n Stacktrace: \n{e}"); }
        //                else { Debug.LogError($"Failed to Get the Field:{field} Due to a failure due to the type: {_type.Name} being NULL \n Stacktrace: \n{e}"); }
        //                return default(T);
        //            }
        //            return _database_data;
        //        }

        #endregion


        //        public void SaveDatabaseInfo()
        //        {
        //            string json_info;
        //            if (File.Exists(_Database_Full_Path))
        //            {
        //                var db = this;
        //                json_info = JsonUtility.ToJson(db,true);
        //                Debug.Log(json_info);
        //                File.WriteAllText(_Database_Root_Path + Path.DirectorySeparatorChar + Database_Name + ".json", json_info);
        //            }
        //        }
        //
        //        public void LoadDatabaseInfo()
        //        {
        //            string json_info;
        //            if (File.Exists(_Database_Full_Path))
        //            {
        //                json_info = File.ReadAllText(_Database_Root_Path + Path.DirectorySeparatorChar +  Database_Name + ".json");
        //                Debug.Log(json_info);
        //                JsonUtility.FromJsonOverwrite(json_info,this._Database_Name);
        //            }
        //        }

    }

}