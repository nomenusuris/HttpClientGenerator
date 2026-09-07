using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HttpClient.Generator
{
    using SF = SyntaxFactory;
    public static class Constants
    {
        public static string[] UtilityFilenames = new[]
        {
            "FormHelper.cs",
            "QuerySerializer.cs",
            "RouteHelper.cs"
        };

        public static HashSet<string> HttpAttributeNames { get; } = new()
            {
                "HttpGetAttribute",
                "HttpPostAttribute",
                "HttpPatchAttribute",
                "HttpPutAttribute",
                "HttpDeleteAttribute"
            };

        public static readonly string FROM_BODY_ATTRIBUTE = "FromBodyAttribute";
        public static readonly string AS_PARAMETERS_ATTRIBUTE = "AsParametersAttribute";
        public static readonly string FROM_FORM_ATTRIBUTE = "FromFormAttribute";
        public static readonly string FROM_QUERY_ATTRIBUTE = "FromQueryAttribute";
        public static readonly string FROM_ROUTE_ATTRIBUTE = "FromRouteAttribute";
        public static string GENERIC_TASK_TYPE = typeof(Task<>).FullName;
        public static string TASK_TYPE = typeof(Task).FullName;

        public static readonly Dictionary<string, string> HttpMethodMapping = new()
        {
            ["HttpGetAttribute"] = "GET",
            ["HttpPostAttribute"] = "POST",
            ["HttpPatchAttribute"] = "PATCH",
            ["HttpPutAttribute"] = "PUT",
            ["HttpDeleteAttribute"] = "DELETE",
        };

        public static readonly string[] HttpMethodsWithBody = new[] { "POST", "PATCH", "PUT" };

        public static readonly UsingDirectiveSyntax[] CommonUsings = new[]
            {
                SF.UsingDirective(SF.ParseName("HttpClient.Utils")),
                SF.UsingDirective(SF.ParseName("Microsoft.AspNetCore.WebUtilities")),
                SF.UsingDirective(SF.ParseName("Microsoft.Extensions.Http")),
                SF.UsingDirective(SF.ParseName("System")),
                SF.UsingDirective(SF.ParseName("System.Collections.Generic")),
                SF.UsingDirective(SF.ParseName("System.IO")),
                SF.UsingDirective(SF.ParseName("System.Net.Http")),
                SF.UsingDirective(SF.ParseName("System.Runtime")),
                SF.UsingDirective(SF.ParseName("System.Text")),
                SF.UsingDirective(SF.ParseName("System.Text.Json")),
                SF.UsingDirective(SF.ParseName("System.Threading.Tasks"))
            };

        public static readonly string[] HttpNonPassableTypes = new[] {
            "HttpRequest",
            "HttpResponse",
            "HttpContext",
            "CancellationToken"
        };

        public static readonly TypeSyntax HttpClientFactoryType = SF.ParseTypeName("IHttpClientFactory");
        public static readonly TypeSyntax JsonSerializerOptionsType = SF.ParseTypeName("JsonSerializerOptions");

        public static readonly SyntaxToken httpClientFactory = SF.ParseToken("httpClientFactory");
        public static readonly SyntaxToken jsonSerializerOptions = SF.ParseToken("jsonSerializerOptions");
    }
}
