using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DLS.SQLiteUnity
{

    
    #region Classes

    public class TableQuery<I_DB_Field> : A_BaseTableQuery, IEnumerable<I_DB_Field>
    {

        #region Local Classes

        private class CompileResult
        {
            #region Properties

            public string CommandText { get; set; }

            public object Value { get; set; }

            #endregion //END Region Properties

        } //END Class CompileResult

        #endregion //END Region Local Classes  

        #region Fields

        #region Public Fields

        public bool _deferred;

        #endregion //END Region Public Fields

        #region Private Fields

        private Expression _where;
        private List<Ordering> _orderBys;
        private int? _limit;
        private int? _offset;
        private A_BaseTableQuery _joinInner;
        private Expression _joinInnerKeySelector;
        private A_BaseTableQuery _joinOuter;
        private Expression _joinOuterKeySelector;
        private Expression _joinSelector;
        private Expression _selector;

        #endregion //END Region Private Fields

        #endregion //END Region Fields

        #region Properties

        public SQLiteConnection Connection { get; private set; }

        public TableMapping Table { get; private set; }

        #endregion //END Region Properties

        #region Constructors
        
        public TableQuery()
        {
	        
        }

        public TableQuery(SQLiteConnection conn)
        {
            Connection = conn;
            Table = Connection.GetMapping(typeof(I_DB_Field));
        }

        //Added by DLS
        public TableQuery(string tablename, SQLiteConnection conn, object field_type = null)
        {
            Type _type = typeof(I_DB_Field);
            if (field_type == null) { _type = typeof(Base_Field_Structure); }
            else { _type = field_type.GetType(); }
            Connection = conn;
            Table = Connection.GetMapping(tablename, _type);
        }

        private TableQuery (SQLiteConnection conn, TableMapping table)
        {
            Connection = conn;
            Table = table;
        }

        #endregion //END Region Constructors

        #region Methods

        #region Public Methods

        public TableQuery<U> Clone<U> ()
        {
            var q = new TableQuery<U> (Connection, Table);
            q._where = _where;
            q._deferred = _deferred;
            if (_orderBys != null) {
                q._orderBys = new List<Ordering> (_orderBys);
            }
            q._limit = _limit;
            q._offset = _offset;
            q._joinInner = _joinInner;
            q._joinInnerKeySelector = _joinInnerKeySelector;
            q._joinOuter = _joinOuter;
            q._joinOuterKeySelector = _joinOuterKeySelector;
            q._joinSelector = _joinSelector;
            q._selector = _selector;
            return q;
        }

        public TableQuery<I_DB_Field> Where (Expression<Func<I_DB_Field, bool>> predExpr)
        {
            if (predExpr.NodeType == ExpressionType.Lambda) {
                var lambda = (LambdaExpression)predExpr;
                var pred = lambda.Body;
                var q = Clone<I_DB_Field> ();
                q.AddWhere (pred);
                return q;
            } else {
                throw new NotSupportedException ("Must be a predicate");
            }
        }

        public TableQuery<I_DB_Field> Take (int n)
        {
            var q = Clone<I_DB_Field> ();
            q._limit = n;
            return q;
        }

        public TableQuery<I_DB_Field> Skip (int n)
        {
            var q = Clone<I_DB_Field> ();
            q._offset = n;
            return q;
        }

        public I_DB_Field ElementAt (int index)
        {
            return Skip (index).Take (1).First ();
        }

        public TableQuery<I_DB_Field> Deferred ()
        {
            var q = Clone<I_DB_Field> ();
            q._deferred = true;
            return q;
        }

        public TableQuery<I_DB_Field> OrderBy<U> (Expression<Func<I_DB_Field, U>> orderExpr)
        {
            return AddOrderBy<U> (orderExpr, true);
        }

        public TableQuery<I_DB_Field> OrderByDescending<U> (Expression<Func<I_DB_Field, U>> orderExpr)
        {
            return AddOrderBy<U> (orderExpr, false);
        }

        public TableQuery<I_DB_Field> ThenBy<U>(Expression<Func<I_DB_Field, U>> orderExpr)
        {
            return AddOrderBy<U>(orderExpr, true);
        }

        public TableQuery<I_DB_Field> ThenByDescending<U>(Expression<Func<I_DB_Field, U>> orderExpr)
        {
            return AddOrderBy<U>(orderExpr, false);
        }

        public TableQuery<TResult> Join<TInner, TKey, TResult> (
            TableQuery<TInner> inner,
            Expression<Func<I_DB_Field, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Expression<Func<I_DB_Field, TInner, TResult>> resultSelector)
        {
            var q = new TableQuery<TResult> (Connection, Connection.GetMapping (typeof (TResult))) {
                _joinOuter = this,
                _joinOuterKeySelector = outerKeySelector,
                _joinInner = inner,
                _joinInnerKeySelector = innerKeySelector,
                _joinSelector = resultSelector,
            };
            return q;
        }
				
        public TableQuery<TResult> Select<TResult> (Expression<Func<I_DB_Field, TResult>> selector)
        {
            var q = Clone<TResult> ();
            q._selector = selector;
            return q;
        }

        public int Count ()
        {
            return GenerateCommand("count(*)").ExecuteScalar<int> ();			
        }

        public int Count (Expression<Func<I_DB_Field, bool>> predExpr)
        {
            return Where (predExpr).Count ();
        }

        public I_DB_Field First ()
        {
            var query = Take (1);
            return query.ToList<I_DB_Field>().First ();
        }

        public I_DB_Field FirstOrDefault ()
        {
            var query = Take (1);
            return query.ToList<I_DB_Field>().FirstOrDefault ();
        }

        public IEnumerator<I_DB_Field> GetEnumerator()
        {
            if (!_deferred)
                return GenerateCommand("*").ExecuteQuery<I_DB_Field>().GetEnumerator();

            return GenerateCommand("*").ExecuteDeferredQuery<I_DB_Field>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion //END Region Public Methods

        #region Private Methods

        private TableQuery<I_DB_Field> AddOrderBy<U> (Expression<Func<I_DB_Field, U>> orderExpr, bool asc)
        {
            if (orderExpr.NodeType == ExpressionType.Lambda)
            {
                var lambda = (LambdaExpression)orderExpr;
				
                MemberExpression mem = null;
				
                var unary = lambda.Body as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                {
                    mem = unary.Operand as MemberExpression;
                }
                else
                {
                    mem = lambda.Body as MemberExpression;
                }
				
                if (mem != null && (mem.Expression.NodeType == ExpressionType.Parameter))
                {
                    var q = Clone<I_DB_Field> ();
                    if (q._orderBys == null)
                    {
                        q._orderBys = new List<Ordering> ();
                    }
                    q._orderBys.Add (new Ordering
                    {
                        ColumnName = Table.FindColumnWithPropertyName(mem.Member.Name).Name,
                        Ascending = asc
                    });
                    return q;
                }
                else
                {
                    throw new NotSupportedException ("Order By does not support: " + orderExpr);
                }
            }
            else
            {
                throw new NotSupportedException ("Must be a predicate");
            }
        }

        private void AddWhere (Expression pred)
        {
            if (_where == null)
            {
                _where = pred;
            }
            else
            {
                _where = Expression.AndAlso (_where, pred);
            }
        }
				
        private SQLiteCommand GenerateCommand (string selectionList)
        {
            if (_joinInner != null && _joinOuter != null)
            {
                throw new NotSupportedException ("Joins are not supported.");
            }
            else
            {
                var cmdText = "select " + selectionList + " from \"" + Table.TableName + "\"";
                var args = new List<object> ();

                if (_where != null)
                {
                    var w = CompileExpr (_where, args);
                    cmdText += " where " + w.CommandText;
                }

                if ((_orderBys != null) && (_orderBys.Count > 0))
                {
                    var t = string.Join (", ", _orderBys.Select (o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " desc")).ToArray ());
                    cmdText += " order by " + t;
                }

                if (_limit.HasValue)
                {
                    cmdText += " limit " + _limit.Value;
                }

                if (_offset.HasValue)
                {
                    if (!_limit.HasValue)
                    {
                        cmdText += " limit -1 ";
                    }
                    cmdText += " offset " + _offset.Value;
                }

                return Connection.CreateCommand (cmdText, args.ToArray ());
            }
        }



        private CompileResult CompileExpr (Expression expr, List<object> queryArgs)
        {
            if (expr == null)
            {
                throw new NotSupportedException ("Expression is NULL");
            }

            if (expr is BinaryExpression)
            {
                var bin = (BinaryExpression)expr;
				
                var leftr = CompileExpr (bin.Left, queryArgs);
                var rightr = CompileExpr (bin.Right, queryArgs);

                //If either side is a parameter and is null, then handle the other side specially (for "is null"/"is not null")
                string text;
                if (leftr.CommandText == "?" && leftr.Value == null)
                    text = CompileNullBinaryExpression(bin, rightr);
                else if (rightr.CommandText == "?" && rightr.Value == null)
                    text = CompileNullBinaryExpression(bin, leftr);
                else
                    text = "(" + leftr.CommandText + " " + GetSqlName(bin) + " " + rightr.CommandText + ")";
                return new CompileResult { CommandText = text };
            }

            if (expr.NodeType == ExpressionType.Call)
            {
				
                var call = (MethodCallExpression)expr;
                var args = new CompileResult[call.Arguments.Count];
                var obj = call.Object != null ? CompileExpr (call.Object, queryArgs) : null;
				
                for (var i = 0; i < args.Length; i++)
                {
                    args [i] = CompileExpr (call.Arguments [i], queryArgs);
                }
				
                var sqlCall = "";
				
                if (call.Method.Name == "Like" && args.Length == 2)
                {
                    sqlCall = "(" + args [0].CommandText + " like " + args [1].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 2)
                {
                    sqlCall = "(" + args [1].CommandText + " in " + args [0].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 1)
                {
                    if (call.Object != null && call.Object.Type == typeof(string))
                    {
                        sqlCall = "(" + obj.CommandText + " like ('%' || " + args [0].CommandText + " || '%'))";
                    }
                    else
                    {
                        sqlCall = "(" + args [0].CommandText + " in " + obj.CommandText + ")";
                    }
                }
                else if (call.Method.Name == "StartsWith" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " like (" + args [0].CommandText + " || '%'))";
                }
                else if (call.Method.Name == "EndsWith" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " like ('%' || " + args [0].CommandText + "))";
                }
                else if (call.Method.Name == "Equals" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " = (" + args[0].CommandText + "))";
                }
                else if (call.Method.Name == "ToLower")
                {
                    sqlCall = "(lower(" + obj.CommandText + "))"; 
                }
                else if (call.Method.Name == "ToUpper")
                {
                    sqlCall = "(upper(" + obj.CommandText + "))"; 
                }
                else
                {
                    sqlCall = call.Method.Name.ToLower () + "(" + string.Join (",", args.Select (a => a.CommandText).ToArray ()) + ")";
                }
                return new CompileResult { CommandText = sqlCall };
				
            }

            if (expr.NodeType == ExpressionType.Constant)
            {
                var c = (ConstantExpression)expr;
                queryArgs.Add (c.Value);
                return new CompileResult
                {
                    CommandText = "?",
                    Value = c.Value
                };
            }

            if (expr.NodeType == ExpressionType.Convert)
            {
                var u = (UnaryExpression)expr;
                var ty = u.Type;
                var valr = CompileExpr (u.Operand, queryArgs);
                return new CompileResult
                {
                    CommandText = valr.CommandText,
                    Value = valr.Value != null ? ConvertTo (valr.Value, ty) : null
                };
            }

            if (expr.NodeType == ExpressionType.MemberAccess)
            {
                var mem = (MemberExpression)expr;
				
                if (mem.Expression!=null && mem.Expression.NodeType == ExpressionType.Parameter)
                {
                    //
                    // This is a column of our table, output just the column name
                    // Need to translate it if that column name is mapped
                    //
                    var columnName = Table.FindColumnWithPropertyName (mem.Member.Name).Name;
                    return new CompileResult { CommandText = "\"" + columnName + "\"" };
                }
                    object obj = null;
                    if (mem.Expression != null)
                    {
                        var r = CompileExpr (mem.Expression, queryArgs);

                        if (r.Value == null)
                        {
                            throw new NotSupportedException ("Member access failed to compile expression");
                        }

                        if (r.CommandText == "?")
                        {
                            queryArgs.RemoveAt (queryArgs.Count - 1);
                        }

                        obj = r.Value;
                    }
					
                    //
                    // Get the member value
                    //
                    object val = null;
					
#if !NETFX_CORE
                    if (mem.Member.MemberType == MemberTypes.Property)
                    {
#else
					if (mem.Member is PropertyInfo) 
                    {
#endif
                        var m = (PropertyInfo)mem.Member;
                        //val = m.GetValue (obj, null);
                        val = m.GetGetMethod().Invoke(obj, null);
#if !NETFX_CORE
                    }
                    else if (mem.Member.MemberType == MemberTypes.Field)
                    {
#else
					}
                    else if (mem.Member is FieldInfo) 
                    {
#endif
#if SILVERLIGHT
						val = Expression.Lambda (expr).Compile ().DynamicInvoke ();
#else
                        var m = (FieldInfo)mem.Member;
                        val = m.GetValue (obj);
#endif
                    }
                    else
                    {
#if !NETFX_CORE
                        throw new NotSupportedException ("MemberExpr: " + mem.Member.MemberType);
#else
						throw new NotSupportedException ("MemberExpr: " + mem.Member.DeclaringType);
#endif
                    }
					
                    //
                    // Work special magic for enumerables
                    //
                    if (val != null && val is System.Collections.IEnumerable && !(val is string) && !(val is System.Collections.Generic.IEnumerable<byte>))
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("(");
                        var head = "";
                        foreach (var a in (System.Collections.IEnumerable)val)
                        {
                            queryArgs.Add(a);
                            sb.Append(head);
                            sb.Append("?");
                            head = ",";
                        }
                        sb.Append(")");
                        return new CompileResult
                        {
                            CommandText = sb.ToString(),
                            Value = val
                        };
                    }

                    queryArgs.Add (val);
                    return new CompileResult
                    {
                        CommandText = "?",
                        Value = val
                    };
                }

            throw new NotSupportedException ("Cannot compile: " + expr.NodeType.ToString ());
        }

        private static object ConvertTo (object obj, Type t)
        {
            Type nut = Nullable.GetUnderlyingType(t);
			
            if (nut != null)
            {
                if (obj == null) return null;				
                return Convert.ChangeType (obj, nut);
            }

            //returns if condition is not met.
            return Convert.ChangeType (obj, t);
        }

        /// <summary>
        /// Compiles a BinaryExpression where one of the parameters is null.
        /// </summary>
        /// <param name="parameter">The non-null parameter</param>
        private string CompileNullBinaryExpression(BinaryExpression expression, CompileResult parameter)
        {
            if (expression.NodeType == ExpressionType.Equal)
            {
                return "(" + parameter.CommandText + " is ?)";
            }

            if (expression.NodeType == ExpressionType.NotEqual)
            {
                return "(" + parameter.CommandText + " is not ?)";
            }

            //Throws if either condition is not met.
            throw new NotSupportedException("Cannot compile Null-BinaryExpression with type " + expression.NodeType.ToString());
                
        }

        private string GetSqlName (Expression expr) //test:Test to validate.
        {
            var n = expr.NodeType;

            //There might be more types but this is what was done previously for conditional types.
            switch (n)
            {
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.And: return "&";
                case ExpressionType.AndAlso: return "and";
                case ExpressionType.Or: return "|";
                case ExpressionType.OrElse: return "or";
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "!=";
                default: throw new NotSupportedException("Cannot get SQL for: " + n);
            }

//OLD CODE
//            if (n == ExpressionType.GreaterThan)
//            {
//                return ">";
//            }
//            if (n == ExpressionType.GreaterThanOrEqual)
//            {
//                return ">=";
//            }
//            else if (n == ExpressionType.LessThan)
//            {
//                return "<";
//            }
//            else if (n == ExpressionType.LessThanOrEqual)
//            {
//                return "<=";
//            }
//            else if (n == ExpressionType.And)
//            {
//                return "&";
//            }
//            else if (n == ExpressionType.AndAlso)
//            {
//                return "and";
//            }
//            else if (n == ExpressionType.Or)
//            {
//                return "|";
//            }
//            else if (n == ExpressionType.OrElse)
//            {
//                return "or";
//            }
//            else if (n == ExpressionType.Equal)
//            {
//                return "=";
//            }
//            else if (n == ExpressionType.NotEqual)
//            {
//                return "!=";
//            }
//            else
//            {
//                throw new NotSupportedException("Cannot get SQL for: " + n);
//            }
//
        }

        #endregion //END Region Private Methods

        #endregion //End Region Methods
    } //END Class TableQuery

    #endregion // END Region Classes

} //END Namespace DLS.SQLiteUnity