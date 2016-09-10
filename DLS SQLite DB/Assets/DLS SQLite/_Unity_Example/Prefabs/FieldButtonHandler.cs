using DLS.SQLiteUnity;
using DLS.SQLiteUnity.Example;
using UnityEngine;
using UnityEngine.UI;

public class FieldButtonHandler : MonoBehaviour
{
    public Button Button;
    public Text Text;

    public void Awake()
    {
        Button = GetComponent<Button>();
        Text = GetComponentInChildren<Text>();
    }

    public void OpenField()
    {
        var monster = TestDB.OpenedTable.GetField<Base_Field_Structure>(Text.text);
        Debug.Log(string.Format("Monster Name: {0} Monster Data: {1}", monster.Name, monster.Data));
    }
}
