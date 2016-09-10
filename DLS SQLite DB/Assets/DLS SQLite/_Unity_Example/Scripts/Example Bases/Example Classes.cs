using System.Collections.Generic;
using UnityEngine;

namespace DLS.SQLiteUnity.Example
{
    [System.Serializable]
    public class MonsterDude : I_DB_Data
    {
        public string _name;
        public int _age;
        public List<int> _tacosList = new List<int> { 5, 2, 4, 5, 1, 2, 424, 5, 345, 12, 5, 34, 234, 5, 345, 12 };

        [Column("Monster Name")]
        public string Name { get { return _name; } set { _name = value; } }

        [Column("Monster Age")]
        public int Age { get { return _age; } set { _age = value; } }

        [Ignore]
        public List<int> TacoList { get { return _tacosList; } set { _tacosList = value; } }

//        [Ignore]
//        public MonsterDude2 Monster2 { get; set; }
        

        public MonsterDude()
        {

        }
        public MonsterDude(string name)
        {
            _name = name;
        }
    }
    [System.Serializable]
    public class MonsterDude2 : I_DB_Data
    {
        public string _name;
        public bool _alive;
        public float _amount;
        public List<MonsterDude> _Dudes = new List<MonsterDude>();

        [Column("Monster 2 Name")]
        public string Name { get { return _name; } set { _name = value; } }
        public bool Alive { get { return _alive; } set { _alive = value; } }
        public float Amount { get { return _amount; } set { _amount = value; } }

        public MonsterDude2()
        {
            
        }

        public MonsterDude2(string name)
        {
            Name = name;
        }
    }

    [System.Serializable]
    public class MonsterDude3 : I_DB_Data
    {

        [SerializeField]
        private string _name;
        [SerializeField]
        public Dictionary<string, MonsterDude> MonsterDictionary = new Dictionary<string, MonsterDude>();

        public string Name { get { return _name; } set { _name = value; } }

        public MonsterDude3(string name)
        {
            Name = name;
        }
    }
}
