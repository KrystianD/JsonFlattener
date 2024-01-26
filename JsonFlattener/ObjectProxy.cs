using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace JsonFlattener;

internal class PathItem
{
  public readonly string? NextKey;
  public readonly JToken OuterJson;

  public PathItem(string? nextKey, JToken outerJson)
  {
    NextKey = nextKey;
    OuterJson = outerJson;
  }
}

[PublicAPI]
public class ObjectProxy
{
  private readonly List<PathItem> PathItems;

  internal ObjectProxy(List<PathItem> pathItems)
  {
    PathItems = pathItems;
  }

  public JToken? GetByPath(string path)
  {
    var parts = path.Split('/');
    return GetByPath(new Path(parts));
  }

  internal JToken? GetByPath(Path path)
  {
    for (int i = 0; i < PathItems.Count; i++) {
      if (i >= path.Parts.Length) {
        var jsonPath = string.Join('.', path.Parts.Skip(i));
        return PathItems[i].OuterJson.SelectToken(jsonPath);
      }

      if (path.Parts[i] != PathItems[i].NextKey) {
        var jsonPath = string.Join('.', path.Parts.Skip(i));
        return PathItems[i].OuterJson.SelectToken(jsonPath);
      }
    }

    return null;
  }

  public Dictionary<string, JValue> GetAsDictionary()
  {
    var obj = new Dictionary<string, JValue>();
    FillDictionaryFromObjectProxy(obj);
    return obj;
  }

  private void FillDictionaryFromObjectProxy(Dictionary<string, JValue> obj)
  {
    var curPath = new PropPath();
    foreach (var pathItem in PathItems) {
      if (pathItem.NextKey == null) {
        FillDictionaryFromJToken(pathItem.OuterJson, curPath, obj);
      }
      else {
        FillDictionaryFromJToken(pathItem.OuterJson, curPath, obj, skipProperty: pathItem.NextKey);
        curPath = curPath.Append(pathItem.NextKey);
      }
    }
  }

  private static void FillDictionaryFromJToken(JToken token,
                                               PropPath path,
                                               Dictionary<string, JValue> obj,
                                               string? skipProperty = null)
  {
    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
    switch (token) {
      case JObject jObject:
        foreach (JProperty prop in jObject.Properties()) {
          if (prop.Name == skipProperty)
            continue;
          FillDictionaryFromJToken(prop.Value, path.Append(prop.Name), obj);
        }

        break;

      case JArray jArray:
        for (int i = 0; i < jArray.Count; i++)
          FillDictionaryFromJToken(jArray[i], path.AppendIndex(i), obj);
        break;

      default:
        obj.Add(path.ToString(), (JValue)token);
        break;
    }
  }

  public T GetAsObject<T>() where T : class, new()
  {
    var obj = new T();
    JsonFlattener.FillClassFields(this, obj);
    return obj;
  }

  public void GetIntoObject(object obj)
  {
    JsonFlattener.FillClassFields(this, obj);
  }
}