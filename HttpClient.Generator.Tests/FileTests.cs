using HttpClient.Generator.Tests.Implementations.Services;
using HttpClient.Generator.Tests.Models;
using HttpClient.Generator.Tests.Models.Common;
using HttpClient.Generator.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests
{
    /// <summary>
    /// Checks the validity of received and returned objects with
    /// HttpClient generated from <see cref="IFileService"/> interface declaration on the client side
    /// and <see cref="FileServiceMock"/> on the server side
    /// </summary>
    [TestClass]
    public class FileTests
    {
        private LoopbackServerFactory _host = null!;


        [TestInitialize()]
        public void TestInitialize()
        {
            _host = new LoopbackServerFactory();
        }

        [TestMethod]
        public async Task TestUpload()
        {
            var serverSideMock = _host.Services.GetService<FileServiceMock>();
            var proxy = _host.Services.GetKeyedService<IFileService>("Proxy");
            var message = "This is file contents";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var testFileContent = new MemoryStream();
            testFileContent.Write(messageBytes);
            testFileContent.Seek(0, SeekOrigin.Begin);
            var formFile = new FormFile(testFileContent, 0, messageBytes.Length, "File", "FileName.test");
            var receivedId = await proxy.UploadAsync(formFile);

            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(IFileService.UploadAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedFormFile = args.FirstOrDefault() as IFormFile;
            Assert.IsTrue(formFile.Name == receivedFormFile.Name);
            Assert.IsTrue(formFile.FileName == receivedFormFile.FileName);
            Assert.IsTrue(serverSideMock.CapturedMessages[formFile.FileName] == message);
            Assert.IsTrue(receivedId == 1u);
        }

        [TestMethod]
        public async Task TestUploadMultiple()
        {
            var serverSideMock = _host.Services.GetService<FileServiceMock>();
            var proxy = _host.Services.GetKeyedService<IFileService>("Proxy");
            var message1 = "This is file #1 contents";
            var message2 = "This is file #2 contents";
            var message1Bytes = Encoding.UTF8.GetBytes(message1);
            var message2Bytes = Encoding.UTF8.GetBytes(message2);

            using var testFile1Content = new MemoryStream();
            testFile1Content.Write(message1Bytes);
            testFile1Content.Seek(0, SeekOrigin.Begin);

            using var testFile2Content = new MemoryStream();
            testFile2Content.Write(message2Bytes);
            testFile2Content.Seek(0, SeekOrigin.Begin);

            var formFile1 = new FormFile(testFile1Content, 0, message1Bytes.Length, "File1", "FileName1.test");
            var formFile2 = new FormFile(testFile2Content, 0, message2Bytes.Length, "File2", "FileName2.test");
            await proxy.UploadMultipleAsync(new FormFileCollection() { formFile1, formFile2 });

            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(IFileService.UploadMultipleAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");

            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedFormFileCollection = args.FirstOrDefault() as IFormFileCollection;
            var file1 = receivedFormFileCollection.First();
            Assert.IsTrue(file1.Name == formFile1.Name);
            Assert.IsTrue(file1.FileName == formFile1.FileName);
            Assert.IsTrue(serverSideMock.CapturedMessages[file1.FileName] == message1);

            var file2 = receivedFormFileCollection.Last();
            Assert.IsTrue(file2.Name == formFile2.Name);
            Assert.IsTrue(file2.FileName == formFile2.FileName);
            Assert.IsTrue(serverSideMock.CapturedMessages[file2.FileName] == message2);
        }

        [TestMethod]
        public async Task TestBrowserDownload()
        {
            var serverSideMock = _host.Services.GetService<FileServiceMock>();
            var proxy = _host.Services.GetKeyedService<IFileService>("Proxy");

            var result = await proxy.BrowserDownloadAsync(1u);
            var methodInvocations = serverSideMock.Invocations.Where(i => i.Method.Name == nameof(IFileService.BrowserDownloadAsync));
            Assert.IsTrue(methodInvocations.Count() == 1, "Should be called once");
            
            var fileResult = result as FileStreamHttpResult;
            using var reader = new StreamReader(fileResult.FileStream);
            var fileResultContent = await reader.ReadToEndAsync();
            var invocation = methodInvocations.First();
            var args = invocation.Arguments;
            var receivedId = args.FirstOrDefault() as uint?;

            Assert.IsTrue(1u == receivedId);
            Assert.IsTrue(Encoding.UTF8.GetString(serverSideMock.FileContentExample) == fileResultContent);
            Assert.IsTrue(serverSideMock.ResultFileNameExample == fileResult.FileDownloadName);
        }
    }
}
