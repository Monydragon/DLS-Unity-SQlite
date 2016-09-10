using System.Collections.Generic;
using DLS.SQLiteUnity.Example;
using UnityEngine;

namespace DLS.SQLiteUnity
{
    [System.Serializable]
    public class Item_DB_field_Structure : I_DB_Field
    {
        public enum TestEnums
        {
            None,
            Taco,
            Taco2,
            Taco3s
        }

        [SerializeField] private int _Id;
        [SerializeField] private string _Name;
        [SerializeField] private string _Description;
        [SerializeField] private bool _sellable = true;
        [SerializeField] private int _value;
        [SerializeField] private bool _stackable = true;
        [SerializeField] private int _stackLimit = 9999;
        [SerializeField] private bool _unique;
        [SerializeField] private TestEnums enumTest;
               
        public MonsterDude dude = new MonsterDude();
//        public MonsterDude2 dude2 = new MonsterDude2();
        public List<string> string_list = new List<string>();

        private string _Data;

        [PrimaryKey, AutoIncrement]
        public int ID
        {
            get { return _Id; }
            set { _Id = value; }
        }

        [Unique]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        public bool Sellable
        {
            get { return _sellable; }
            set { _sellable = value; }
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public bool Stackable
        {
            get { return _stackable; }
            set { _stackable = value; }
        }

        public int StackLimit
        {
            get { return _stackLimit; }
            set { _stackLimit = value; }
        }

        public bool Unique
        {
            get { return _unique; }
            set { _unique = value; }
        }
        
        public TestEnums EnumTest
        {
            get { return enumTest; }
            set { enumTest = value; }
        }

        [NestedProperty]
        public MonsterDude Dude { get { return dude;} set { dude = value; } }


        //        [NestedProperty]
        //        public MonsterDude2 Dude2 { get { return dude2; } set { dude2 = value; } }

        public List<string> String_list
        {
            get { return string_list; }
            set { string_list = value; }
        }

//        [NestedProperty]
//        public MonsterDude2 Dude2 { get; set; }


        public string Data
        {
            get { return _Data; }
            set { _Data = value; }
        }


        //        [NestedProperty]
        //        public MonsterDude Dude
        //        {
        //            get { return dude; }
        //            set { dude = value; }
        //        }
        //
        //        [NestedProperty]
        //        public MonsterDude2 Dude2
        //        {
        //            get { return dude2; }
        //            set { dude2 = value; }
        //        }

        public void AddData<T>(T _object) where T : I_DB_Data
        {
            throw new System.NotImplementedException();
        }

        public void SaveData<T>(T data) where T : I_DB_Data
        {
            throw new System.NotImplementedException();
        }

        public T LoadData<T>() where T : I_DB_Data
        {
            throw new System.NotImplementedException();
        }

    }
}