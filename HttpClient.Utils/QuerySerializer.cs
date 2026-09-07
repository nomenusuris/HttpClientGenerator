using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HttpClient.Utils
{
    /// <summary>
    /// Allows to pass and receive nested objects with query string
    /// </summary>
    /// <remarks>
    /// Remember:
    /// <list type="bullet">
    /// <item>Enums passed by name</item>
    /// <item>Arrays should be indexed (only last level arrays may not have indicies)</item>
    /// <item>Arrays usually deserialize as dictionaries with indecies as keys (only last level arrays parsed as arrays)</item>
    /// </list>
    /// </remarks>
    public static partial class QuerySerializer
    {
        private static readonly string QUERY_PATH_PART_PATTERN = @"([0-9a-zA-Z_\-~]+)";

        /// <param name="obj"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <remarks>Transforms objects like 
        /// <list type="bullet"><item>{ "users": [{ "login": "abc", "phone": "123" }, { "login": "efg", "phone": "456", "address": {"city":"sample"} } ]}</item></list>
        /// to
        /// <list type="bullet">
        /// <item>users[0][login]=abc</item>
        /// <item>users[1][phone]=123</item>
        /// <item>users[1][login]=efg</item>
        /// <item>users[1][phone]=456</item>
        /// <item>users[1][address][city]=sample</item>
        /// </list>
        /// </remarks>
        public static IDictionary<string, StringValues> Serialize(object obj, JsonSerializerOptions jsonSerializerOptions)
        {
            if (obj == null)
            {
                return new Dictionary<string, StringValues>();
            }

            var jsonStr = JsonSerializer.Serialize(obj, jsonSerializerOptions);
            var jsonNode = JsonNode.Parse(jsonStr);
            var flatten = new Dictionary<string, StringValues>();
            FlattenRecursive(jsonNode, "", flatten);
            return flatten;
        }

        /// <summary>
        /// Flattens json object to dictionary using bracket notation with explicit Indices
        /// </summary>
        /// <param name="node">Current node</param>
        /// <param name="prefix">Current prefix</param>
        /// <param name="flatDictionary">Result</param>
        private static void FlattenRecursive(JsonNode node, string prefix, Dictionary<string, StringValues> flatDictionary)
        {
            if (node is JsonObject obj)
            {
                foreach (var property in obj)
                {
                    var newPrefix = string.IsNullOrEmpty(prefix) ? property.Key : $"{prefix}[{property.Key}]";
                    FlattenRecursive(property.Value, newPrefix, flatDictionary);
                }
            }
            else if (node is JsonArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var newPrefix = string.IsNullOrEmpty(prefix) ? $"[{i}]" : $"{prefix}[{i}]";
                    FlattenRecursive(array[i], newPrefix, flatDictionary);
                }
            }
            else if (node is JsonValue value)
            {
                switch (value.GetValueKind())
                {
                    case JsonValueKind.String:
                        flatDictionary[prefix] = (string)value;
                        break;
                    case JsonValueKind.Null:
                        break;
                    default:
                        flatDictionary[prefix] = value.ToString();
                        break;
                }
            }
        }

        public static IDictionary<string, object> Deserialize(IQueryCollection query, string parentPath, Type parentType)
        {
            var flattenedQuery = query.SelectMany(q => q.Value.Select(v => (q.Key, v)));
            return Deserialize(flattenedQuery, parentPath, parentType);
        }

        private static IDictionary<string, object> Deserialize(IEnumerable<(string path, string value)> flattenedQuery, string parentPath, Type parentType)
        {
            var parameters = flattenedQuery
                .Where(p => string.Join(".", GetPathParts(p.path))
                        .StartsWith($"{parentPath}"));
            var result = new Dictionary<string, object>();
            var buffer = new List<(string key, object value)>();
            var parentPathParts = GetPathParts(parentPath);
            var processedProperties = new List<string>();

            foreach (var parameter in parameters)
            {
                var parameterPath = GetPathParts(parameter.path);
                var currentPropertyName = parameterPath.Skip(parentPathParts.Length).First();
                if (processedProperties.Contains(currentPropertyName))
                {
                    continue;
                }
                processedProperties.Add(currentPropertyName);

                var parameterType = GetNextPropertyType(parentType, currentPropertyName);
                if (parameterPath.Count() - parentPathParts.Count() == 1)
                {
                    var value = ParsePrimitive(parameterType, parameter.value);
                    buffer.Add((currentPropertyName, value));
                    continue;
                }
                var deserialized = Deserialize(parameters, $"{parentPath}.{currentPropertyName}", parameterType);
                object subObject = deserialized;
                buffer.Add((currentPropertyName, subObject));
            }

            // merge to array
            var objectKeys = buffer.Select(r => r.key).Distinct();
            foreach (var objectKey in objectKeys)
            {
                var values = buffer
                    .Where(b => b.key == objectKey)
                    .Select(b => b.value);
                if (values.Count() > 1)
                {
                    result.Add(objectKey, values.ToArray());
                }
                else
                {
                    result.Add(objectKey, values.First());
                }
            }
            return result;
        }

        private static Type GetNextPropertyType(Type from, string propertyName)
        {
            if (IsEnumerable(from) && uint.TryParse(propertyName, out var _))
            {
                return GetElementType(from);
            }
            var type = from
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(pi => pi.Name == propertyName)?
                .PropertyType;
            return type;
        }

        private static Type GetElementType(Type type)
        {
            if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var genericArg = type.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Single(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .GetGenericArguments()
                    .First();
                return genericArg;
            }
            return type;
        }

        private static object ParsePrimitive(Type currentType, string strValue)
        {
            if (currentType != typeof(string) && IsEnumerable(currentType))
            {
                currentType = GetElementType(currentType);
            }
            currentType = currentType ?? typeof(string);
            var underlyingType = Nullable.GetUnderlyingType(currentType);
            var isNullable = underlyingType != null || currentType.IsByRef;
            if (isNullable)
            {
                currentType = underlyingType ?? currentType;
            }

            if (isNullable && string.IsNullOrWhiteSpace(strValue))
            {
                return null;
            }

            if (currentType?.IsEnum ?? false)
            {
                return Enum.Parse(currentType, strValue);
            }

            TypeConverter converter = TypeDescriptor.GetConverter(currentType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                var result = converter.ConvertFrom(strValue);
                if (result is DateTime date)
                {
                    return date.ToUniversalTime();
                }
                return result;
            }

            throw new InvalidOperationException("Unsupported query type");
            //if ((currentType == typeof(long?) || currentType == typeof(long)) && long.TryParse(strValue, out var longVal))
            //{
            //    val = longVal;
            //}
            //else if ((currentType == typeof(ulong?) || currentType == typeof(ulong)) && ulong.TryParse(strValue, out var ulongVal))
            //{
            //    val = ulongVal;
            //}
            //else if ((currentType == typeof(decimal?) || currentType == typeof(decimal)) && decimal.TryParse(strValue, out var decimalVal))
            //{
            //    val = decimalVal;
            //}
            //else if ((currentType == typeof(int?) || currentType == typeof(int)) && int.TryParse(strValue, out var intVal))
            //{
            //    val = intVal;
            //}
            //else if ((currentType == typeof(uint?) || currentType == typeof(uint)) && uint.TryParse(strValue, out var uintVal))
            //{
            //    val = uintVal;
            //}
            //else if ((currentType == typeof(float?) || currentType == typeof(float)) && float.TryParse(strValue, out var floatVal))
            //{
            //    val = floatVal;
            //}
            //else if (typeof(DateTime?).IsAssignableFrom(currentType) && DateTime.TryParse(strValue, out var dateVal))
            //{
            //    val = dateVal.ToUniversalTime();
            //}
            //else if (typeof(Guid?).IsAssignableFrom(currentType) && Guid.TryParse(strValue, out var guidVal))
            //{
            //    val = guidVal;
            //}
            //else if (typeof(bool?).IsAssignableFrom(currentType) && bool.TryParse(strValue, out var boolVal))
            //{
            //    val = boolVal;
            //}
            //else
            //else
            //{
            //    val = strValue;
            //}
            //return val;
        }

        private static string[] GetPathParts(string path)
        {
            var parts = new List<string>();
            var matches = Regex.Matches(path, QUERY_PATH_PART_PATTERN);
            for (int i = 0; i < matches.Count; i++)
            {
                parts.Add(matches[i].Groups[0].Value);
            }
            return parts.ToArray();
        }

        private static bool IsEnumerable(Type type)
        {
            if (type == null || type == typeof(string))
            {
                return false;
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return false;
            }
            return typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}