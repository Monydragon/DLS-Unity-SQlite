using System.Collections.Generic;

namespace DLS.SQLiteUnity
{
    public interface I_DB_Table
    {
        string Name { get; }
        Dictionary<string,I_DB_Field> Fields { get; }

        SQLDatabase Database { get; }

        bool AddField(I_DB_Data data);
        bool AddField(I_DB_Data data, I_DB_Field row_field);
        bool DeleteField(I_DB_Data data);

        bool DeleteField(int id);
        bool DeleteField(string name);

        bool RenameTable(string name);


        T GetField<T>(string field_name) where T : I_DB_Field;
        T GetField<T, D>(D data, T field_type) where T : I_DB_Field where D : I_DB_Data;

//
//        bool AddField();
//        bool ReplaceField();
//        bool DeleteField();
//
//        I_DB_Field GetField();
//        I_DB_Data GetData();
    }
}