namespace HttpClient.Generator.Tests.Models.Common
{
    public class PageRequestGenericExample<TFilters>
        where TFilters : QueryObjectExampleTyped<TFilters>, new()
    {
        public uint Page { get; set; }
        public uint PageSize { get; set; }

        public TFilters Filters { get; set; }
    }
}
