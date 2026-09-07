using System.Collections.Generic;

namespace HttpClient.Generator.Tests.Models
{
    public class ExtendedModelExample
    {
        public uint Id { get; set; }
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public int? NullableIntProperty { get; set; }
        public int[] IntArrayProperty { get; set; }
        public IEnumerable<int> IntEnumerableProperty { get; set; }
        public EnumExample EnumProperty { get; set; }
        public EnumExample? NullableEnumProperty { get; set; }
        public IEnumerable<SubModelExample> SubModelsProperty { get; set; }
    }
}
