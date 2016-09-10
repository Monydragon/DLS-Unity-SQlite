namespace DLS.SQLiteUnity
{

    #region Classes

    public abstract class A_BaseTableQuery
    {

        #region Local Classes

        protected class Ordering
        {
            #region Properties

            public string ColumnName { get; set; }
            public bool Ascending { get; set; }

            #endregion //END Region Properties.
        }

        #endregion //END Region Local Classes 
         
    } //END Class A_BaseTableQuery

    #endregion // END Region Classes

} //END Namespace DLS.SQLiteUnity