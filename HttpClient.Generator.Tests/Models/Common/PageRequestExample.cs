namespace HttpClient.Generator.Tests.Models.Common
{
    public class PageRequestExample
    {
        public uint Page { get; set; }
        public uint PageSize { get; set; }

        public FilterExample Filters { get; set; }
    }
}
