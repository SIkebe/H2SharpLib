using System.Collections.Generic;
using System.Text;
using java.sql;

using DbCommand = System.Data.Common.DbCommand;
using DbConnection = System.Data.Common.DbConnection;
using DbDataReader = System.Data.Common.DbDataReader;
using DbParameter = System.Data.Common.DbParameter;
using DbParameterCollection = System.Data.Common.DbParameterCollection;
using DbTransaction = System.Data.Common.DbTransaction;

namespace System.Data.H2
{
    public sealed class H2Command : DbCommand
    {
        #region sub classes

        class PreparedTemplate
        {
            public PreparedTemplate(string oldSql, string trueSql, int[] mapping)
            {
                OldSql = oldSql;
                TrueSql = trueSql;
                Mapping = mapping;
            }

            public string OldSql { get; }
            public string TrueSql { get; }
            public int[] Mapping { get; }
        }

        #endregion

        #region static

        private static Dictionary<string, PreparedTemplate> templates = new Dictionary<string, PreparedTemplate>();
        private static readonly object syncRoot = new object();

        private static int[] CreateRange(int length)
        {
            int[] result = new int[length];
            for (int index = 0; index < length; ++index)
            {
                result[index] = index;
            }

            return result;
        }

        #endregion

        #region fields

        private string _commandText;
        private int _commandTimeout = 30;
        private bool _timeoutSet;
        private PreparedStatement _statement;
        private PreparedTemplate _template;

        #endregion

        #region constructors

        public H2Command()
            : this(null, null, null)
        { }

        public H2Command(H2Connection connection)
            : this(null, connection, null)
        { }

        public H2Command(string commandText)
            : this(commandText, null, null)
        { }

        public H2Command(string commandText, H2Connection connection)
            : this(commandText, connection, null)
        { }

        public H2Command(string commandText, H2Connection connection, H2Transaction transaction)
        {
            _commandText = commandText;
            Connection = connection;
            Parameters = new H2ParameterCollection();
            UpdatedRowSource = UpdateRowSource.None;
        }
        #endregion

        #region properties

        public new H2Connection Connection { get; set; }
        public new H2ParameterCollection Parameters { get; }
        public new H2Transaction Transaction
        {
            get
            {
                if (Connection == null) { return null; }
                return Connection.Transaction;
            }
            set
            {
                if (value == null) return;
                Connection = value.Connection;
            }
        }

        protected override DbConnection DbConnection
        {
            get => Connection;
            set => Connection = (H2Connection)value;
        }

        protected override DbParameterCollection DbParameterCollection => Parameters;

        protected override DbTransaction DbTransaction
        {
            get => Transaction;
            set => Transaction = (H2Transaction)value;
        }

        public override string CommandText
        {
            get => _commandText;
            set => _commandText = value;
        }

        public override int CommandTimeout
        {
            get => _commandTimeout;
            set
            {
                _timeoutSet = true;
                _commandTimeout = value;
            }
        }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        public bool DisableNamedParameters { get; set; } = false;

        private bool IsNamed
        {
            get
            {
                if (DisableNamedParameters) { return false; }

                bool inQuote = false;
                for (int index = 0; index < _commandText.Length; ++index)
                {
                    char c = _commandText[index];
                    if (!inQuote && c == '@')
                    {
                        return true;
                    }
                    else if (c == '\'')
                    {
                        inQuote = !inQuote;
                    }
                }

                return false;
            }
        }
        #endregion

        #region methods

        private void CheckConnection()
        {
            if (Connection == null) { throw new H2Exception("DbConnection must be set."); }
            Connection.CheckIsOpen();
        }

        private PreparedTemplate CreateNameTemplate()
        {
            var list = new List<int>();
            var command = new StringBuilder();
            var name = new StringBuilder();
            bool inQuote = false;

            for (int index = 0; index < _commandText.Length; ++index)
            {
                char c = _commandText[index];
                if (name.Length == 0)
                {
                    if (!inQuote && c == '@')
                    {
                        name.Append(c);
                    }
                    else
                    {
                        if (c == '\'')
                        {
                            inQuote = !inQuote;
                        }

                        command.Append(c);
                    }
                }
                else
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        name.Append(c);
                    }
                    else
                    {
                        command.Append('?');
                        command.Append(c);
                        string paramName = name.ToString();
                        name.Length = 0;
                        int paramIndex = Parameters.FindIndex(p => p.ParameterName == paramName);

                        if (paramIndex == -1) { throw new H2Exception($"Missing Parameter: {paramName}"); }
                        list.Add(paramIndex);
                    }
                }
            }

