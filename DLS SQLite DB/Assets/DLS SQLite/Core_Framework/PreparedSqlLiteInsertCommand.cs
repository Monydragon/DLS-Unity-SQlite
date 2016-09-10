using System;

namespace DLS.SQLiteUnity
{
    #region Classes

    /// <summary>
    /// Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {

        #region Fields

        #region Internal Fields

        internal static readonly IntPtr NullStatement = default(IntPtr);

        #endregion //END Region Internal Fields

        #endregion //END Region Fields

        #region Properties

        public bool Initialized { get; set; }

        protected SQLiteConnection Connection { get; set; }

        public string CommandText { get; set; }

        protected IntPtr Statement { get; set; }

        #endregion //END Region Properties

        #region Constructors

        internal PreparedSqlLiteInsertCommand(SQLiteConnection conn)
        {
            Connection = conn;
        }

        #endregion //END Region Constructors

        #region Methods

        #region Public Methods

        public int ExecuteNonQuery(object[] source)
        {
            if (Connection.Trace) { Connection.InvokeTrace("Executing: " + CommandText); }

            var r = SQLite3_DLL_Handler.Result.OK;

            if (!Initialized)
            {
                Statement = Prepare();
                Initialized = true;
            }

            //bind the values.
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    SQLiteCommand.BindParameter(Statement, i + 1, source[i], Connection.StoreDateTimeAsTicks);
                }
            }
            r = SQLite3_DLL_Handler.Step(Statement);

            if (r == SQLite3_DLL_Handler.Result.Done)
            {
                int rowsAffected = SQLite3_DLL_Handler.Changes(Connection.Handle);
                SQLite3_DLL_Handler.Reset(Statement);
                return rowsAffected;
            }

            if (r == SQLite3_DLL_Handler.Result.Error)
            {
                string msg = SQLite3_DLL_Handler.GetErrmsg(Connection.Handle);
                SQLite3_DLL_Handler.Reset(Statement);
                throw SQLiteException.New(r, msg);
            }

            if (r == SQLite3_DLL_Handler.Result.Constraint && SQLite3_DLL_Handler.ExtendedErrCode(Connection.Handle) == SQLite3_DLL_Handler.ExtendedResult.ConstraintNotNull)
            {
                SQLite3_DLL_Handler.Reset(Statement);
                throw NotNullConstraintViolationException.New(r, SQLite3_DLL_Handler.GetErrmsg(Connection.Handle));
            }

            //Throws exception when all conditions fail to validate.
            SQLite3_DLL_Handler.Reset(Statement);
            throw SQLiteException.New(r, r.ToString());
            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion //END Region Public Methods

        #region Protected Methods
        protected virtual IntPtr Prepare()
        {
            var stmt = SQLite3_DLL_Handler.Prepare2(Connection.Handle, CommandText);
            return stmt;
        }

        #endregion //END Region Protected Methods

        #region Private Methods

        private void Dispose(bool disposing)
        {
            if (Statement != NullStatement)
            {
                try
                {
                    SQLite3_DLL_Handler.Finalize(Statement);
                }
                finally
                {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }

        #endregion //END Region Private Methods

        #endregion //End Region Methods

        #region Deconstructors
        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }

        #endregion //END Region Deconstructors

    } //END Class PreparedSqlLiteInsertCommand

    #endregion // END Region Classes

} //END Namespace DLS.SQLiteUnity