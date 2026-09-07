using HttpClient.Generator.Tests.Models;
using HttpClient.Generator.Tests.Models.Common;
using HttpClient.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Services
{
    [ExposeEndpoints("generic", typeof(ICrudServiceGeneric<ExtendedModelExample, ChangeModelExample, FilterExample>))]
    public interface ICrudServiceGeneric<TModel, TChangeModel, TFilter>
        where TFilter : QueryObjectExampleTyped<TFilter>, new()
    {
        [HttpGet("page")]
        Task<PageExample<TModel>> GetPageAsync([AsParameters] PageRequestGenericExample<TFilter> pageRequest);

        [HttpGet("{id:int}")]
        Task<TModel> GetAsync(uint id);

        [HttpGet("byIds")]
        Task<TModel[]> GetByIdsAsync(QueryArrayExampleTyped<uint> ids);

        [HttpPost]
        Task<uint> CreateAsync(TChangeModel model);

        [HttpPut("{id:int}")]
        Task UpdateAsync(uint id, TChangeModel model);

        [HttpDelete("{id:int}")]
        Task DeleteAsync(uint id);
    }
}
