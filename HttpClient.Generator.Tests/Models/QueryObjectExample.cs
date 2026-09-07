using HttpClient.Utils;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Models
{
    public class QueryObjectExample : IDictionary<string, object>
    {
        private const int CAPACITY = 250;
        private readonly IQueryCollection _sourceQuery;
        protected readonly Dictionary<string, object> _internalDictionary;

        /// <summary>
        /// Returns true if all indicies is integers
        /// </summary>
        [IgnoreDataMember]
        public virtual bool IsArray => _internalDictionary.Keys.All(key => int.TryParse(key, out var _));

        public QueryObjectExample()
        {
            _internalDictionary = new Dictionary<string, object>();
        }

        public QueryObjectExample(IDictionary<string, object> dictionary) : this()
        {
            _internalDictionary = dictionary?.ToDictionary() ?? _internalDictionary;
        }

        public QueryObjectExample(IEnumerable<object> values) : this(values?.Select((val, idx) => KeyValuePair.Create(idx.ToString(), val))?.ToDictionary())
        {
        }

        public QueryObjectExample(IQueryCollection queryCollection) : this()
        {
            if (queryCollection.Count > CAPACITY)
            {
                throw new Exception("Dictionary exceeds CAPACITY");
            }
            _sourceQuery = queryCollection;
        }

        protected object this[string key]
        {
            get => Get(key);

            set => Set(key, value);
        }

        public object Get(string key)
        {
            if (!_internalDictionary.ContainsKey(key))
            {
                return null;
            }
            return _internalDictionary[key];
        }

        public void Set(string key, object val)
        {
            _internalDictionary[key] = val;
            if (_internalDictionary.Count > CAPACITY)
            {
                throw new Exception("Dictionary exceeds CAPACITY");
            }
        }

        public T[] GetArray<T>(string key)
        {
            if (!_internalDictionary.ContainsKey(key))
            {
                return null;
            }
            return (_internalDictionary[key] as IDictionary<string, object>)
                .Values
                .Cast<T>()
                .ToArray();
        }


        public void SetArray<T>(string key, T[] array)
        {
            _internalDictionary[key] = new QueryObjectExample();
            foreach (var (idx, el) in array.Select((el, idx) => (idx, el)))
            {
                (_internalDictionary[key] as QueryObjectExample).Add(idx.ToString(), el);
            }
        }

        public static ValueTask<QueryObjectExample> BindAsync(HttpContext httpContext, ParameterInfo parameter)
        {
            var parsedObject = QuerySerializer.Deserialize(httpContext.Request.Query, parameter.Name, parameter.ParameterType);
            var result = new QueryObjectExample(httpContext.Request.Query);
            foreach (var (key, parsedValue) in parsedObject)
            {
                result.Add(KeyValuePair.Create(key, parsedValue));
            }
            return ValueTask.FromResult(result);
        }

        public T[] ToArray<T>()
        {
            return _internalDictionary.Values.Cast<T>().ToArray();
        }

        #region IDictionary

        [IgnoreDataMember]
        public int Count => _internalDictionary.Count;

        [IgnoreDataMember]
        public bool IsReadOnly => false;

        [IgnoreDataMember]
        public IEnumerable<object> Values => _internalDictionary.Values;

        [IgnoreDataMember]
        public ICollection<string> Keys => ((IDictionary<string, object>)_internalDictionary).Keys;

        ICollection<object> IDictionary<string, object>.Values => ((IDictionary<string, object>)_internalDictionary).Values;

        object IDictionary<string, object>.this[string key] { get => ((IDictionary<string, object>)_internalDictionary)[key]; set => ((IDictionary<string, object>)_internalDictionary)[key] = value; }

        public void Add(string key, object value)
        {
            _internalDictionary.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _internalDictionary.Add(item.Key, item.Value);
        }

        public bool Remove(string key)
        {
            return _internalDictionary.Remove(key);
        }

        public void Clear()
        {
            _internalDictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _internalDictionary.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _internalDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            return ((IDictionary<string, object>)_internalDictionary).TryGetValue(key, out value);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_internalDictionary).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)_internalDictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_internalDictionary).GetEnumerator();
        }

        #endregion
    }


    /// <summary>
    /// <see cref="QueryObjectExample"/> with binding from query string to <typeparamref name="TSelf"/>
    /// </summary>
    public class QueryObjectExampleTyped<TSelf> : QueryObjectExample
        where TSelf : IDictionary<string, object>, new()
    {
        public QueryObjectExampleTyped() : base()
        {
        }

        public QueryObjectExampleTyped(IDictionary<string, object> dictionary) : base(dictionary)
        {
        }

        public static new ValueTask<TSelf> BindAsync(HttpContext httpContext, ParameterInfo parameter)
        {
            var parsedObject = QuerySerializer.Deserialize(httpContext.Request.Query, parameter.Name, parameter.ParameterType);
            var result = new TSelf();
            foreach (var (key, parsedValue) in parsedObject)
            {
                result.Add(KeyValuePair.Create(key, parsedValue));
            }
            return ValueTask.FromResult(result);
        }


    }
}
