using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HttpClient.Utils
{
    public static class RouteHelper
    {

        private const string PARAMETER_PATTERN = @"\{(?<key>[^}]+?)(?<type>\:[^}]+)?\}";

        public static string BuildPath(string routeTemplate, IDictionary<string, string> parameters)
        {
            var path = Regex.Replace(routeTemplate, PARAMETER_PATTERN, match =>
            {
                string key = match.Groups["key"].Value;
                return (parameters.TryGetValue(key, out var value) ? value : null) ?? "";
            });
            return Regex.Replace(path, @"(\/)+", "/");
        }

        public static string[] GetParameterNames(string routeTemplate)
        {
            var names = new List<string>();
            var matches = Regex.Matches(routeTemplate, PARAMETER_PATTERN);
            foreach (Match match in matches)
            {
                names.Add(match.Groups["key"].Value);
            }
            return names.ToArray();
        }
    }
}
