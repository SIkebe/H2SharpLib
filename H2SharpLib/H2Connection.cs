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
using System.Data.Common;

namespace System.Data.H2
{
    public sealed class H2Connection : DbConnection
    {
        private string _connectionString;
        private string _userName;
        private string _password;
        private H2ConnectionPool _pool;

        static H2Connection() => org.h2.Driver.load();

        public H2Connection() { }

        public H2Connection(string connectionString) => _connectionString = connectionString;

        public H2Connection(string connectionString, string userName, string password)
        {
            _connectionString = connectionString;
            _userName = userName;
            _password = password;
        }

        internal H2Connection(Connection self)
        {
            Connection = self;
            if (H2Helper.GetAdoTransactionLevel(self.getTransactionIsolation()) != IsolationLevel.Unspecified)
            {
                Transaction = new H2Transaction(this);
            }
        }

        internal H2Connection(H2ConnectionPool pool) => _pool = pool;

        internal Connection Connection { get; set; }

        public override string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (IsOpen) { throw new InvalidOperationException(); }
                _connectionString = value;
            }
        }

        public override string Database => throw new NotImplementedException();
        public override string DataSource => throw new NotImplementedException();
        public bool IsOpen => Connection != null;

        public string Password
        {
            get => _password;
            set
            {
                if (IsOpen) { throw new InvalidOperationException(); }
                _password = value;
            }
        }

        public override string ServerVersion => throw new NotImplementedException();

        public override ConnectionState State
        {
            get
            {
                if (IsOpen)
                {
                    return ConnectionState.Open;
                }

                return ConnectionState.Closed;
            }
        }

        internal H2Transaction Transaction { get; set; }

        public string UserName
        {
            get => _userName;
            set
            {
                if (IsOpen) { throw new InvalidOperationException(); }
                _userName = value;
            }
        }

        public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();

        public override void Close() => Dispose(true);

        public override void Open()
        {
            if (_userName == null || _password == null)
            {
                if (IsOpen) { throw new InvalidOperationException("connection is already open"); }
                try
                {
                    if (_pool != null)
                    {
                        Connection = _pool.GetConnection();
                    }
                    else
                    {
                        Connection = java.sql.DriverManager.getConnection(_connectionString);
                    }
                }
                catch (org.h2.jdbc.JdbcSQLException ex)
                {
                    throw new H2Exception(ex);
                }
            }
            else
            {
                Open(_userName, _password);
            }
        }

        public void Open(string userName, string password)
        {
            if (userName == null) { throw new ArgumentNullException(nameof(userName)); }
            if (password == null) { throw new ArgumentNullException(nameof(password)); }
            if (IsOpen) { throw new InvalidOperationException("connection is already open"); }

            try
            {
                if (_pool != null)
                {
                    Connection = _pool.GetConnection(userName, password);
                }
                else
                {
                    Connection = java.sql.DriverManager.getConnection(_connectionString, userName, password);
                }
            }
            catch (org.h2.jdbc.JdbcSQLException ex)
            {
                throw new H2Exception(ex);
            }
        }

        public new H2Command CreateCommand() => new H2Command(this);

        public new H2Transaction BeginTransaction() => BeginTransaction(IsolationLevel.ReadCommitted);

        public new H2Transaction BeginTransaction(IsolationLevel isolationLevel)
        {
            CheckIsOpen();
            if (isolationLevel == IsolationLevel.Unspecified)
            {
                isolationLevel = IsolationLevel.ReadCommitted;
            }

            if (Transaction != null) { throw new InvalidOperationException(); }
            try
            {
                Connection.setTransactionIsolation(H2Helper.GetJdbcTransactionLevel(isolationLevel));
            }
            catch (org.h2.jdbc.JdbcSQLException ex)
            {
                throw new H2Exception(ex);
            }

            Transaction = new H2Transaction(this);
            return Transaction;
        }

        internal void CheckIsOpen()
        {
            if (!IsOpen) { throw new InvalidOperationException("must open the connection first"); }
        }

        protected override DbCommand CreateDbCommand() => CreateCommand();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => BeginTransaction(isolationLevel);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (IsOpen)
                {
                    if (Transaction != null)
                    {
                        Transaction.Dispose();
                        Transaction = null;
                    }

                    if (_pool != null)
                    {
                        _pool.Enqueue(Connection);
                        Connection = null;
                    }
                    else
                    {
                        Connection.close();
                        Connection = null;
                    }
                }

                _userName = null;
                _password = null;
            }
        }
    }
}