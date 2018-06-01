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

using System.Collections.Generic;

using DbParameter = System.Data.Common.DbParameter;
using DbParameterCollection = System.Data.Common.DbParameterCollection;

namespace System.Data.H2
{
    public sealed class H2ParameterCollection : DbParameterCollection, IList<H2Parameter>
    {
        private List<H2Parameter> _parameters = new List<H2Parameter>();

        internal H2ParameterCollection() { }

        public override int Count => _parameters.Count;
        public override bool IsFixedSize => false;
        public override bool IsReadOnly => false;
        public override object SyncRoot => throw new NotImplementedException();
        public override bool IsSynchronized => false;

        public new H2Parameter this[int index]
        {
            get => _parameters[index];
            set => _parameters[index] = value;
        }

        protected override DbParameter GetParameter(string parameterName) 
            => _parameters.Find(delegate (H2Parameter p) { return p.ParameterName == parameterName; });
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            int index = IndexOf(parameterName);
            if (index != -1)
            {
                _parameters[index] = (H2Parameter)value;
            }
        }

        protected override void SetParameter(int index, DbParameter value) 
            => _parameters[index] = (H2Parameter)value;

        public override int Add(object value)
        {
            if (!(value is H2Parameter parameter))
            {
                _parameters.Add(new H2Parameter(value));
            }
            else
            {
                _parameters.Add(parameter);
            }

            return _parameters.Count - 1;
        }
        public override void AddRange(Array values) => throw new NotImplementedException();
        public override void Clear() => _parameters.Clear();
        public override bool Contains(string value) 
            => _parameters.Exists(delegate (H2Parameter p) { return p.ParameterName == value; });
        public override bool Contains(object value) 
            => _parameters.Exists(delegate (H2Parameter p) { return p.Value == value; });
        public override void CopyTo(Array array, int index) => throw new NotImplementedException();
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(string parameterName) 
            => _parameters.FindIndex(delegate (H2Parameter p) { return p.ParameterName == parameterName; });
        public override int IndexOf(object value) 
            => _parameters.FindIndex(delegate (H2Parameter p) { return p.Value == value; });
        public override void Insert(int index, object value) => _parameters.Insert(index, new H2Parameter(value));

        public override void Remove(object value)
        {
            int index = IndexOf(value);
            if (index != -1)
            {
                _parameters.RemoveAt(index);
            }
        }

        public override void RemoveAt(string parameterName)
        {
            int index = IndexOf(parameterName);
            if (index != -1)
            {
                _parameters.RemoveAt(index);
            }
        }

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);

        public int FindIndex(int index, Predicate<H2Parameter> match) => _parameters.FindIndex(index, match);
        public int FindIndex(int index, int count, Predicate<H2Parameter> match) 
            => _parameters.FindIndex(index, count, match);
        public int FindIndex(Predicate<H2Parameter> match) => _parameters.FindIndex(match);
        public H2Parameter Find(Predicate<H2Parameter> match) => _parameters.Find(match);
        public H2Parameter FindLast(Predicate<H2Parameter> match) => _parameters.FindLast(match);
        public int IndexOf(H2Parameter item) => _parameters.IndexOf(item);
        public void Insert(int index, H2Parameter item) => _parameters.Insert(index, item);
        public void Add(H2Parameter item) => _parameters.Add(item);
        public bool Contains(H2Parameter item) => _parameters.Contains(item);
        public void CopyTo(H2Parameter[] array, int arrayIndex) => _parameters.CopyTo(array, arrayIndex);
        public bool Remove(H2Parameter item) => _parameters.Remove(item);
        IEnumerator<H2Parameter> IEnumerable<H2Parameter>.GetEnumerator() => _parameters.GetEnumerator();
    }
}