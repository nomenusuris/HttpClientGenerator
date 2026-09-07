using HttpClient.Generator.Tests.Models;
using HttpClient.Generator.Tests.Models.Common;
using HttpClient.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Services
{
    [ExposeEndpoints("crud")]
    public interface ICrudService
    {
        [HttpGet("page")]
        Task<PageExample<ModelExample>> GetPageAsync([AsParameters] PageRequestExample pageRequest);

        [HttpGet("{id:int}")]
        Task<ModelExample> GetAsync(uint id);

        [HttpGet("byIds")]
        Task<ModelExample[]> GetByIdsAsync(QueryArrayExampleTyped<uint> ids);

        [HttpPost]
        Task<uint> CreateAsync(ModelExample model);

        [HttpPut("{id:int}")]
        Task UpdateAsync(uint id, ModelExample model);

        [HttpDelete("{id:int}")]
        Task DeleteAsync(uint id);
    }
}
