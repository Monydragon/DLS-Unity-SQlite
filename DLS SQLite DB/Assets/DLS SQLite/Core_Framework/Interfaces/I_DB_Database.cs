using System.Collections.Generic;

namespace DLS.SQLiteUnity
{
    public interface I_DB_Database
    {
        //Database Properties
        string Name { get; set; }
        List<I_DB_Table> Tables { get; set; }
        SQLiteConnection Connection { get; set; }
        SQLiteOpenFlags OpenFlags { get; set; }
        string RootPath { get; set; }
        string FullPath { get; set; }
        string File_Extension { get; set; }

        //Database Handling
        bool Prepare();
        bool Create();
        bool Open();
        bool Close();
        bool Delete();

        //Table Handling
        bool AddTable();
        bool ReplaceTable();
        bool DeleteTable();
        I_DB_Table GetTable();

        //Field Handling (Might Move to just handle with Tables)

        bool AddField();
        bool UpdateField();
        bool DeleteField();
        I_DB_Field GetField();
        I_DB_Data GetData();


    }
}