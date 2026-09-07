using HttpClient.Generator.Tests.Implementations.Services;
using HttpClient.Generator.Tests.Models;
using HttpClient.Generator.Tests.Models.Common;
using HttpClient.Generator.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests
{
    /// <summary>
    /// Checks the validity of received and returned objects with
    /// HttpClient generated from <see cref="ICrudService"/> interface declaration on the client side
    /// and <see cref="CrudServiceMock"/> on the server side
    /// </summary>
    [TestClass]
    public class CrudTests
    {
        private LoopbackServerFactory _host = null!;


        [TestInitialize()]
        public void TestInitialize()
        {
            _host = new LoopbackServerFactory();
        }

        [TestMethod]
        public async Task TestGetPage()
        {
            var serverSideMock = _host.Services.GetService<CrudServiceMock>();
            var proxy = _host.Services.GetKeyedService<ICrudService>("Proxy");
            var pageRequest = new PageRequestExample()
            {
                PageSize = 11,
                Page = 100,
                Filters = new FilterExample()
                {
                    StringFilter = "FilterValue",
                    DateTimeFilter = DateTime.UtcNow,
                    EnumFilter = EnumExample.ThirdEnumValue,
                    IntArrayFilter = new[] { 1, 2, 3 }
                }
            };
            var page = await proxy.GetPageAsync(pageRequest);

            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(ICrudService.GetPageAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedPageRequest = args.FirstOrDefault() as PageRequestExample;
            Assert.IsTrue(JsonSerializer.Serialize(pageRequest) == JsonSerializer.Serialize(receivedPageRequest));
            Assert.IsTrue(JsonSerializer.Serialize(page) == JsonSerializer.Serialize(_host.CrudServiceMock.Page));
        }

        [TestMethod]
        public async Task TestGet()
        {
            var serverSideMock = _host.Services.GetService<CrudServiceMock>();
            var proxy = _host.Services.GetKeyedService<ICrudService>("Proxy");
            var id = 1u;
            var item = await proxy.GetAsync(id);

            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(ICrudService.GetAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedId = args.FirstOrDefault();
            Assert.IsTrue(JsonSerializer.Serialize(id) == JsonSerializer.Serialize(receivedId));
            Assert.IsTrue(JsonSerializer.Serialize(item) == JsonSerializer.Serialize(_host.CrudServiceMock.Page.Items.First(i => i.Id == id)));
        }

        [TestMethod]
        public async Task TestGetByIds()
        {
            var serverSideMock = _host.Services.GetService<CrudServiceMock>();
            var proxy = _host.Services.GetKeyedService<ICrudService>("Proxy");
            var ids = new[] { 1u, 2u, 3u };
            var items = await proxy.GetByIdsAsync(ids);


            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(ICrudService.GetByIdsAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedIds = args.FirstOrDefault();
            Assert.IsTrue(JsonSerializer.Serialize(ids) == JsonSerializer.Serialize(receivedIds));
            Assert.IsTrue(
                JsonSerializer.Serialize(items
                    .OrderBy(i => i.Id))
                == JsonSerializer.Serialize(_host.CrudServiceMock.Page.Items
                    .Where(i => ids.Contains(i.Id))
                    .OrderBy(i => i.Id)));
        }

        [TestMethod]
        public async Task TestCreate()
        {
            var serverSideMock = _host.Services.GetService<CrudServiceMock>();
            var proxy = _host.Services.GetKeyedService<ICrudService>("Proxy");
            var item = serverSideMock.Page.Items.First();
            var items = await proxy.CreateAsync(item);


            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(ICrudService.CreateAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedItem = args.FirstOrDefault();
            Assert.IsTrue(JsonSerializer.Serialize(item) == JsonSerializer.Serialize(receivedItem));
        }

        [TestMethod]
        public async Task TestUpdate()
        {
            var serverSideMock = _host.Services.GetService<CrudServiceMock>();
            var proxy = _host.Services.GetKeyedService<ICrudService>("Proxy");
            var item = serverSideMock.Page.Items.First();
            await proxy.UpdateAsync(item.Id, item);


            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(ICrudService.UpdateAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedId = args.FirstOrDefault();
            var receivedModel = args.LastOrDefault();
            Assert.IsTrue(JsonSerializer.Serialize(item.Id) == JsonSerializer.Serialize(receivedId));
            Assert.IsTrue(JsonSerializer.Serialize(item) == JsonSerializer.Serialize(receivedModel));
        }

        [TestMethod]
        public async Task TestDelete()
        {
            var serverSideMock = _host.Services.GetService<CrudServiceMock>();
            var proxy = _host.Services.GetKeyedService<ICrudService>("Proxy");
            var item = serverSideMock.Page.Items.First();
            await proxy.DeleteAsync(item.Id);


            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(ICrudService.DeleteAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedId = args.FirstOrDefault();
            Assert.IsTrue(JsonSerializer.Serialize(item.Id) == JsonSerializer.Serialize(receivedId));
        }
    }
}