            return new PreparedTemplate(_commandText, command.ToString(), list.ToArray());
        }

        private PreparedTemplate CreateIndexTemplate()
        {
            int count = 0;
            int index = -1;
            while ((index = _commandText.IndexOf('?', index + 1)) != -1)
            {
                count++;
            }

            return new PreparedTemplate(_commandText, _commandText, CreateRange(count));
        }

        private void CreateStatement()
        {
            if (_statement != null)
            {
                _statement.close();
            }

            try
            {
                _statement = Connection.Connection.prepareStatement(_template.TrueSql);
            }
            catch (org.h2.jdbc.JdbcSQLException ex)
            {
                throw new H2Exception(ex);
            }

            if (_timeoutSet)
            {
                _statement.setQueryTimeout(_commandTimeout);
            }
        }

        private void EnsureStatment()
        {
            if (_commandText == null) { throw new InvalidOperationException("must set CommandText"); }
            if (_template == null || _template.OldSql != _commandText)
            {
                lock (syncRoot)
                {
                    if (!templates.TryGetValue(_commandText, out _template))
                    {
                        if (IsNamed)
                        {
                            _template = CreateNameTemplate();
                        }
                        else
                        {
                            _template = CreateIndexTemplate();
                        }

                        templates.Add(_commandText, _template);
                    }
                }

                CreateStatement();
            }
            else
            {
                _statement.clearParameters();
            }

            for (int index = 0; index < _template.Mapping.Length; ++index)
            {
                Parameters[_template.Mapping[index]].SetStatement(index + 1, _statement);
            }
        }

        protected override DbParameter CreateDbParameter() => new H2Parameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => ExecuteReader(behavior);

        public new H2Parameter CreateParameter() => new H2Parameter();

        public new H2DataReader ExecuteReader() => ExecuteReader(CommandBehavior.Default);

        public new H2DataReader ExecuteReader(CommandBehavior behavior)
        {
            // TODO check this : if (behavior != CommandBehavior.Default) { throw new NotSupportedException("Only CommandBehavior Default is supported for now."); }
            Prepare();
            try
            {
                var low = CommandText.ToLower().Trim();
                var iSemi = low.IndexOf(';');
                if ((low.StartsWith("insert") || low.StartsWith("update")) && (iSemi < 0 || iSemi == low.Length - 1))
                {
                    _statement.executeUpdate();
                    return null;
                }
                else
                {
                    return new H2DataReader(Connection, _statement.executeQuery());
                }
            }
            catch (org.h2.jdbc.JdbcSQLException ex)
            {
                ex.printStackTrace();
                throw new H2Exception(ex);
            }
        }

        public override void Cancel()
        {
            CheckConnection();
            if (_statement != null)
            {
                try
                {
                    _statement.cancel();
                }
                catch (org.h2.jdbc.JdbcSQLException ex)
                {
                    throw new H2Exception(ex);
                }
            }
        }

        public override int ExecuteNonQuery()
        {
            Prepare();
            try
            {
                return _statement.executeUpdate();
            }
            catch (org.h2.jdbc.JdbcSQLException ex)
            {
                throw new H2Exception(ex);
            }
        }

        public override object ExecuteScalar()
        {
            Prepare();
            object result = null;
            try
            {
                ResultSet set = _statement.executeQuery();
                try
                {
                    if (set.next())
                    {
                        result = set.getObject(1);
                        if (result == null)
                        {
                            result = DBNull.Value;
                        }
                        else
                        {
                            result = H2Helper.ConverterToCLR(set.getMetaData().getColumnType(1))(result);
                        }
                    }
                }
                finally
                {
                    set.close();
                }

                return result;
            }
            catch (org.h2.jdbc.JdbcSQLException ex)
            {
                throw new H2Exception(ex);
            }
        }

        public override void Prepare()
        {
            CheckConnection();
            EnsureStatment();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_statement != null)
                {
                    _statement.close();
                    _statement = null;
                }
            }
        }
        #endregion
    }
}