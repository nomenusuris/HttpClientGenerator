using HttpClient.Generator.Tests.Helpers;
using HttpClient.Generator.Tests.Models;
using HttpClient.Generator.Tests.Models.Common;
using HttpClient.Generator.Tests.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Text.Json;

namespace HttpClient.Generator.Tests.Implementations.Services
{
    /// <summary>
    /// Sets up proxies and server with services mocks.
    /// </summary>
    /// <remarks>
    /// Each test follows these steps:
    /// <list type="number">
    ///     <item>
    ///     Service proxy and server-side mock are initialized
    ///     </item>
    ///     <item>
    ///     Proxy method is called with a predefined set of arguments
    ///     </item>
    ///     <item>
    ///     Mock invocation arguments are compared with a predefined set of arguments
    ///     </item>
    ///     <item>
    ///     Returned object is compared with an expected value if present
    ///     </item>
    /// </list>
    /// </remarks>
    internal class LoopbackServerFactory : WebApplicationFactory<ServerEntryPoint>
    {
        public CrudServiceMock CrudServiceMock { get; }
        public CrudServiceGenericMock<ExtendedModelExample, ChangeModelExample, FilterExample> CrudServiceGenericMock { get; }
        public FileServiceMock FileServiceMock { get; }
        public Uri ServerAddress = new Uri("http://127.0.0.1:1000");

        public LoopbackServerFactory()
        {
            CrudServiceMock = new CrudServiceMock();
            CrudServiceGenericMock = new CrudServiceGenericMock<ExtendedModelExample, ChangeModelExample, FilterExample>();
            FileServiceMock = new FileServiceMock();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseUrls(ServerAddress.ToString());
            builder.ConfigureServices((_, services) =>
            {
                // because service proxies will be using httpClient with assembly name as key
                // http client that sends requests to the test server get registered in such way
                services.AddHttpClient(Assembly.GetExecutingAssembly().GetName().Name, (hc) => { hc.BaseAddress = new Uri("http://127.0.0.1:1000"); })
                    .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
                services.AddRouting();
                services.AddSingleton(new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
                services.AddAntiforgery();
                RegisterCrudServices(services);
                RegisterGenericCrudServices(services);
                RegisterFileServices(services);
            });

            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseAntiforgery();
                app.UseEndpoints(endpoints =>
                {
                    EndpointRegistratorHelper.RegisterExposedEndpoint<ICrudService>(endpoints);
                    EndpointRegistratorHelper.RegisterExposedEndpoint<ICrudServiceGeneric<ExtendedModelExample, ChangeModelExample, FilterExample>>(endpoints);
                    EndpointRegistratorHelper.RegisterExposedEndpoint<IFileService>(endpoints);
                });
            });
        }

        private void RegisterCrudServices(IServiceCollection services)
        {
            services.AddSingleton(CrudServiceMock);
            services.AddSingleton(CrudServiceMock.Object);
            services.AddKeyedSingleton<ICrudService, ICrudServiceProxy_>("Proxy");
        }

        private void RegisterGenericCrudServices(IServiceCollection services)
        {
            services.AddSingleton(CrudServiceGenericMock);
            services.AddSingleton(CrudServiceGenericMock.Object);
            services.AddKeyedSingleton<ICrudServiceGeneric<ExtendedModelExample, ChangeModelExample, FilterExample>, ICrudServiceGenericProxy_ExtendedModelExample_ChangeModelExample_FilterExample>("Proxy");
        }

        private void RegisterFileServices(IServiceCollection services)
        {
            services.AddSingleton(FileServiceMock);
            services.AddSingleton(FileServiceMock.Object);
            services.AddKeyedSingleton<IFileService, IFileServiceProxy_>("Proxy");
        }
    }
}
