using Microsoft.AspNetCore.Builder;

namespace HttpClient.Generator.Tests.Implementations.Services
{
    public class ServerEntryPoint
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            app.Run();
        }
    }
}
