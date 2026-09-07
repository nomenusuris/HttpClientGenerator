namespace HttpClient.Generator.Tests.Models.Common
{
    public class PageExample<TItem>
    {
        public TItem[] Items { get; set; }
        public uint Page { get; set; }
        public uint PageSize { get; set; }
        public uint TotalCount { get; set; }
    }
}
