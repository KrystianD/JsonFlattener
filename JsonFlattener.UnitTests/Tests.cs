using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace JsonFlattener.UnitTests;

public class Tests
{
  [Fact]
  public void TestSimple()
  {
    AssertUtils.AssertFlattened(@"{
  ""name1"": ""value1"",
  ""name2"": ""value2""
}", new object[] {
        new {
            name1 = "value1",
            name2 = "value2",
        },
    });
  }

  [Fact]
  public void TestNested()
  {
    AssertUtils.AssertFlattened(@"{
  ""name"": ""value"",
  ""obj"": {
    ""n1"": 1
  }
}", new object[] {
        new Dictionary<string, object>() {
            ["name"] = "value",
            ["obj/n1"] = 1,
        },
    });
  }

  [Fact]
  public void TestUnwrap()
  {
    AssertUtils.AssertFlattened(
        @"{
  ""name"": ""value"",
  ""objs"": [
    {
      ""n1"": 1
    },
    {
      ""n1"": 2
    }
  ]
}",
        "objs",
        new object[] {
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["objs/n1"] = 1,
            },
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["objs/n1"] = 2,
            },
        });
  }

  [Fact]
  public void TestUnwrap2()
  {
    AssertUtils.AssertFlattened(
        new {
            name = "value",
            objs = new[] {
                new {
                    n1 = 1,
                },
                new {
                    n1 = 2,
                },
            },
        },
        "name",
        new object[] {
            new Dictionary<string, object>() {
                ["objs[0]/n1"] = 1,
                ["objs[1]/n1"] = 2,
                ["name"] = "value",
            },
        });
  }

  [Fact]
  public void TestUnwrapNested()
  {
    AssertUtils.AssertFlattened(
        JToken.FromObject(new {
            name = "value",
            sub = new {
                objs = new[] {
                    new {
                        n1 = 1,
                    },
                    new {
                        n1 = 2,
                    },
                },
            },
        }).ToString(),
        "sub/objs",
        new object[] {
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/objs/n1"] = 1,
            },
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/objs/n1"] = 2,
            },
        });
  }

  [Fact]
  public void TestUnwrapNested2()
  {
    AssertUtils.AssertFlattened(
        JToken.FromObject(new {
            name = "value",
            sub = new {
                objs = new[] {
                    new {
                        n1 = 1,
                    },
                    new {
                        n1 = 2,
                    },
                },
            },
        }).ToString(),
        "sub/objs/n1",
        new object[] {
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/objs/n1"] = 1,
            },
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/objs/n1"] = 2,
            },
        });
  }

  [Fact]
  public void TestUnwrapNestedInArray()
  {
    AssertUtils.AssertFlattened(
        JToken.FromObject(new {
            name = "value",
            sub = new[] {
                new {
                    subv = "s1",
                    objs = new[] {
                        new {
                            n1 = 1,
                        },
                        new {
                            n1 = 2,
                        },
                    },
                },
                new {
                    subv = "s2",
                    objs = new[] {
                        new {
                            n1 = 3,
                        },
                        new {
                            n1 = 4,
                        },
                    },
                },
            },
        }).ToString(),
        "sub/objs",
        new object[] {
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/subv"] = "s1",
                ["sub/objs/n1"] = 1,
            },
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/subv"] = "s1",
                ["sub/objs/n1"] = 2,
            },
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/subv"] = "s2",
                ["sub/objs/n1"] = 3,
            },
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["sub/subv"] = "s2",
                ["sub/objs/n1"] = 4,
            },
        });
  }

  [Fact]
  public void TestArray()
  {
    AssertUtils.AssertFlattened(
        JToken.FromObject(new {
            name = "value",
            objs = new[] {
                new {
                    n1 = 3,
                },
                new {
                    n1 = 4,
                },
            },
        }).ToString(),
        new object[] {
            new Dictionary<string, object>() {
                ["name"] = "value",
                ["objs[0]/n1"] = 3,
                ["objs[1]/n1"] = 4,
            },
        });
  }

  [Fact]
  public void TestObjectProxyGetByPath()
  {
    var data = JToken.FromObject(new {
        name = "value",
        sub1 = new {
            sub1_val = 1,
            sub2 = new[] {
                new {
                    sub2_val = 11,
                    sub3 = new[] {
                        new {
                            n2 = 1,
                        },
                        new {
                            n2 = 2,
                        },
                    },
                },
                new {
                    sub2_val = 22,
                    sub3 = new[] {
                        new {
                            n2 = 3,
                        },
                    },
                },
            },
        },
    });

    Assert.Collection(
        JsonFlattener.FlattenToProxy(data, "sub1/sub2/sub3").Select(x => JsonConvert.SerializeObject(x.GetByPath(""))),
        json => Assert.Equal("{\"name\":\"value\",\"sub1\":{\"sub1_val\":1,\"sub2\":{\"sub2_val\":11,\"sub3\":{\"n2\":1}}}}", json),
        json => Assert.Equal("{\"name\":\"value\",\"sub1\":{\"sub1_val\":1,\"sub2\":{\"sub2_val\":11,\"sub3\":{\"n2\":2}}}}", json),
        json => Assert.Equal("{\"name\":\"value\",\"sub1\":{\"sub1_val\":1,\"sub2\":{\"sub2_val\":22,\"sub3\":{\"n2\":3}}}}", json));

    Assert.Collection(
        JsonFlattener.FlattenToProxy(data, "sub1/sub2/sub3").Select(x => JsonConvert.SerializeObject(x.GetByPath("sub1"))),
        json => Assert.Equal("{\"sub1_val\":1,\"sub2\":{\"sub2_val\":11,\"sub3\":{\"n2\":1}}}", json),
        json => Assert.Equal("{\"sub1_val\":1,\"sub2\":{\"sub2_val\":11,\"sub3\":{\"n2\":2}}}", json),
        json => Assert.Equal("{\"sub1_val\":1,\"sub2\":{\"sub2_val\":22,\"sub3\":{\"n2\":3}}}", json));

    Assert.Collection(
        JsonFlattener.FlattenToProxy(data, "sub1/sub2/sub3").Select(x => JsonConvert.SerializeObject(x.GetByPath("sub1/sub2"))),
        json => Assert.Equal("{\"sub2_val\":11,\"sub3\":{\"n2\":1}}", json),
        json => Assert.Equal("{\"sub2_val\":11,\"sub3\":{\"n2\":2}}", json),
        json => Assert.Equal("{\"sub2_val\":22,\"sub3\":{\"n2\":3}}", json));

    Assert.Collection(
        JsonFlattener.FlattenToProxy(data, "sub1/sub2/sub3").Select(x => JsonConvert.SerializeObject(x.GetByPath("sub1/sub2/sub3"))),
        json => Assert.Equal("{\"n2\":1}", json),
        json => Assert.Equal("{\"n2\":2}", json),
        json => Assert.Equal("{\"n2\":3}", json));
  }

  [Fact]
  public void TestErrors()
  {
    Assert.Throws<ArgumentNullException>(() => JsonFlattener.FlattenToDict(null!, ""));
  }
}