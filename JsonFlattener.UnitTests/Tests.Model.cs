using Newtonsoft.Json.Linq;
using Xunit;
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace JsonFlattener.UnitTests;

public class TestsModel
{
  private class Model
  {
    [FlattenerMapping("name")]
    public readonly string Name = null!; // as field

    [FlattenerMapping("objs/n1")]
    public int N1 { get; set; } // as property
  }

  [Fact]
  public void TestSimple()
  {
    var obj = new {
        name = "value",
        objs = new[] {
            new {
                n1 = 1,
            },
            new {
                n1 = 2,
            },
        },
    };

    var objs = JsonFlattener.FlattenToObject<Model>(JToken.FromObject(obj), "objs");

    Assert.Collection(objs,
                      x => {
                        Assert.Equal("value", x.Name);
                        Assert.Equal(1, x.N1);
                      },
                      x => {
                        Assert.Equal("value", x.Name);
                        Assert.Equal(2, x.N1);
                      });
  }
}