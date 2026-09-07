using HttpClient.Utils;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Models
{
    public class QueryArrayExampleTyped<TVal> : IEnumerable<TVal>
    {
        const int MAX_QUERY_ARRAY_LENGTH = 250;
        public TVal[] Values { get; }

        public QueryArrayExampleTyped(TVal[] values)
        {
            if (values.Length > MAX_QUERY_ARRAY_LENGTH)
            {
                throw new Exception("Превышен максимальный размер массива передаваемого в строке запроса");
            }
            Values = values;
        }

        public static implicit operator QueryArrayExampleTyped<TVal>(TVal[] readings)
        {
            return new QueryArrayExampleTyped<TVal>(readings);
        }

        public IEnumerator<TVal> GetEnumerator()
        {
            return Values.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static async ValueTask<QueryArrayExampleTyped<TVal>> BindAsync(HttpContext httpContext, ParameterInfo parameter)
        {
            var result = QuerySerializer.Deserialize(httpContext.Request.Query, parameter.Name, parameter.ParameterType);
            return await ValueTask.FromResult(new QueryArrayExampleTyped<TVal>(result.Values.Cast<TVal>().ToArray()));
        }
    }
}
