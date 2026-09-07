using HttpClient.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace HttpClient.Generator.Tests.Helpers
{
    internal class EndpointRegistratorHelper
    {
        /// <summary>
        /// Registers interface http exposed methods as instance method calls with target binding to interface implementation 
        /// </summary>
        /// <typeparam name="TInterface">Interface consisting of http exposed methods</typeparam>
        /// <param name="routeBuilder">Endpoint route builder</param>
        public static void RegisterExposedEndpoint<TInterface>(IEndpointRouteBuilder routeBuilder)
        {
            var interfaceType = typeof(TInterface);
            var exposedEndpointAttribute = DeduceExposeEndpointsAttribute(interfaceType);
            var routePrefix = exposedEndpointAttribute.Template;
            foreach (var methodInfo in interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                var endpointMethods = DeduceEndpointMethods(methodInfo, interfaceType);
                foreach (var endpointMethod in endpointMethods)
                {
                    var requestDelegateResult = CreateRequestDelegate(routeBuilder.ServiceProvider, methodInfo, endpointMethod.Route);

                    routeBuilder.MapMethods(routePrefix + endpointMethod.Route.RawText ?? "", new[] { endpointMethod.HttpMethod.ToString() }, requestDelegateResult.RequestDelegate)
                        .DisableAntiforgery()
                        .WithMetadata(requestDelegateResult.EndpointMetadata.ToArray());
                }
            }
        }

        /// <summary>
        /// Returns interface ExposeEndpointsAttribute deduced by signature if present
        /// </summary>
        public static ExposeEndpointsAttribute DeduceExposeEndpointsAttribute(Type serviceInterfaceType)
        {
            if (serviceInterfaceType.IsGenericTypeDefinition)
            {
                throw new Exception("Service needs to have all generic arguments defined");
            }
            var attrs = serviceInterfaceType.GetCustomAttributes<ExposeEndpointsAttribute>();
            if (serviceInterfaceType.IsGenericType)
            {
                var endpointRouteTemplateAttr = attrs.SingleOrDefault(attr => attr.ServiceSignature == serviceInterfaceType);
                if (endpointRouteTemplateAttr == null)
                {
                    throw new Exception($"Missing exposed signature for {serviceInterfaceType}, use {nameof(ExposeEndpointsAttribute)} with parameter {nameof(ExposeEndpointsAttribute.ServiceSignature)} for concrete service type");
                }
                return endpointRouteTemplateAttr;
            }
            else
            {
                return attrs.Single();
            }
            throw new Exception("Service needs to be declared with ExposeEndpoints attribute for it to be exposable to http");
        }


        /// <summary>
        /// Returns EndpointMethods depending on method attributes
        /// </summary>
        private static IEnumerable<EndpointMethod> DeduceEndpointMethods(MethodInfo methodInfo, Type serviceInterfaceType)
        {
            var endpointRouteTemplate = "";
            foreach (var attr in methodInfo.GetCustomAttributes(false))
            {
                if (attr is IActionHttpMethodProvider methodAttr)
                {
                    var actionRouteTemplate = (attr as IRouteTemplateProvider)?.Template;
                    var routePattern = CreateRoutePattern(endpointRouteTemplate, actionRouteTemplate);
                    foreach (var method in methodAttr.HttpMethods)
                    {
                        yield return new EndpointMethod
                        {
                            HttpMethod = new HttpMethod(method),
                            Route = routePattern
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Creates endpoint from interface method declaration. 
        /// Uses first registered service implementation from DI as instance 
        /// </summary>
        private static RequestDelegateResult CreateRequestDelegate(IServiceProvider serviceProvider, MethodInfo methodInfo, RoutePattern routePattern)
        {
            var rdfOpt = new RequestDelegateFactoryOptions()
            {
                ServiceProvider = serviceProvider,
                RouteParameterNames = routePattern.Parameters
                    .Select(p => p.Name)
                    .ToList(),
                ThrowOnBadRequest = true,
                DisableInferBodyFromParameters = false
            };

            // creates delegate that will be called on service instance resolved from ServiceProvider
            var result = RequestDelegateFactory.Create(methodInfo, httpContext => httpContext.RequestServices.GetRequiredService(methodInfo.DeclaringType), rdfOpt);
            return result;
        }


        public static RoutePattern CreateRoutePattern(string prefix, string route)
        {
            if (route?.StartsWith("/") ?? false)
            {
                return RoutePatternFactory.Parse(route);
            }
            return RoutePatternFactory.Combine(prefix != null ? RoutePatternFactory.Parse(prefix) : null, RoutePatternFactory.Parse(route ?? ""));
        }
    }

    public class EndpointMethod
    {
        public HttpMethod HttpMethod { get; set; }
        public RoutePattern Route { get; set; }
    }
}
