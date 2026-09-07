using HttpClient.Generator;
using HttpClient.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator
{
    namespace HttpClientGenerator
    {
        /// <summary>
        /// Generates proxy classes from interface declarations marked with <see cref="ExposeEndpointsAttribute"/>.
        /// 
        /// </summary>
        [Generator(LanguageNames.CSharp)]
        public class HttpClientGenerator : IIncrementalGenerator
        {
            private static readonly DiagnosticDescriptor Rule = new(
                id: "HCG001",
                title: "Proxy generation error",
                messageFormat: "Error in '{0}' during proxy generation",
                category: "HttpClientGenerator",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public void Initialize(IncrementalGeneratorInitializationContext context)
            {
                var extractor = new HttpImplementationSyntaxProvider();
                var provider = context.SyntaxProvider.CreateSyntaxProvider(extractor.Predicate, extractor.Transform);
                context.RegisterSourceOutput(provider, (spc, result) =>
                {
                    // Condition to trigger diagnostic
                    if (result.Exception != null)
                    {
                        var diagnostic = Diagnostic.Create(Rule, Location.None, result.Exception.Location);
                        spc.ReportDiagnostic(diagnostic);
                        return;
                    }
                    spc.AddSource($"{result.Name}.g.cs", result.Implementation.NormalizeWhitespace().GetText().ToString());
                });
            }
        }
    }

}
