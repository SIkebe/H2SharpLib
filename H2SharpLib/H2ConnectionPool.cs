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

using System.Threading;
using System.Collections.Generic;
using java.sql;

namespace System.Data.H2
{
    /// <summary>
    /// Pools connections so that applications that opens and closes connections
    /// rapidly will not suffer.
    /// </summary>
    /// <remarks>It does not use the connection pool built into H2.</remarks>
    public sealed class H2ConnectionPool : IDisposable
    {
        #region fields

        private readonly object _syncRoot = new object();
        private readonly string _connectionString;
        private string _userName;
        private string _password;
        private bool _inTimeout;
        private bool _isDisposed;
        private int _currentCount;
        private ManualResetEvent _waitHandle = new ManualResetEvent(false);
        private Queue<Connection> _avaliable = new Queue<Connection>();

        #endregion

        #region constructors

        /// <summary>
        /// Creates a new H2ConnectionPool Instance.
        /// </summary>
        /// <param name="connectionString">connection string</param>
        public H2ConnectionPool(string connectionString) : this(connectionString, null, null) { }

        /// <summary>
        /// Creates a new H2ConnectionPool Instance.
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <param name="userName">username for the database</param>
        /// <param name="password">password for the database</param>
        public H2ConnectionPool(string connectionString, string userName, string password)
        {
            _connectionString = connectionString;
            _userName = userName;
            _password = password;
        }

        #endregion

        #region properties

        /// <summary>
        /// The maximum number of connections that this pool will have open at the same time.
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// The amount of time after all connections are no longer in use that a connection 
        /// gets closed, repeating until all connections are closed.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 2000;

        #endregion

        #region methods

        internal Connection GetConnection() => GetConnection(_userName, _password);

        internal Connection GetConnection(string userName, string password)
        {
            lock (_syncRoot)
            {
                if (_isDisposed) { throw new ObjectDisposedException(GetType().Name); }

                _waitHandle.Set();
                if (_avaliable.Count > 0)
                {
                    return _avaliable.Dequeue();
                }
                else
                {
                    if (_currentCount < MaxConnections)
                    {
                        Connection connection = DriverManager.getConnection(_connectionString, userName, password);
                        _currentCount++;
                        return connection;
                    }
                    else
                    {
                        Monitor.Wait(_syncRoot);
                        if (_avaliable.Count > 0)
                        {
                            return _avaliable.Dequeue();
                        }
                        else
                        {
                            throw new ObjectDisposedException(GetType().Name);
                        }
                    }
                }
            }
        }

        internal void Enqueue(Connection connection)
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    connection.close();
                    _currentCount--;
                    return;
                }

                connection.clearWarnings();
                _avaliable.Enqueue(connection);
                Monitor.Pulse(_syncRoot);
                RegisterTimout();
            }
        }

        void RegisterTimout()
        {
            if (!_inTimeout &&
                _avaliable.Count == _currentCount &&
                _currentCount > 0)
            {
                _inTimeout = true;
                ThreadPool.RegisterWaitForSingleObject(_waitHandle, TimeoutCallback, null, ConnectionTimeout, true);
            }
        }

        void TimeoutCallback(object state, bool timedout)
        {
            lock (_syncRoot)
            {
                _inTimeout = false;
                if (!timedout || _isDisposed) { return; }
                if (_avaliable.Count > 0)
                {
                    Connection connection = _avaliable.Dequeue();
                    connection.close();
                    _currentCount--;
                    RegisterTimout();
                }
            }
        }

        /// <summary>
        /// Gets a connection that will open from this pool.
        /// </summary>
        /// <returns>A H2Connection</returns>
        public H2Connection CreateConnection() => new H2Connection(this);

        /// <summary>
        /// Closes the pool and all its connections.
        /// </summary>
        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    Monitor.PulseAll(_syncRoot);
                    foreach (Connection con in _avaliable)
                    {
                        con.close();
                        _currentCount--;
                    }

                    _avaliable.Clear();
                    _waitHandle.Set();
                    _waitHandle.Close();
                    _userName = null;
                    _password = null;
                }
            }
        }
        #endregion
    }
}