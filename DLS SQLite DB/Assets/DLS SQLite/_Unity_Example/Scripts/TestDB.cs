using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.SQLiteUnity;
using DLS.SQLiteUnity.Example;
using UnityEngine.Networking;

public class TestDB : MonoBehaviour
{
    public bool m_EnableHotKeys = true;
    public static SQLDatabase RealDatabase = new SQLDatabase();
    public static Base_Table_Structure OpenedTable;

    public bool m_RunAutomatedTestsOnStart;
    public int m_AutomatedDelay = 5;
    public string m_DatabaseName = "Database1";
    public string m_Database2Name = "Database2";
    public SQLDatabase m_Database;
    public SQLDatabase m_Database2;

    public Item_DB_field_Structure item1;
    public Item_DB_field_Structure item2;
    public Item_DB_field_Structure item3;

    [SerializeField] public TableMapping tablemap;

    public Base_Field_Structure field;
    public Base_Field_Structure field2;
    public Base_Field_Structure field3;



    [SerializeField]public Base_Table_Structure table;

    public MonsterDude monster = new MonsterDude("Monster 1");
    public MonsterDude2 monster2 = new MonsterDude2("Monster 2");
    public MonsterDude3 monster3 = new MonsterDude3("Monster 3");
    public MonsterDude LoadedMonster;
    public Item_DB_field_Structure LoadedItem;


    private void OnEnable()
    {
        m_Database.PrepareDatabase(m_DatabaseName); //must be called.
        m_Database2.PrepareDatabase(m_Database2Name); //must be called.
    }

    private void Start()
    {
        if (m_RunAutomatedTestsOnStart)
        {
            StartCoroutine(RunAllTests());
        }
    }


