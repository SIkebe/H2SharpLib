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

using System.Data.Common;

namespace System.Data.H2
{
    public sealed class H2DataAdapter : DbDataAdapter, IDbDataAdapter
    {
        private static readonly object _eventRowUpdated = new object();
        private static readonly object _eventRowUpdating = new object();

        public event EventHandler<H2RowUpdatingEventArgs> RowUpdating
        {
            add { Events.AddHandler(_eventRowUpdating, value); }
            remove { Events.RemoveHandler(_eventRowUpdating, value); }
        }

        public event EventHandler<H2RowUpdatedEventArgs> RowUpdated
        {
            add { Events.AddHandler(_eventRowUpdated, value); }
            remove { Events.RemoveHandler(_eventRowUpdated, value); }
        }

        public H2DataAdapter()
        {
        }

        public H2DataAdapter(H2Command selectCommand)
        {
            SelectCommand = selectCommand;
        }

        public H2DataAdapter(string selectCommandText, string selectConnectionString)
            : this(selectCommandText, new H2Connection(selectConnectionString))
        { }

        public H2DataAdapter(string selectCommandText, H2Connection selectConnection)
        {
            SelectCommand = selectConnection.CreateCommand();
            SelectCommand.CommandText = selectCommandText;
        }
        
        public new H2Command SelectCommand { get; set; }
        IDbCommand IDbDataAdapter.SelectCommand
        {
            get { return SelectCommand; }
            set { SelectCommand = (H2Command)value; }
        }

        public new H2Command InsertCommand { get; set; }
        IDbCommand IDbDataAdapter.InsertCommand
        {
            get { return InsertCommand; }
            set { InsertCommand = (H2Command)value; }
        }

        public new H2Command UpdateCommand { get; set; }
        IDbCommand IDbDataAdapter.UpdateCommand
        {
            get { return UpdateCommand; }
            set { UpdateCommand = (H2Command)value; }
        }

        public new H2Command DeleteCommand { get; set; }
        IDbCommand IDbDataAdapter.DeleteCommand
        {
            get { return DeleteCommand; }
            set { DeleteCommand = (H2Command)value; }
        }

        override protected RowUpdatedEventArgs CreateRowUpdatedEvent(
            DataRow dataRow,
            IDbCommand command, 
            StatementType statementType, 
            DataTableMapping tableMapping) 
            => new H2RowUpdatedEventArgs(dataRow, command, statementType, tableMapping);

        override protected RowUpdatingEventArgs CreateRowUpdatingEvent(
            DataRow dataRow,
            IDbCommand command,
            StatementType statementType,
            DataTableMapping tableMapping) 
            => new H2RowUpdatingEventArgs(dataRow, command, statementType, tableMapping);

        override protected void OnRowUpdating(RowUpdatingEventArgs value) 
            => ((EventHandler<H2RowUpdatingEventArgs>)Events[_eventRowUpdating])?
            .Invoke(this, (H2RowUpdatingEventArgs)value);

        override protected void OnRowUpdated(RowUpdatedEventArgs value) 
            => ((EventHandler<H2RowUpdatedEventArgs>)Events[_eventRowUpdated])?
            .Invoke(this, (H2RowUpdatedEventArgs)value);
    }
}