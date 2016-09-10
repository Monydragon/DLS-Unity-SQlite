using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DLS.SQLiteUnity
{
    #region Classes

    //Object Relational_Mapping? //Originally named ORM
    public static class Object_Relational_Mapping
    {

        #region Fields

        #region Public Fields

        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        #endregion //END Region Public Fields

        #endregion //END Region Fields

        #region Methods

        #region Public Methods
        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks) + " ";

            if (p.IsPK) decl += "primary key ";
            if (p.IsAutoInc) decl += "autoincrement ";
            if (!p.IsNullable) decl += "not null ";
            if (!string.IsNullOrEmpty(p.Collation)) decl += "collate " + p.Collation + " ";

            return decl;
        }

        public static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks) //todo:Add more Type Handling if possible here.
        {
            var clrType = p.ColumnType;



            if ( clrType == typeof (byte) 
                || clrType == typeof (ushort) 
                || clrType == typeof (sbyte) 
                || clrType == typeof (short) 
                || clrType == typeof (int))
            {
                return "integer";
            }

            if (clrType == typeof (uint) 
                || clrType == typeof (long))
            {
                return "bigint";
            }

            if (clrType == typeof (float) 
                || clrType == typeof (double) 
                || clrType == typeof (decimal))
            {
                return "float";
            }

            if (clrType == typeof(bool))
            {
                return "varchar";
            }

            if (clrType == typeof (string))
            {
                int? len = p.MaxStringLength;

                if (len.HasValue) return "varchar(" + len.Value + ")";

                return "varchar";
            }

            //experimental
            if (clrType == typeof (List<string>)) return "varchar";
            //end experimental
            if (clrType == typeof (TimeSpan)) return "bigint";
            if (clrType == typeof (DateTime)) return storeDateTimeAsTicks ? "bigint" : "datetime";
            if (clrType == typeof (DateTimeOffset)) return "bigint";
            if (clrType == typeof (byte[])) return "blob";
            if (clrType == typeof (Guid)) return "varchar(36)";

            
            #if NETFX_CORE //NETFX_CORE is used for Windows Store Builds (METRO)
//            if (clrType.GetTypeInfo().IsEnum) return "integer";
            if (clrType.GetTypeInfo().IsEnum) return "varchar";
            #else
//            if (clrType.IsEnum) return "integer";
            if (clrType.IsEnum) return "varchar";
#endif

            //If no type matches.
            throw new NotSupportedException("Don't know about " + clrType);
        }

        public static bool IsPK(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof (PrimaryKeyAttribute), true);

            return attrs.Any(); //test: This should work for both normal builds and Metro.
        }

        public static string Collation(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof (CollationAttribute), true);

            if (attrs.Any()) return ((CollationAttribute)attrs.First()).Value; //test: This should work for both normal builds and Metro.

            return string.Empty;

        }

        public static bool IsAutoInc(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof (AutoIncrementAttribute), true);

            return attrs.Any(); //test: This should work for both normal builds and Metro.

        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof (IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int? MaxStringLength(PropertyInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof (MaxLengthAttribute), true);

            if(attrs.Any()) return ((MaxLengthAttribute)attrs.First()).Value; //test: This should work for both normal builds and Metro.

            return null;
        }

        public static bool IsMarkedNotNull(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof (NotNullAttribute), true);

            return attrs.Any();  //test: This should work for both normal builds and Metro.

        }

        #endregion //END Region Public Methods

        #endregion //END Region Methods

    } //END Class Object_Relational_Mapping

    #endregion //END Region Classes

} //END Namespace DLS.SQLiteUnity