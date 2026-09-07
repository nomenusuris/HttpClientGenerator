using HttpClient.Generator.Tests.Models;
using HttpClient.Generator.Tests.Models.Common;
using HttpClient.Generator.Tests.Services;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HttpClient.Generator.Tests.Implementations.Services
{
    internal class CrudServiceMock : Mock<ICrudService>
    {
        public CrudServiceMock()
        {
            Setup(s => s.GetPageAsync(It.IsAny<PageRequestExample>()))
                .Returns(Task.FromResult(Page));
            Setup(s => s.GetAsync(It.IsAny<uint>()))
                .Returns<uint>((id) => Task.FromResult(Page.Items.First(i => i.Id == id)));
            Setup(s => s.GetByIdsAsync(It.IsAny<QueryArrayExampleTyped<uint>>()))
               .Returns<QueryArrayExampleTyped<uint>>((ids) => Task.FromResult(Page.Items.Where(i => ids.Contains(i.Id)).ToArray()));
            Setup(s => s.CreateAsync(It.IsAny<ModelExample>()))
                .Returns(Task.FromResult(5u));
            Setup(s => s.UpdateAsync(It.IsAny<uint>(), It.IsAny<ModelExample>()))
                .Returns(Task.FromResult(0));
            Setup(s => s.DeleteAsync(It.IsAny<uint>()))
                .Returns(Task.FromResult(0));
        }

        public PageExample<ModelExample> Page => new PageExample<ModelExample>()
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 100,
            Items = new[]
            {
                new ModelExample()
                {
                    Id = 1,
                    IntArrayProperty = new []{ 1, 2, 3 },
                    IntProperty = 1,
                    NullableIntProperty = null,
                    StringProperty = "FirstModel",
                    SubModelsProperty = new []
                    {
                        new SubModelExample()
                        {
                            SubId = 1,
                            SubEnumProperty = EnumExample.FirstEnumValue,
                            SubFloatArrayProperty = new [] {1.0f,2.0f},
                            SubGuidArrayProperty = new []{Guid.Parse("{960710C2-5E26-4F25-920A-4073C6CBFACD}"), Guid.Parse("{4BB96514-4AC7-4985-ADC5-B490FB0A8E56}") },
                            SubIntArrayProperty = new []{1,2,3,4},
                            SubIntEnumerableProperty = new [] { 10,20,30,40},
                            SubIntProperty = 1,
                            SubNullableEnumProperty = null,
                            SubNullableIntProperty = null,
                            SubStringProperty= "SubModel_1"
                        },
                        new SubModelExample()
                        {
                            SubId = 2,
                            SubEnumProperty = EnumExample.FirstEnumValue,
                            SubFloatArrayProperty = new [] {3.0f,4.0f},
                            SubGuidArrayProperty = new []{Guid.Parse("{120710C2-5E26-4F25-920A-4073C6CBFACD}"), Guid.Parse("{34B96514-4AC7-4985-ADC5-B490FB0A8E56}") },
                            SubIntArrayProperty = new []{1,2,3,4},
                            SubIntEnumerableProperty = new [] { 50,60,70},
                            SubIntProperty = 2,
                            SubNullableEnumProperty = EnumExample.SecondEnumValue,
                            SubNullableIntProperty = 2,
                            SubStringProperty= "SubModel_2"
                        },
                    }
                },
                new ModelExample()
                {
                    Id = 2,
                    IntArrayProperty = new []{ 4, 5, 6 },
                    IntProperty = 2,
                    NullableIntProperty = 2,
                    StringProperty = "Second"
                },
                new ModelExample()
                {
                    Id = 3,
                    IntArrayProperty = null,
                    IntProperty = 3,
                    NullableIntProperty = 3,
                    StringProperty = "Third"
                }
            }
        };
    }
}
