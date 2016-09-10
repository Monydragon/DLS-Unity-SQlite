//using DLS.PhantasyQuest;

namespace DLS.SQLiteUnity
{
    public interface I_DB_Field
    {
//        [PrimaryKey, AutoIncrement]
        int ID { get; set; }
//        [Unique]
        string Name { get; set; }
        string Data { get; set; }
        void AddData<T>(T _object) where T : I_DB_Data;
        void SaveData<T>(T data) where T : I_DB_Data;
        T LoadData<T>() where T : I_DB_Data;
    }
}