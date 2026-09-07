using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HttpClient.Generator
{
    internal class GeneratedResult
    {
        public string Name { get; private set; }
        public CompilationUnitSyntax Implementation { get; private set; }
        public GenerationException Exception { get; private set; }


        public static GeneratedResult Error(GenerationException ex)
        {
            return new GeneratedResult { Exception = ex };
        }

        public static GeneratedResult Success(string name, CompilationUnitSyntax classDeclaration)
        {
            return new GeneratedResult { Name = name, Implementation = classDeclaration };
        }
    }

    internal class GenerationException : Exception
    {
        public GenerationException(string message) : base(message) { }
        public string Location { get; set; }
    }
}
