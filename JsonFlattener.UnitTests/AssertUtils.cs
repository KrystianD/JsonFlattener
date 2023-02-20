using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace JsonFlattener.UnitTests;

public static class AssertUtils
{
  public static void AssertFlattened(string json, object[] expectedObjects)
  {
    AssertFlattened(JsonFlattener.Flatten(JToken.Parse(json), ""), expectedObjects);
  }

  public static void AssertFlattened(object json, string unwrapBy, object[] expectedObjects)
  {
    AssertFlattened(JsonFlattener.Flatten(JToken.FromObject(json), unwrapBy), expectedObjects);
  }

  public static void AssertFlattened(string json, string unwrapBy, object[] expectedObjects)
  {
    AssertFlattened(JsonFlattener.Flatten(JToken.Parse(json), unwrapBy), expectedObjects);
  }

  private static void AssertFlattened(List<Dictionary<string, JValue>> res, object[] expectedObjects)
  {
    Assert.Equal(expectedObjects.Length, res.Count);

    foreach (var (outputObject, expectedObject) in res.Zip(expectedObjects)) {
      if (expectedObject is Dictionary<string, object> objDict) {
        Assert.Equal(objDict.Count, outputObject.Count);

        foreach (var (field, outItemField) in objDict.Zip(outputObject)) {
          Assert.Equal(field.Key, outItemField.Key);
          Assert.Equal(field.Value, outItemField.Value.ToObject(field.Value.GetType()));
        }
      }
      else {
        var objType = expectedObject.GetType();

        var fields = objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .ToList();

        Assert.Equal(fields.Count, outputObject.Count);

        foreach (var (field, outItemField) in fields.Zip(outputObject)) {
          Assert.Equal(field.Name, outItemField.Key);
          Assert.Equal(field.GetValue(expectedObject), outItemField.Value.ToObject(field.PropertyType));
        }
      }
    }
  }
}