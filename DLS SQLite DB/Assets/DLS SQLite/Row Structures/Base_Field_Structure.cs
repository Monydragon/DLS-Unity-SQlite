using UnityEngine;

namespace DLS.SQLiteUnity
{
    [System.Serializable]
    public class Base_Field_Structure : I_DB_Field
    {
        [SerializeField]
        private int _id;
        [SerializeField]
        private string _name;
        [SerializeField]
        private string _JsonText;

        [PrimaryKey, AutoIncrement]
        public int ID { get { return _id; } set { _id = value; } }
        [Unique]
        public string Name { get { return _name; } set { _name = value; } }
        public string Data { get { return _JsonText; } set { _JsonText = value; } }

        public Base_Field_Structure(I_DB_Data _data)
        {
            AddData(_data);
        }

        public Base_Field_Structure()
        {

        }

        public void AddData<T>(T _object) where T : I_DB_Data
        {
            _name = _object.Name;
            SaveData(_object);
        }

        public void SaveData<T>(T data) where T : I_DB_Data
        {
            _JsonText = JsonUtility.ToJson(data);
        }

        public T LoadData<T>() where T : I_DB_Data
        {
            return JsonUtility.FromJson<T>(_JsonText);
        }

        public override string ToString()
        {
            var info = "ID:" + _id + " Name:" +_name;
            return info;
        }
    }
}