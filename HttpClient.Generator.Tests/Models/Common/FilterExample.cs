using System;

namespace HttpClient.Generator.Tests.Models.Common
{
    public class FilterExample : QueryObjectExampleTyped<FilterExample>
    {
        public string StringFilter
        {
            get => (string)Get(nameof(StringFilter));
            set => Set(nameof(StringFilter), value);
        }

        public DateTime? DateTimeFilter
        {
            get => (DateTime?)Get(nameof(DateTimeFilter));
            set => Set(nameof(DateTimeFilter), value);
        }


        public int[]? IntArrayFilter
        {
            get => GetArray<int>(nameof(IntArrayFilter));
            set => SetArray(nameof(IntArrayFilter), value);
        }

        public EnumExample? EnumFilter
        {
            get => (EnumExample?)Get(nameof(EnumFilter));
            set => Set(nameof(EnumFilter), value);
        }
    }
}