    private void Update()
    {
        if (m_Database != null && m_EnableHotKeys)
        {

            if (Input.GetKeyDown(KeyCode.N))
            {
//                ItemDBModel item1Model = item1.toModel();

                m_Database.CreateDatabase("MainDB");
                m_Database.CreateTable("Items", item1);
//                m_Database.CreateTable("Demons", item1);
                m_Database.AddField("Items", item1);
//                m_Database.AddField("Items", item2);
//                m_Database.AddField("Items", item3);

//                var item = m_Database.GetField<Item_DB_field_Structure>("Items", item1.Name);
//                item1.Name = item.Name;
//                item1.Data = item.Data;
//                item1.Description = item.Description;
//                item1.Sellable = item.Sellable;

//                item1.dude = item.Dude;
//                item1.dude2 = item.Dude2;

//                item1.EnumTest = item.EnumTest;

//                Debug.Log("ENUM TEST: " + item.EnumTest.ToString() );
//                Debug.Log("SELLABLE BOOL: " + item.Sellable.ToString() );

//                item1.String_list = item.String_list;
                //
                //                m_Database.OpenDatabase("MainDB");
                //                var db_items = m_Database.Connection.Query<Item_DB_field_Structure>("SELECT * FROM Items");
                //
                //                foreach (var i in db_items)
                //                {
                //                    Debug.Log("DB FOUND: " + i.Name);
                //                }
                //
                //                var db_tables = m_Database.Tables;
                //                foreach (var t in db_tables)
                //                {
                //                    Debug.Log("TABLE: " + t.Key);
                //                }
                //
                //                Debug.Log(db_tables.ToString());
                //                m_Database.CloseDatabase();
                //                Debug.Log(item.EnumTestString);
                //                Debug.Log(item.EnumTest.ToString());

                // IEnumarble<TableType>
                //return connection.Query<TableType>("SELECT * FROM TableType")
                //                m_Database.Connection.CreateTable<ItemModel>();
            }
            if (Input.GetKeyDown(KeyCode.BackQuote)) { StartCoroutine(RunAllTests()); }

            if (Input.GetKeyDown(KeyCode.Equals)) { CreateDatabaseTests(); }

            if (Input.GetKeyDown(KeyCode.Alpha1)) { CreateTableTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { AddFieldTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { GetFieldTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { GetDataTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha5)) { UpdateFieldTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha6)) { UpdateAllFieldsTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha7)) { DeleteFieldTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha8)) { ReplaceTableTests(); }
            if (Input.GetKeyDown(KeyCode.Alpha9)) { DeleteTableTests(); }

            if (Input.GetKeyDown(KeyCode.Minus)) { DeleteDatabaseTests(); }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                table = new Base_Table_Structure("Ninja", "Tacos");

                Debug.Log(string.Format("Table: {0} Created on {1}.",table.Name,m_Database.Database_Name));

                foreach (var obj in table.Fields)
                {
                    Debug.Log(string.Format("Field: {0} Found", obj.Value.Name));
                }
                table.AddField(monster);
                table.AddField(monster2);

//                field = table.GetField<Base_Field_Structure>(monster.Name);

//                Debug.Log(field.Data);

//                field2 = table.GetField<Base_Field_Structure>(monster2.Name);
                Debug.Log(field2.Name + " Data: " + field2.Data);

            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                m_Database.CreateDatabase(m_DatabaseName);
                m_Database.CreateTable("Hero",m_Database);

                foreach (var key in m_Database.Tables)
                {
                 Debug.Log(string.Format("key: {0}", key.Key));   
                }
                m_Database.Tables["Hero"].AddField(monster);
                Debug.Log(string.Format("Database Created: {0} Added Table: {1} Added Field {2}", m_DatabaseName, m_Database.Tables["Hero"].Name, monster.Name));
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                m_Database.Tables["Hero"].RenameTable("Dicks");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
//                m_Database.Tables["Hero"].DeleteField("Monster 1");
//                Debug.Log(string.Format("{monster.Name} Deleted!");
                table.DeleteField("Monster 2");
            }
        }

    }


    public IEnumerator RunAllTests()
    {
        yield return CreateDatabaseTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return CreateTableTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return AddFieldTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return GetFieldTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return GetDataTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return UpdateFieldTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return UpdateAllFieldsTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return ReplaceTableTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return DeleteFieldTests();
        yield return new WaitForSeconds(m_AutomatedDelay);
        yield return DeleteDatabaseTests();
        Debug.Log("Test Ran RunAllTests");
    }

    public IEnumerator CreateDatabaseTests()
    {
        if (m_DatabaseName.Length > 0)
        {
            m_Database = new SQLDatabase(m_DatabaseName,true);
            if (System.IO.File.Exists(m_Database.Database_Full_Path)) { Debug.Log("Create Database Test 1 Worked"); } else { Debug.LogError("Create Database Test 1 Failed"); }

//            m_Database2 = new SQLDatabase();
//            if (m_Database2.CreateDatabase(m_Database2Name)) { Debug.Log("Create Database Test 2 Worked"); } else { Debug.LogError("Create Database Test 2 Failed"); }

//            Debug.Log(string.Format("DATABASES Created: 1:{m_DatabaseName} 2:{m_Database2.Database_Name}");
        }
        else
        {
            Debug.LogError("Database is Null or Database name is Empty");
        }
        Debug.Log("Test Ran CreateDatabaseTests");
        return null;
    }

    public IEnumerator DeleteDatabaseTests()
    {
        if (m_Database.DeleteDatabase(m_DatabaseName)) { Debug.Log("Delete Database Test 1 Worked"); } else { Debug.LogError("Delete Database Test 1 Failed"); }
        if (m_Database2.DeleteDatabase(m_Database2Name)) { Debug.Log("Delete Database Test 2 Worked"); } else { Debug.LogError("Delete Database Test 2 Failed"); }
        Debug.Log("Test Ran DeleteDatabaseTests");
        return null;
    }

    public IEnumerator CreateTableTests()
    {
        if (m_Database != null)
        {
//            if (m_Database.CreateTable("Tacos")) { Debug.Log("Create Table Test 1 Worked"); } else { Debug.LogError("Create Table Test 1 Failed"); }
//            if (m_Database.CreateTable("New Tacos", field)) { Debug.Log("Create Table Test 2 Worked"); } else { Debug.LogError("Create Table Test 2 Failed"); }
//            if (m_Database.CreateTable(monster2)) { Debug.Log("Create Table Test 3 Worked"); } else { Debug.LogError("Create Table Test 3 Failed"); }
//            if (m_Database.CreateTable(monster, new Base_Field_Structure())) { Debug.Log("Create Table Test 4 Worked"); } else { Debug.LogError("Create Table Test 4 Failed"); }

            if (m_Database.CreateTable("Items", item1))
            {
                Debug.Log("Table Created!");
            }
                
            foreach (var db in m_Database.Connection.TableMappings)
            {
                Debug.Log(string.Format("Table Created: {0}", db.TableName));
            }
            Debug.Log("Test Ran CreateTableTests");
        }
        return null;
    }

    public IEnumerator ReplaceTableTests()
    {
        if (m_Database != null)
        {
            if (m_Database.ReplaceTable("Tacos")) { Debug.Log("Replace Table Test 1 Worked"); } else { Debug.LogError("Replace Table Test 1 Failed"); }
            if (m_Database.ReplaceTable("New Tacos", field)) { Debug.Log("Replace Table Test 2 Worked"); } else { Debug.LogError("Replace Table Test 2 Failed"); }
            if (m_Database.ReplaceTable(monster2)) { Debug.Log("Replace Table Test 3 Worked"); } else { Debug.LogError("Replace Table Test 3 Failed"); }
            if (m_Database.ReplaceTable(monster, new Base_Field_Structure())) { Debug.Log("Replace Table Test 4 Worked"); } else { Debug.LogError("Replace Table Test 4 Failed"); }

            foreach (var db in m_Database.Connection.TableMappings)
            {
                Debug.Log(string.Format("Table Replaced: {0}", db.TableName));
            }
            Debug.Log("Test Ran ReplaceTableTests");

        }
        return null;
    }

    public IEnumerator DeleteTableTests()
    {
        new WaitForSeconds(2);
        if (m_Database != null)
        {
//            if (m_Database.DeleteTable("Tacos")) { Debug.Log("Delete Table Test 1 Worked"); } else { Debug.LogError("Delete Table Test 1 Failed"); }
//            if (m_Database.DeleteTable("New Tacos", field)) { Debug.Log("Delete Table Test 2 Worked"); } else { Debug.LogError("Delete Table Test 2 Failed"); }
//            if (m_Database.DeleteTable(monster2)) { Debug.Log("Delete Table Test 3 Worked"); } else { Debug.LogError("Delete Table Test 3 Failed"); }
            if (m_Database.DeleteTable(monster, new Base_Field_Structure())) { Debug.Log("Delete Table Test 4 Worked"); } else { Debug.LogError("Delete Table Test 4 Failed"); }
            Debug.Log("Test Ran DeleteTableTests");

        }
        return null;
    }

    public IEnumerator AddFieldTests()
    {
        if (m_Database != null)
        {
            m_Database.AddField("Items", item1);
            m_Database.AddField("Items", item2);
            //            MonsterDude d1 = new MonsterDude("Dude Monster Fire");
            //            MonsterDude d2 = new MonsterDude("Dude Monster Water");
            //            MonsterDude d3 = new MonsterDude("Dude Monster Earth");
            //            MonsterDude d4 = new MonsterDude("Dude Monster Wind");
            //            if (m_Database.AddField(monster)) { Debug.Log("Add Field Test 1 Worked"); } else { Debug.Log("Add Field Test 1 Failed"); }
            //            if (m_Database.AddField(d1)) { Debug.Log("Add Field Test 2 Worked"); } else { Debug.Log("Add Field Test 2 Failed"); }
            //            if (m_Database.AddField(d2)) { Debug.Log("Add Field Test 3 Worked"); } else { Debug.Log("Add Field Test 3 Failed"); }
            //            if (m_Database.AddField(d3)) { Debug.Log("Add Field Test 5 Worked"); } else { Debug.Log("Add Field Test 5 Failed"); }
            //            if (m_Database.AddField(d4)) { Debug.Log("Add Field Test 6 Worked"); } else { Debug.Log("Add Field Test 6 Failed"); }

            //                        if (m_Database.AddField(monster2,field)) { Debug.Log("Add Field Test 2 Worked"); } else { Debug.Log("Add Field Test 2 Failed"); }
            //            if (m_Database.AddField("New Tacos",monster)) { Debug.Log("Add Field Test 2 Worked"); } else { Debug.Log("Add Field Test 2 Failed"); }
            //            if (m_Database.AddField("Tacos", monster2)) { Debug.Log("Add Field Test 2 Worked"); } else { Debug.Log("Add Field Test 2 Failed"); }

            Debug.Log("Test Ran AddFieldTests");

        }
        return null;
    }
    public IEnumerator UpdateFieldTests()
    {
        if (m_Database != null)
        {
            if (m_Database.UpdateField(monster)) { Debug.Log("Update Field Test 1 Worked"); } else { Debug.LogError("Update Field Test 1 Failed"); }
//            if (m_Database.UpdateField(monster2, field)) { Debug.Log("Update Field Test 2 Worked"); } else { Debug.LogError("Update Field Test 2 Failed"); }
            Debug.Log("Test Ran UpdateFieldTests");
        }
        return null;
    }

    public IEnumerator UpdateAllFieldsTests()
    {
        if (m_Database != null)
        {
            if (m_Database.UpdateAllFields(monster)) { Debug.Log("Update All Fields Test 1 Worked"); } else { Debug.LogError("Update All Fields Test 1 Failed"); }
//            if (m_Database.UpdateAllFields(monster2, field)) { Debug.Log("Update All Fields Test 2 Worked"); } else { Debug.LogError("Update All Fields Test 2 Failed"); }
            Debug.Log("Test Ran UpdateAllFieldsTests");
        }
        return null;
    }


    public IEnumerator DeleteFieldTests()
    {
        if (m_Database != null)
        {
//            m_Database.DeleteField(monster.GetType().Name, 2);
            m_Database.DeleteField(monster);
//            if (m_Database.DeleteField(monster)) { Debug.Log("Delete Field Test 1 Worked"); } else { Debug.LogError("Delete Field Test 1 Failed"); }
//            if (m_Database.DeleteField(monster2, field)) { Debug.Log("Delete Field Test 2 Worked"); } else { Debug.LogError("Delete Field Test 2 Failed"); }
            Debug.Log("Test Ran DeleteFieldTests");
        }
        return null;
    }

    public IEnumerator GetFieldTests()
    {
        if (m_Database != null)
        {
            
            var data1 = m_Database.GetField("Items",item1.Name);
            var data2 = m_Database.GetField("Items",2);

            Debug.Log(string.Format("Field Name: {0} Field ID: {1} 2: Name: {2} Field ID: {3}", data1.Name,data1.ID,data2.Name,data2.ID));
            Debug.Log("Test Ran GetFieldTests");
        }
        return null;
    }


    public IEnumerator GetDataTests()
    {
        if (m_Database != null)
        {
            var data1 = m_Database.GetData<Item_DB_field_Structure>("Items", item1.Name);
            var data2 = m_Database.GetData<Item_DB_field_Structure>("Items", 2);

            Debug.Log(string.Format(data1.Name));
            Debug.Log(string.Format(data2.Name));

            LoadedItem = data1;
            Debug.Log("Test Ran GetDataTests");
        }
        return null;
    }

    public bool OpenDatabase(string database_name)
    {
        return m_Database.OpenDatabase(database_name);
    }
}

