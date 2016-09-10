using System.IO;
using DLS.SQLiteUnity;
using DLS.SQLiteUnity.Example;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DB = TestDB;

public class InGameDBGuiHandler : MonoBehaviour
{
    public FileBrowser file_browser;
    public GameObject create_db_panel;
    public GameObject delete_db_panel;
    public Text opened_database_text;
    public Text create_db_directory_text;
    public Text delete_db_directory_text;
    public Toggle EnableCustomDirectoryToggle;
    public Toggle EnableCustomExtensionToggle;
    public InputField Database_NameInputField;
    public InputField Database_ExtensionInputField;
    public InputField Table_NameInputField;
    public InputField Field_NameInputField;
    public GameObject Field_Button_Prefab;
    public GameObject Field_Content_Panel;
    public string Database_Name;
    public string Database_Directory;
    public string Database_Extension;
    public string Database_Full_Path;

    [SerializeField] public Event file_explorer_closed_event;

    [SerializeField]
    public UnityEvent CancelEvent;
    [SerializeField]
    public UnityEvent SelectEvent;

    public void OnEnable()
    {
        file_browser.setDirectory(Application.streamingAssetsPath);
    }

    public void ToggleGameObject(GameObject game_object)
    {
        game_object.SetActive(!game_object.activeSelf);
    }

    public void CreateDatabase()
    {
        Database_Name = Database_NameInputField.text;
        Database_Extension = Database_ExtensionInputField.text;
        DB.RealDatabase.PrepareDatabase(Database_Name, Database_Directory, Database_Extension);

        Debug.Log(string.Format("Trying to Create Database: {0} at {1} with extension: {2} at full path {3}", Database_Name, Database_Directory, Database_Extension, DB.RealDatabase.Database_Full_Path));

        if (Database_Name.Length <= 0)
        {
            Debug.LogError("Cannot Create Database with NO Name.");
        }

        if (EnableCustomDirectoryToggle.isOn == false && EnableCustomExtensionToggle.isOn == false)
        {
            TestDB.RealDatabase.CreateDatabase(DB.RealDatabase.Database_Name);
        }
        else if(EnableCustomDirectoryToggle.isOn && EnableCustomExtensionToggle.isOn)
        {
            TestDB.RealDatabase.CreateDatabase(DB.RealDatabase.Database_Name,DB.RealDatabase.Database_Root_Path,DB.RealDatabase.Database_File_Extension);
        }
        else if (EnableCustomDirectoryToggle.isOn)
        {
            TestDB.RealDatabase.CreateDatabase(DB.RealDatabase.Database_Name, DB.RealDatabase.Database_Root_Path);
        }
        else if (EnableCustomExtensionToggle.isOn)
        {
            TestDB.RealDatabase.CreateDatabase(DB.RealDatabase.Database_Name, _extension:DB.RealDatabase.Database_File_Extension);
        }

//        if (EnableCustomDirectoryToggle.isOn && EnableCustomExtensionToggle.isOn == false)
//        {
//            DB.RealDatabase.CreateDatabase(Database_Name,Database_Directory);
//        }
//        if (EnableCustomDirectoryToggle.isOn == false && EnableCustomExtensionToggle.isOn)
//        {
//            Database_Extension = Database_ExtensionInputField.text;
//            DB.RealDatabase.CreateDatabase(Database_Name,_extension: Database_Extension);
//        }
//
//        if (EnableCustomDirectoryToggle.isOn && EnableCustomExtensionToggle.isOn)
//        {
//            DB.RealDatabase.CreateDatabase(Database_Name, Database_Directory,Database_Extension);
//        }

        if (opened_database_text != null)
        {
            opened_database_text.text = "Database Opened: " + DB.RealDatabase.Database_Name;
        }
    }

    public void OpenDatabase()
    {
        if (DB.RealDatabase.OpenDatabase(file_browser.fileName))
        {
            opened_database_text.text = "Database Opened: " + DB.RealDatabase.Database_Name;
        }
    }

    public void SetDirectoryTextWhenSelected(Text text_field)
    {
        text_field.text = file_browser.fileDirectory;
    }

    public void CloseDatabase()
    {
        if (DB.RealDatabase.CloseDatabase(file_browser.fileName))
        {
            opened_database_text.text = "No Database Opened";
        }
    }

    public void CreateTable()
    {
//        Base_Table_Structure table = new Base_Table_Structure(Table_NameInputField.text,DB.RealDatabase);
        if (DB.RealDatabase.CreateTable(Table_NameInputField.text, DB.RealDatabase))
        {
            DB.OpenedTable = new Base_Table_Structure(Table_NameInputField.text, DB.RealDatabase);
            Debug.Log(string.Format("Table Created: {0} on Database: {1}", Table_NameInputField.text, DB.RealDatabase.Database_Name));
        }
    }

    public void CreateField()
    {
        var data = new MonsterDude(Field_NameInputField.text);
        if (DB.OpenedTable != null)
        {
            DB.OpenedTable.AddField(data);

            foreach (var field in DB.OpenedTable.Fields)
            {
                var old_go = Field_Content_Panel.transform.Find(field.Key);
                if (old_go != null)
                {
                    Destroy(old_go.gameObject);
                }

                var go = Instantiate(Field_Button_Prefab);
                go.name = field.Key;
                go.GetComponentInChildren<Text>().text = field.Key;
                go.SetActive(true);
                go.transform.SetParent(Field_Content_Panel.transform);
            }
        }
    }

    public void EnableFileExplorer(bool enable)
    {
        file_browser.Enabled = enable;
    }

    private string foldername;
    void OnGUI()
    {
        if (file_browser.draw())
        { //true is returned when a file has been selected
          //the output file is a member if the FileInfo class, if cancel was selected the value is null

//            if (create_db_panel.activeSelf)
//            {
//                create_db_panel.SetActive(false);
//            }
//            if (delete_db_panel.activeSelf)
//            {
//                create_db_panel.SetActive(false);
//            }

            Database_Directory = (file_browser.outputFile == null) ? "DEFAULT" : Path.GetDirectoryName(file_browser.outputFile.FullName);
            if (Database_Directory == "DEFAULT")
            {
                Debug.Log("Cancel Event Ran");
                CancelEvent.Invoke();
            }
            else
            {
//                foldername = Path.GetFileNameWithoutExtension(file_browser.fileDirectory
//                foldername = "DEFAULT";
                Debug.Log("Select Event Ran");
                SelectEvent.Invoke();

                create_db_directory_text.text = Path.GetFileNameWithoutExtension(Database_Directory);
                Debug.Log("Create DB Path Name: " + create_db_directory_text.text);
            }

//            string foldername = Path.GetDirectoryName(file_browser.fileDirectory);
//             foldername = Path.GetFileNameWithoutExtension(file_browser.fileDirectory);
//            if (create_db_directory_text != null)
//            {
//                create_db_directory_text.text = Database_Directory;
//                Debug.Log("Create DB Path Name: " + create_db_directory_text.text);
//            }

//            if (create_db_panel.activeSelf && delete_db_panel.activeSelf == false)
//            {
//                create_db_panel.SetActive(true);
//            }
//            if (delete_db_panel.activeSelf && create_db_panel.activeSelf == false)
//            {
//                create_db_panel.SetActive(true);
//            }

        }

        

    }

}
