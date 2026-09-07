using HttpClient.Generator.Tests.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Implementations.Services
{
    internal class FileServiceMock : Mock<IFileService>
    {
        public byte[] FileContentExample { get; }
        public IDictionary<string, string> CapturedMessages;

        public string ResultFileNameExample => "DownloadName";
        public IResult ResultFileExample => TypedResults.File(FileContentExample, "text/plain", ResultFileNameExample);

        public FileServiceMock()
        {
            CapturedMessages = new Dictionary<string, string>();
            FileContentExample = Enumerable.Range(0, 100)
                .Select(i => (byte)i)
                .ToArray();

            Setup(s => s.UploadAsync(It.IsAny<IFormFile>()))
                .Returns<IFormFile>((file) =>
                {
                    var stream = file.OpenReadStream();
                    using var reader = new StreamReader(stream);
                    CapturedMessages.Add(file.FileName, reader.ReadToEnd());
                    return Task.FromResult(1u);
                });

            Setup(s => s.UploadMultipleAsync(It.IsAny<IFormFileCollection>()))
                .Returns<IFormFileCollection>((fileCollection) =>
                {
                    foreach (var file in fileCollection)
                    {
                        var stream = file.OpenReadStream();
                        using var reader = new StreamReader(stream);
                        CapturedMessages.Add(file.FileName, reader.ReadToEnd());
                    }
                    return Task.FromResult(new[] { 1u, 2u });
                });

            Setup(s => s.BrowserDownloadAsync(It.IsAny<uint>()))
                .Returns<uint>((id) => Task.FromResult(ResultFileExample));

            Setup(s => s.RawDownloadAsync(It.IsAny<uint>()))
               .Returns<byte[]>((ids) => Task.FromResult(FileContentExample));
        }
    }
}
