using System;
using System.Collections.Generic;
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
  public void TestErrors()
  {
    Assert.Throws<ArgumentNullException>(() => JsonFlattener.FlattenToDict(null!, ""));
  }
}