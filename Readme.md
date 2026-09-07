
# REST proxy generator for Minimal API like interfaces 

### What is it?
A source generator that creates API service class from an interface declaration containing ASP.NET Core attributes. The API service class works as a proxy and consists of method definitions that call their remote counterparts. Remote calls are done with injected HttpClient (through IHttpClientFactory using containing assembly name as a key) and JsonSerializerOptions for subsequent http message exchange.

### Quickstart
Add "HttpClient.Generator.csproj" and "HttpClient.Utils.csproj" as project references to the target project. Set OutputItemType="Analyzer" and ReferenceOutputAssembly="false" properties for "HttpClient.Generator.csproj":

```xml
<ProjectReference Include="..\HttpClient.Generator\HttpClient.Generator.csproj" 
				  OutputItemType="Analyzer" 
				  ReferenceOutputAssembly="false">
</ProjectReference>
<ProjectReference Include="..\HttpClient.Utils\HttpClient.Utils.csproj" />
```



Add ExposeEndpoints attribute with a required route prefix to the interfaces that need to be processed. For generic types service signature must be specified:

```csharp
[ExposeEndpoints("api/")]
public interface ICrudService
{
    [HttpGet("page")]
    Task<PageExample<ModelExample>> GetPageAsync([AsParameters] PageRequestExample pageRequest);
    // ...
}

// or for generic type

[ExposeEndpoints("api/", typeof(ICrudService<ReadModel, ChangeModel>))]
public interface ICrudService<TModel, TChangeModel>
{
    [HttpGet("{id:int}")]
    Task<TModel> GetAsync(uint id);

    [HttpPost]
    Task<uint> CreateAsync(TChangeModel model);
    //...
}
```

Rebuild the solution. 
Generated classes are named as {interfaceName}Proxy_{genericArg1_genericArg2} and placed in interface namespace.

### Specifics
For exposed methods only Task return types are supported.

Query string is serialized and deserialized only in bracket notation with explicit indices.

Supported return types:
- Task\<T\> (T needs to be deserializable from json)
- Task
- IResult (for file downloading)
- HttpResponseMessage

Not supported return types are not returned from the method.


Supported parameter types:
- Value types
- Reference types marked with [AsParameters] or sent as a request body
- IFormFile, IFormFileCollection

Other parameter types are not proxied and are substituted in place 

### Configuration
#### HttpClient
For each assembly that uses the generator, a dedicated HttpClient should be registered with that assembly name as a key.

```csharp
serviceCollection.AddHttpClient(
    iterfaceType.Assembly.GetName().Name, 
    (client) => { client.BaseAddress = new Uri(SERVER_HOST); });
```
All headers, antiforgery, CORS, authentication, authorization, and error handling logic should be configured at this stage. 


### Examples
Given the interface:
```csharp
[ExposeEndpoints("api/entity")]
public interface IExampleService
{
    [HttpGet("page")]
    Task<Page<EntityModel>> GetPageAsync([AsParameters] PageRequest pageRequest);

    [HttpGet("{id:int}")]
    Task<EntityModel> GetAsync(int id);

    [HttpPut("{id:int}")]
    Task UpdateAsync(int id, EntityChangeModel model);
}
```

The source generator produces:
```csharp
public class IExampleServiceProxy_ : IExampleService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    public IExampleServiceProxy_(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonSerializerOptions)
    {
        this.httpClientFactory = httpClientFactory;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task<Page<EntityModel>> GetPageAsync(PageRequest pageRequest)
    {
        using var client = httpClientFactory.CreateClient("Containing.Assembly");
        var routeParameters = new Dictionary<string, string>();
        var queryParameters = new Dictionary<string, object>();
        queryParameters.Add("Page", pageRequest.Page);
        queryParameters.Add("PageSize", pageRequest.PageSize);
        queryParameters.Add("Filters", pageRequest.Filters);
        var url = QueryHelpers.AddQueryString(RouteHelper.BuildPath("api/entity/page", routeParameters), QuerySerializer.Serialize(queryParameters, jsonSerializerOptions));
        ;
        using var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = null
        };
        using var response = await client.SendAsync(request);
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Page<EntityModel>>(responseContent, jsonSerializerOptions);
        }
    }

    public async Task<EntityModel> GetAsync(Int32 id)
    {
        using var client = httpClientFactory.CreateClient("Containing.Assembly");
        var routeParameters = new Dictionary<string, string>();
        var queryParameters = new Dictionary<string, object>();
        routeParameters.Add("id", id.ToString());
        var url = QueryHelpers.AddQueryString(RouteHelper.BuildPath("api/entity/{id:int}", routeParameters), QuerySerializer.Serialize(queryParameters, jsonSerializerOptions));
        ;
        using var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = null
        };
        using var response = await client.SendAsync(request);
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<EntityModel>(responseContent, jsonSerializerOptions);
        }
    }

    public async Task UpdateAsync(Int32 id, EntityChangeModel model)
    {
        using var client = httpClientFactory.CreateClient("Containing.Assembly");
        var routeParameters = new Dictionary<string, string>();
        var queryParameters = new Dictionary<string, object>();
        routeParameters.Add("id", id.ToString());
        var url = QueryHelpers.AddQueryString(RouteHelper.BuildPath("api/entity/{id:int}", routeParameters), QuerySerializer.Serialize(queryParameters, jsonSerializerOptions));
        using var content = new StringContent(JsonSerializer.Serialize(model, jsonSerializerOptions), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        using var response = await client.SendAsync(request);
        await response.Content.ReadAsStringAsync();
    }
}
```

### Usage scenarios