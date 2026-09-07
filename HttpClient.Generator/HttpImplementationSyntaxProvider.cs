using HttpClient.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Globalization;

namespace HttpClient.Generator
{
    using SF = SyntaxFactory;
    internal class HttpImplementationSyntaxProvider
    {
        private readonly SymbolDisplayFormat _symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        private readonly SymbolDisplayFormat _resultTypeSymbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
            );


        private StatementSyntax Statement(string str) => SF.ParseStatement(str);

        public List<InterfaceDeclarationSyntax> HttpInterfaces { get; } = new();

        /// <remarks>Only interfaces with <see cref="ExposeEndpointsAttribute"/> are added</remarks>
        public bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (syntaxNode is InterfaceDeclarationSyntax interfaceDeclaration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return HasExposeEndpointAttributesSyntax(interfaceDeclaration);
            }
            return false;
        }

        public GeneratedResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.Node is not InterfaceDeclarationSyntax interfaceDeclaration)
            {
                throw new InvalidOperationException("Wrong syntax node type");
            }

            var compilation = context.SemanticModel.Compilation;
            var semanticModel = compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration);
            try
            {
                if (interfaceSymbol == null)
                {
                    throw new InvalidOperationException("Wrong syntax node type");
                }

                cancellationToken.ThrowIfCancellationRequested();

                var namespaceDecl = GetNamespaceDeclarationNode(context, interfaceDeclaration, cancellationToken);
                var exposedAttributes = interfaceSymbol.GetAttributes()
                    .Where(attr => nameof(ExposeEndpointsAttribute).StartsWith(attr.AttributeClass.Name));
                var classDecls = new List<ClassDeclarationSyntax>();

                // creates a class declaration for each ExposeEndpoints attribute
                // so that every particular generic type is present
                foreach (var exposedAttribute in exposedAttributes)
                {
                    classDecls.Add(GetClassDeclaration(context, interfaceSymbol, exposedAttribute));
                }

                namespaceDecl = namespaceDecl.WithMembers(SF.List<MemberDeclarationSyntax>(
                    classDecls
                ));
                var compilationRoots = interfaceSymbol.DeclaringSyntaxReferences
                    .Select(r => r.SyntaxTree.GetCompilationUnitRoot(cancellationToken));
                var usingNodes = compilationRoots
                    .SelectMany(cr => cr
                        .DescendantNodes()
                        .Where(n => n is UsingDirectiveSyntax)
                        .Cast<UsingDirectiveSyntax>());
                var unit = SF.CompilationUnit()
                    .WithUsings(SF.List(usingNodes.Concat(Constants.CommonUsings)))
                    .WithMembers(SF.List<MemberDeclarationSyntax>(new[]
                    {
                        namespaceDecl
                    }));
                return GeneratedResult.Success($"{GetClassName(interfaceSymbol)}", unit);
            }
            catch (Exception ex)
            {
                var loc = ex.Source;
                return GeneratedResult.Error(new GenerationException(ex.Message)
                {
                    Source = ex.Source,
                    Location = $"{loc}"
                });
            }
        }

        /// <summary>
        /// Creates a namespace declaration that matches the namespace containing the interface
        /// </summary>
        private NamespaceDeclarationSyntax GetNamespaceDeclarationNode(GeneratorSyntaxContext context, InterfaceDeclarationSyntax interfaceDeclaration, CancellationToken cancellationToken)
        {
            var semanticModel = context.SemanticModel.Compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration);
            var namespaceName = interfaceSymbol.ContainingNamespace
                .ToDisplayString(_symbolDisplayFormat);
            var interfaceNamespaceSyntaxNode = SF.ParseName(namespaceName);
            var namespaceDecl = SF.NamespaceDeclaration(interfaceNamespaceSyntaxNode);
            return namespaceDecl;
        }

        /// <summary>
        /// Creates a class declaration syntax from the interface symbol.
        /// The generated class will be named ${interfaceName}Proxy_${genericArgument1}_${genericArgument2}
        /// </summary>
        private ClassDeclarationSyntax GetClassDeclaration(GeneratorSyntaxContext context, INamedTypeSymbol interfaceSymbol, AttributeData exposedAttribute)
        {
            var prefix = exposedAttribute.ConstructorArguments.ElementAtOrDefault(0).Value.ToString() ?? "";
            interfaceSymbol = exposedAttribute.ConstructorArguments.ElementAtOrDefault(1).Value as INamedTypeSymbol
                ?? interfaceSymbol;

            var methods = GetMembers<IMethodSymbol>(interfaceSymbol);
            var baseSymbol = SF.SimpleBaseType(SF.ParseTypeName(interfaceSymbol.ToDisplayString(_resultTypeSymbolDisplayFormat)));
            var className = GetClassName(interfaceSymbol);
            var classDecl = SF.ClassDeclaration(SF.ParseToken(className))
                .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)))
                .WithBaseList(SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(baseSymbol)))
                .WithMembers(SF.List(new[]
                {
                    SF.ParseMemberDeclaration($"private readonly IHttpClientFactory {nameof(Constants.httpClientFactory)};"),
                    SF.ParseMemberDeclaration($"private readonly JsonSerializerOptions {nameof(Constants.jsonSerializerOptions)};"),
                    GetConstructor(context, className)
                }.Concat(methods.Select(m => GetMethod(context, m, prefix)))));
            return classDecl;
        }

        /// <summary>
        /// Creates a constructor declaration that injects IHttpClientFactory and JsonSerializerOptions
        /// </summary>
        private ConstructorDeclarationSyntax GetConstructor(GeneratorSyntaxContext context, string className)
        {
            return SF.ConstructorDeclaration(SF.List<AttributeListSyntax>(),
                SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                SF.ParseToken(className),
                SF.ParameterList(
                    SF.SeparatedList(new[] {
                        SF.Parameter(Constants.httpClientFactory).WithType(Constants.HttpClientFactoryType),
                        SF.Parameter(Constants.jsonSerializerOptions).WithType(Constants.JsonSerializerOptionsType),
                    })),
                null,
                SF.Block(SF.List(new[]
                {
                    Statement($"this.{Constants.httpClientFactory} = {Constants.httpClientFactory};"),
                    Statement($"this.{Constants.jsonSerializerOptions} = {Constants.jsonSerializerOptions};")
                })));
        }

        /// <summary>
        /// Creates a method declaration from the interface method signature.
        /// Resolves HttpClient with the containing assembly name as a key than 
        /// sends appropriate request to the methods remote counterpart returning result
        /// </summary>
        private MethodDeclarationSyntax GetMethod(GeneratorSyntaxContext context, IMethodSymbol methodSymbol, string routePrefix = "")
        {
            var httpAttributeWithName = methodSymbol
                .GetAttributes()
                .Select(attribute => (attribute, attributeName: Constants.HttpAttributeNames.FirstOrDefault(httpAttribute => attribute.AttributeClass.Name.ToString().StartsWith(httpAttribute))))
                .Where(attribute => attribute.attributeName != null)
                .FirstOrDefault();
            if (httpAttributeWithName.attribute == null)
            {
                return GetNotImplementedMethodStub(context, methodSymbol);
            }
            var httpMethod = Constants.HttpMethodMapping[httpAttributeWithName.attributeName];
            var methodPrefix = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(httpMethod.ToLower());
            var route = routePrefix.TrimEnd('/') + "/" + (httpAttributeWithName.attribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "").TrimStart('/');
            var routeParameterNames = RouteHelper.GetParameterNames(route);
            var routeParameters = methodSymbol.Parameters
                .Where(p => routeParameterNames.Contains(p.Name));
            var nonRouteParameters = InferParameters(httpMethod, methodSymbol.Parameters
                .Where(p => !routeParameterNames.Contains(p.Name)));
            var hasCancellationToken = nonRouteParameters.Any(p => p.AttributeName == null && p.Type.Name == nameof(CancellationToken));
            var queryParameters = nonRouteParameters.Where(p => p.IsQuery());
            var contentParameter = nonRouteParameters.SingleOrDefault(p => p.IsContent());
            var returnType = GetAsyncResultType(methodSymbol);

            var clientCreation = Statement($@"using var client = {Constants.httpClientFactory}.CreateClient(""{methodSymbol.ContainingAssembly.Name}"");");
            var routeParamsDeclarationStatement = Statement(@"var routeParameters = new Dictionary<string, string>();");
            var queryParamsDeclarationStatement = Statement(@"var queryParameters = new Dictionary<string, object>();");
            var routeParamsInitStatements = SF.List(routeParameters.Select(rpn => Statement($"routeParameters.Add(\"{rpn.Name}\",{rpn.Name}{(IsNullable(rpn.Type) ? "?" : "")}.ToString());")));
            var queryParamsInitStatements = SF.List(queryParameters.Select(qp => Statement($"queryParameters.Add(\"{qp.Name}\",{qp.Path});")));
            var urlInitStatement = Statement($@"var url = QueryHelpers.AddQueryString(RouteHelper.BuildPath(""{route}"", routeParameters), {nameof(QuerySerializer)}.Serialize(queryParameters, {nameof(Constants.jsonSerializerOptions)}));");
            StatementSyntax contentInitStatement = SF.EmptyStatement();
            if (contentParameter != null)
            {
                var formBodyStr = $"{nameof(FormHelper)}.{nameof(FormHelper.ToForm)}({contentParameter.Path})";
                var jsonBodyStr = $"JsonSerializer.Serialize({contentParameter.Path},{Constants.jsonSerializerOptions})";
                contentInitStatement = Statement("using var content = " + (contentParameter.IsForm()
                    ? formBodyStr
                    : $"new StringContent({jsonBodyStr}, Encoding.UTF8, \"application/json\")") + ";");
            }
            var requestInitStatement = Statement($"using var request = new HttpRequestMessage(HttpMethod.{methodPrefix}, url){{Content = {(contentParameter != null ? "content" : "null")}}};");

            var responseInitStatement = Statement(hasCancellationToken
                ? "using var response = await client.SendAsync(request, cancellationToken);"
                : "using var response = await client.SendAsync(request);");
            StatementSyntax returnStatement = null;
            if (returnType == null)
            {
                returnStatement = Statement("await response.Content.ReadAsStringAsync();");
            }
            else if (returnType.Name == "string")
            {
                returnStatement = Statement("return await response.Content.ReadAsStringAsync();");
            }
            else if (returnType.Name == "byte[]")
            {
                returnStatement = Statement("return await response.Content.ReadAsByteArrayAsync();");
            }
            else if (returnType.Name == "Stream" || returnType.Name == "System.IO.Stream")
            {
                returnStatement = Statement("return await response.Content.ReadAsStreamAsync();");
            }
            else if (returnType.Name == "HttpResponseMessage")
            {
                returnStatement = Statement("return response;");
            }
            else if (returnType.Name == "IResult")
            {
                returnStatement = Statement("{"
                    + "Stream ms = new MemoryStream();\r\n"
                    + "await response.Content.CopyToAsync(ms);\r\n"
                    + "ms.Seek(0, SeekOrigin.Begin);\r\n"
                    + "return Results.File(ms, response.Content.Headers.ContentType?.ToString(), response.Content.Headers.ContentDisposition?.FileName);"
                    + "}");
            }
            else
            {
                returnStatement = Statement("{var responseContent = await response.Content.ReadAsStringAsync();\r\n"
                    + $"return JsonSerializer.Deserialize<{returnType.ToDisplayString(_resultTypeSymbolDisplayFormat)}>(responseContent, {nameof(Constants.jsonSerializerOptions)});}}");
            }


            return SF.MethodDeclaration(SF.ParseTypeName(methodSymbol.ReturnType.ToDisplayString(_resultTypeSymbolDisplayFormat)), methodSymbol.Name)
                .WithModifiers(SF.TokenList(new[]
                {
                    SF.Token(SyntaxKind.PublicKeyword),
                    SF.Token(SyntaxKind.AsyncKeyword)
                }))
                .WithParameterList(
                SF.ParameterList()
                    .WithParameters(SF.SeparatedList(methodSymbol.Parameters
                        .Select(p => SF.Parameter(SF.ParseToken(p.Name))
                            .WithType(SF.ParseTypeName(p.Type
                                .ToDisplayString(_resultTypeSymbolDisplayFormat)))))))
                .WithBody(SF.Block(SF.List(new[]
                    {
                        clientCreation,
                        routeParamsDeclarationStatement,
                        queryParamsDeclarationStatement
                    }.Concat(routeParamsInitStatements)
                    .Concat(queryParamsInitStatements)
                    .Concat(new[]
                    {
                        urlInitStatement,
                        contentInitStatement,
                        requestInitStatement,
                        responseInitStatement,
                        returnStatement
                }))));
        }

        // mimics Minimal API parameter inference logic
        private IEnumerable<InferredParameter> InferParameters(string httpMethod, IEnumerable<IParameterSymbol> parameters)
        {
            var result = new List<InferredParameter>();
            foreach (var parameter in parameters)
            {
                // AsParameters attribute just flattens the parameter
                if (FindAttribute(parameter, Constants.AS_PARAMETERS_ATTRIBUTE) != null)
                {
                    var properties = GetMembers<IPropertySymbol>(parameter.Type);
                    foreach (var propery in properties)
                    {
                        result.Add(new InferredParameter(propery.Name, propery.Type, InferHttpParameterAttribute(propery), parameter.Name));
                    }
                }
                else
                {
                    result.Add(new InferredParameter(parameter.Name, parameter.Type, InferHttpParameterAttribute(parameter)));
                }
            }

            var shouldHaveBody = Constants.HttpMethodsWithBody.Contains(httpMethod);
            var bodyParameters = result.Where(r => r.AttributeName == Constants.FROM_BODY_ATTRIBUTE);
            var formParameters = result.Where(r => r.AttributeName == Constants.FROM_FORM_ATTRIBUTE);

            if (!shouldHaveBody && bodyParameters.Any())
            {
                throw new InvalidOperationException($"Body parameter {bodyParameters.First().Name} present for bodyless http method");
            }
            if (bodyParameters.Count() > 1)
            {
                throw new InvalidOperationException($"Too many body parameters: {string.Join(", ", bodyParameters.Select(bp => bp.Name))}");
            }
            if (bodyParameters.Any() && formParameters.Any())
            {
                throw new InvalidOperationException($"Cant have FromForm and FromBody simultaneously");
            }

            return result;
        }

        /// <summary>
        /// Creates a method stub that throws NotImplementedException for methods that are not exposed
        /// </summary>
        private MethodDeclarationSyntax GetNotImplementedMethodStub(GeneratorSyntaxContext context, IMethodSymbol methodSymbol)
        {
            return SF.MethodDeclaration(SF.ParseTypeName(methodSymbol.ReturnType.ToDisplayString(_resultTypeSymbolDisplayFormat)), methodSymbol.Name)
                .WithModifiers(SF.TokenList(new[]
                {
                    SF.Token(SyntaxKind.PublicKeyword),
                    SF.Token(SyntaxKind.AsyncKeyword)
                }))
                .WithParameterList(
                SF.ParameterList()
                    .WithParameters(SF.SeparatedList(methodSymbol.Parameters
                        .Select(p => SF.Parameter(SF.ParseToken(p.Name))
                            .WithType(SF.ParseTypeName(p.Type
                                .ToDisplayString(_resultTypeSymbolDisplayFormat)))))))
                .WithBody(SF.Block(SF.List(new[]
                    {
                    Statement("throw new NotImplementedException(\"Method not exposed\")")
                })));
        }

        private string InferHttpParameterAttribute(ISymbol parameter)
        {
            if (IsQueryParameter(parameter))
            {
                return Constants.FROM_QUERY_ATTRIBUTE;
            }
            else if (IsFormParameter(parameter))
            {
                return Constants.FROM_FORM_ATTRIBUTE;
            }
            else if (IsBodyParameter(parameter))
            {
                return Constants.FROM_BODY_ATTRIBUTE;
            }
            return null;
        }


        private bool HasExposeEndpointAttributesSyntax(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            foreach (var attribute in interfaceDeclaration.AttributeLists)
            {
                if (attribute.Attributes.Any(attr => nameof(ExposeEndpointsAttribute).StartsWith(attr.Name.ToString())))
                {
                    return true;
                }
            }
            return false;
        }

        private string GetClassName(INamedTypeSymbol interfaceSymbol)
        {
            return interfaceSymbol.Name + "Proxy_" + (interfaceSymbol.TypeArguments.Any() ? string.Join("_", interfaceSymbol.TypeArguments.Select(a => a.Name)) : "");
        }

        private IEnumerable<TSymbol> GetMembers<TSymbol>(ITypeSymbol typeSymbol)
            where TSymbol : ISymbol
        {
            var allProperties = new List<TSymbol>();
            ITypeSymbol currentType = typeSymbol;

            while (currentType != null)
            {
                allProperties.AddRange(currentType.GetMembers().OfType<TSymbol>());
                currentType = currentType.BaseType;
            }
            return allProperties;
        }

        private bool IsQueryParameter(ISymbol parameter)
        {
            var type = GetSymbolType(parameter);
            if (type.Name == typeof(string).Name || type.IsValueType)
            {
                return true;
            }
            var methods = GetMembers<IMethodSymbol>(type);
            return methods.Any(m => m.Name == "TryParse" || m.Name == "BindAsync")
                || FindAttribute(parameter, Constants.FROM_QUERY_ATTRIBUTE) != null;
        }

        private bool IsFormParameter(ISymbol parameter)
        {
            var type = GetSymbolType(parameter);
            return FindAttribute(parameter, Constants.FROM_FORM_ATTRIBUTE) != null
                || type.Name == "IFormFile"
                || type.Name == "IFormFileCollection";
        }

        private bool IsBodyParameter(ISymbol parameter)
        {
            var type = GetSymbolType(parameter);
            return !Constants.HttpNonPassableTypes.Contains(type.Name)
                || FindAttribute(parameter, Constants.FROM_BODY_ATTRIBUTE) != null;
        }

        private AttributeData FindAttribute(ISymbol symbol, string attributeClassName)
        {
            return symbol.GetAttributes()
                .Where(a => a.AttributeClass != null && a.AttributeClass.Name == attributeClassName)
                .LastOrDefault();
        }

        private ITypeSymbol GetAsyncResultType(IMethodSymbol methodSymbol)
        {
            var returnType = methodSymbol.ReturnType;
            if (returnType is INamedTypeSymbol genericTaskType
                && $"{genericTaskType.ContainingNamespace.ToDisplayString()}.{genericTaskType.MetadataName}" == Constants.GENERIC_TASK_TYPE)
            {
                return genericTaskType.TypeArguments.First();
            }

            if (returnType is INamedTypeSymbol taskType
                && taskType.OriginalDefinition.ToString() == Constants.TASK_TYPE)
            {
                return null;
            }

            throw new Exception("Only async methods are supported");
        }

        private ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            if (symbol is IParameterSymbol parameterSymbol)
            {
                return parameterSymbol.Type;
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                return propertySymbol.Type;
            }
            else if (symbol is IFieldSymbol fieldSymbol)
            {
                return fieldSymbol.Type;
            }
            throw new InvalidOperationException("Symbol has no type");
        }

        private bool IsNullable(ITypeSymbol typeSymbol)
        {
            // 1. Check for Nullable Value Types (e.g., int?, Nullable<T>)
            if (typeSymbol.IsValueType)
            {
                return typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            }

            // 2. Check for Nullable Reference Types (e.g., string?, MyClass?)
            return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        }
    }
}
