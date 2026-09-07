using HttpClient.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Services
{
    [ExposeEndpoints("files")]
    public interface IFileService
    {
        [HttpPost("upload")]
        Task<uint> UploadAsync(IFormFile file);

        [HttpPost("uploadMultiple")]
        Task<uint[]> UploadMultipleAsync(IFormFileCollection files);

        [HttpGet("{id:int}")]
        Task<IResult> BrowserDownloadAsync(uint id);

        [HttpGet("{id:int}/raw")]
        Task<byte[]> RawDownloadAsync(uint id);
    }
}
