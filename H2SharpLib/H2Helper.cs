#region MIT License
/*
 * Copyright � 2008 Jonathan Mark Porter.
 * H2Sharp is a wrapper for the H2 Database Engine. http://h2sharp.googlecode.com
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion
using java.sql;
using System.Collections.Generic;
namespace System.Data.H2
{
    static class H2Helper
    {
        private static Dictionary<int, DbType> _jdbc2dbtype;
        private static Dictionary<DbType, int> _dbtype2jdbc;
        private static Dictionary<DbType, Converter> _converters2java;
        private static Dictionary<int, Converter> _converters2clr;
        private static Dictionary<int, Type> _jdbc2type;

        private static void Map(int jdbcType, DbType dbType, Type type, Converter converter2java, Converter converter2clr)
        {
            try
            {
                if (!_jdbc2dbtype.ContainsKey(jdbcType))
                    _jdbc2dbtype[jdbcType] = dbType;

                if (!_dbtype2jdbc.ContainsKey(dbType))
                    _dbtype2jdbc[dbType] = jdbcType;

                if (type != null && !_jdbc2type.ContainsKey(jdbcType))
                    _jdbc2type[jdbcType] = type;

                if (!_converters2java.ContainsKey(dbType))
                    _converters2java[dbType] = converter2java;

                if (!_converters2clr.ContainsKey(jdbcType))
                    _converters2clr[jdbcType] = converter2clr;
            }
            catch (Exception) { }
        }

        static H2Helper()
        {
            _jdbc2dbtype = new Dictionary<int, DbType>();
            _dbtype2jdbc = new Dictionary<DbType, int>();
            _converters2java = new Dictionary<DbType, Converter>();
            _converters2clr = new Dictionary<int, Converter>();
            _jdbc2type = new Dictionary<int, Type>();

            object id(object x) => x;

            Map(Types.VARCHAR, DbType.AnsiString, typeof(string), id, id);
            Map(Types.CHAR, DbType.AnsiStringFixedLength, typeof(string), id, id);
            Map(Types.LONGVARBINARY, DbType.Binary, typeof(byte[]),                id, id);
            Map(Types.BINARY, DbType.Binary, typeof(byte[]), id, id);
            Map(Types.BOOLEAN, DbType.Boolean, typeof(bool),
                x => new java.lang.Boolean((bool)x),
                x => ((java.lang.Boolean)x).booleanValue()
            );

            Map(Types.TINYINT, DbType.Byte, typeof(byte),
                x => new java.lang.Byte((byte)x),
                x => ((java.lang.Byte)x).byteValue()
            );

            Map(Types.DATE, DbType.Date, typeof(DateTime),
                x => new java.sql.Date((long)(((DateTime)x) - UTCStart).TotalMilliseconds),
                x => UTCStart.AddMilliseconds(((java.sql.Date)x).getTime())
            );

            Map(Types.TIMESTAMP, DbType.DateTime, typeof(DateTime),
                x => new java.sql.Timestamp((long)(((DateTime)x) - UTCStart).TotalMilliseconds),
                x => UTCStart.AddMilliseconds(((java.util.Date)x).getTime())
            );

            Map(Types.TIMESTAMP, DbType.DateTime2, typeof(DateTime),
                x => new java.sql.Timestamp((long)(((DateTime)x) - UTCStart).TotalMilliseconds),
                x => UTCStart.AddMilliseconds(((java.sql.Date)x).getTime())
            );

            Map(Types.TIMESTAMP, DbType.DateTimeOffset, typeof(DateTimeOffset),
                x => new java.sql.Timestamp((long)(((DateTimeOffset)x) - UTCStart).TotalMilliseconds),
                x => (DateTimeOffset)UTCStart.AddMilliseconds(((java.sql.Date)x).getTime())
            );

            Map(Types.DECIMAL, DbType.Decimal, typeof(decimal),
                //TODO: test me !
                x => new java.math.BigDecimal(((decimal)x).ToString()),
                x => decimal.Parse(((java.math.BigDecimal)x).toString())
            );

            Map(Types.DOUBLE, DbType.Double, typeof(double),
                x => new java.lang.Double((double)x),
                x => ((java.lang.Double)x).doubleValue()
            );

            Map(Types.SMALLINT, DbType.Int16, typeof(short),
                x => new java.lang.Short((short)x),
                x => ((java.lang.Short)x).shortValue()
            );

            Map(Types.INTEGER, DbType.Int32, typeof(int),
                x => new java.lang.Integer((int)x),
                x => ((java.lang.Integer)x).intValue()
            );

            Map(Types.BIGINT, DbType.Int64, typeof(long),
                x => new java.lang.Long((long)x),
                x => ((java.lang.Long)x).longValue()
            );

            Map(Types.SMALLINT, DbType.UInt16, typeof(ushort),
                x => new java.lang.Short((short)(ushort)x),
                x => (ushort)((java.lang.Short)x).shortValue()
            );

            Map(Types.INTEGER, DbType.UInt32, typeof(uint),
                x => new java.lang.Integer((int)(uint)x),
                x => (uint)((java.lang.Integer)x).intValue()
            );

            Map(Types.BIGINT, DbType.UInt64, typeof(ulong),
                x => new java.lang.Long((long)(ulong)x),
                x => (ulong)((java.lang.Long)x).longValue()
            );

            Map(Types.JAVA_OBJECT, DbType.Object, typeof(Object), id, id);
            Map(Types.TINYINT, DbType.SByte, typeof(byte),
                x => new java.lang.Byte((byte)x),
                x => ((java.lang.Byte)x).byteValue()
            );

            Map(Types.FLOAT, DbType.Single, typeof(float),
                x => new java.lang.Float((float)x),
                x => ((java.lang.Float)x).floatValue()
            );

            Map(Types.NVARCHAR, DbType.String, typeof(string), id, id);
            Map(Types.NCHAR, DbType.StringFixedLength, typeof(string), id, id);
            Map(Types.TIME, DbType.Time, typeof(DateTime),
                x => new java.sql.Timestamp((long)(((DateTime)x) - UTCStart).TotalMilliseconds),
                x => UTCStart.AddMilliseconds(((java.sql.Date)x).getTime())
            );

            Map(Types.ARRAY, DbType.VarNumeric, null, id, id);
            //DbType.Guid:
            //DbType.Currency:
        }

        public static int GetTypeCode(DbType dbType)
        {
            if (!_dbtype2jdbc.TryGetValue(dbType, out int ret))
            {
                throw new NotSupportedException("Cannot convert the ADO.NET " + Enum.GetName(typeof(DbType), dbType) + " " + typeof(DbType).Name + " to a JDBC type");
            }

            return ret;
        }

        public static DbType GetDbType(int typeCode)
        {
            if (!_jdbc2dbtype.TryGetValue(typeCode, out DbType ret))
            {
                throw new NotSupportedException("Cannot convert JDBC type " + typeCode + " to an ADO.NET " + typeof(DbType).Name);
            }

            return ret;
        }

        public static Converter ConverterToJava(DbType dbType)
        {
            if (!_converters2java.TryGetValue(dbType, out Converter ret))
            {
                throw new NotSupportedException("Cannot find a converter from the ADO.NET " + Enum.GetName(typeof(DbType), dbType) + " " + typeof(DbType).Name + " to a JDBC type");
            }

            return ret;
        }

        public static Converter ConverterToCLR(int typeCode)
        {
            if (!_converters2clr.TryGetValue(typeCode, out Converter ret))
            {
                throw new NotSupportedException("Cannot find a converter the JDBC type " + typeCode + " to an ADO.NET " + typeof(DbType).Name);
            }

            return ret;
        }

        public static IsolationLevel GetAdoTransactionLevel(int level)
        {
            switch (level)
            {
                case java.sql.Connection.__Fields.TRANSACTION_NONE:
                    return IsolationLevel.Unspecified;
                case java.sql.Connection.__Fields.TRANSACTION_READ_COMMITTED:
                    return IsolationLevel.ReadCommitted;
                case java.sql.Connection.__Fields.TRANSACTION_READ_UNCOMMITTED:
                    return IsolationLevel.ReadUncommitted;
                case java.sql.Connection.__Fields.TRANSACTION_REPEATABLE_READ:
                    return IsolationLevel.RepeatableRead;
                case java.sql.Connection.__Fields.TRANSACTION_SERIALIZABLE:
                    return IsolationLevel.Serializable;
                default:
                    throw new NotSupportedException("unsupported transaction level");
            }
        }

        public static int GetJdbcTransactionLevel(IsolationLevel level)
        {
            switch (level)
            {
                case IsolationLevel.Unspecified:
                    return java.sql.Connection.__Fields.TRANSACTION_NONE;
                case IsolationLevel.ReadCommitted:
                    return java.sql.Connection.__Fields.TRANSACTION_READ_COMMITTED;
                case IsolationLevel.ReadUncommitted:
                    return java.sql.Connection.__Fields.TRANSACTION_READ_UNCOMMITTED;
                case IsolationLevel.RepeatableRead:
                    return java.sql.Connection.__Fields.TRANSACTION_REPEATABLE_READ;
                case IsolationLevel.Serializable:
                    return java.sql.Connection.__Fields.TRANSACTION_SERIALIZABLE;
                default:
                    throw new NotSupportedException("unsupported transaction level");
            }
        }

        public delegate Object Converter(Object o);
        static readonly DateTime UTCStart = new DateTime(1970, 1, 1).ToLocalTime().AddHours(1); // TODO fix me !!!

        // TODO check this is complete
        public static Type GetType(int typeCode)
        {
            if (!_jdbc2type.TryGetValue(typeCode, out Type ret))
            {
                throw new NotSupportedException("Cannot convert JDBC type " + typeCode + " to a CLR type");
            }

            return ret;
        }
    }
}