using System;
using System.Collections.Generic;

namespace HttpClient.Generator.Tests.Models
{
    public class SubModelExample
    {
        public uint SubId { get; set; }
        public string SubStringProperty { get; set; }
        public int SubIntProperty { get; set; }
        public int? SubNullableIntProperty { get; set; }
        public int[] SubIntArrayProperty { get; set; }
        public Guid[] SubGuidArrayProperty { get; set; }
        public float[] SubFloatArrayProperty { get; set; }
        public IEnumerable<int> SubIntEnumerableProperty { get; set; }
        public EnumExample SubEnumProperty { get; set; }
        public EnumExample? SubNullableEnumProperty { get; set; }
    }
}
