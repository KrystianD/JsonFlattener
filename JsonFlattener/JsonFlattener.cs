using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace JsonFlattener;

public class PathItem
{
  public string? SkipKey;
  public JToken OuterJson;
}

public class ObjectProxy
{
  public List<PathItem> pathItems;

  public ObjectProxy(List<PathItem> pathItems)
  {
    this.pathItems = pathItems;
  }

  public JToken? GetByPath(string path)
  {
    var parts = path.Split('/');

    for (int i = 0; i < pathItems.Count; i++) {
      if (i >= parts.Length) {
        var jsonPath = string.Join('.', parts.Skip(i));
        return pathItems[i].OuterJson.SelectToken(jsonPath);
      }

      if (parts[i] != pathItems[i].SkipKey) {
        var jsonPath = string.Join('.', parts.Skip(i));
        return pathItems[i].OuterJson.SelectToken(jsonPath);
      }
    }

    return null;
  }
}

public static class JsonFlattener
{
  public static List<Dictionary<string, JValue>> Flatten(JToken token, string flattenAgainst)
  {
    if (token == null) {
      throw new ArgumentNullException(nameof(token));
    }

    var objects = new List<Dictionary<string, JValue>>();

    flattenAgainst = ("/" + flattenAgainst.Trim('/') + "/").Replace("//", "/");

    var emitterPoints = new List<JToken>();
    EnumerateEmitterPoints((JObject)token, "/", flattenAgainst, emitterPoints);

    foreach (var emitterPoint in emitterPoints) {
      var pobj = ProcessEmitterPoint(emitterPoint);

      var obj = new Dictionary<string, JValue>();
      FillDictionaryFromObjectProxy(pobj, new PropPath(), obj);
      objects.Add(obj);
    }

    return objects;
  }

  // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
  // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
  private static void EnumerateEmitterPoints(JToken token,
                                             string simplePath,
                                             string flattenAgainst,
                                             List<JToken> emitterPoints)
  {
    if (simplePath == flattenAgainst) {
      switch (token.Type) {
        case JTokenType.Array:
          emitterPoints.AddRange(token.Children());
          break;
        default:
          emitterPoints.Add(token);
          break;
      }
    }
    else {
      switch (token.Type) {
        case JTokenType.Object:
          foreach (JProperty prop in token.Children<JProperty>()) {
            EnumerateEmitterPoints(prop.Value, simplePath + prop.Name + "/", flattenAgainst, emitterPoints);
          }

          break;
        case JTokenType.Array:
        {
          int index = 0;
          foreach (JToken arrayItem in token.Children()) {
            EnumerateEmitterPoints(arrayItem, simplePath, flattenAgainst, emitterPoints);
            index++;
          }
        }
          break;
      }
    }
  }

  private static ObjectProxy ProcessEmitterPoint(JToken emitterPoint)
  {
    static JToken Parent(JToken x)
    {
      JToken? p = x;
      p = p.Parent;
      while (p is JArray)
        p = p.Parent;
      return p!;
    }

    var path = new List<PathItem>(10);

    path.Add(new PathItem() { OuterJson = emitterPoint });

    // JProperty curProp;
    string curPropName;
    JObject parentObject;
    if (emitterPoint.Parent is JArray emitterArray) {
      // extract single value from the array against which we are unwrapping
      var parentProp = (JProperty)emitterArray.Parent!;
      parentObject = (JObject)parentProp.Parent!;
      curPropName = parentProp.Name;
    }
    else {
      var parentProp = (JProperty?)emitterPoint.Parent;

      // emitter point is the root object, just return it
      if (parentProp == null) {
        path.Reverse();
        return new ObjectProxy(path);
      }

      var curProp = parentProp;
      parentObject = (JObject)curProp.Parent!;
      curPropName = curProp.Name;
    }

    while (true) {
      path.Add(new PathItem() { SkipKey = curPropName, OuterJson = parentObject });

      var parentProp = (JProperty?)Parent(parentObject);
      if (parentProp == null) {
        path.Reverse();
        return new ObjectProxy(path);
      }

      curPropName = parentProp.Name;
      parentObject = (JObject)Parent(parentProp);
    }
  }

  private static void FillDictionaryFromObjectProxy(ObjectProxy proxy,
                                                    PropPath path,
                                                    Dictionary<string, JValue> obj)
  {
    var curPath = new PropPath();
    foreach (var pathItem in proxy.pathItems) {
      if (pathItem.SkipKey == null) {
        FillDictionaryFromJToken(pathItem.OuterJson, curPath, obj);
      }
      else {
        FillDictionaryFromJToken(pathItem.OuterJson, curPath, obj, skipProperty: pathItem.SkipKey);
        curPath = curPath.Append(pathItem.SkipKey);
      }
    }
  }

  private static void FillDictionaryFromJToken(JToken token,
                                               PropPath path,
                                               Dictionary<string, JValue> obj,
                                               string? skipProperty = null)
  {
    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
    switch (token.Type) {
      case JTokenType.Object:
        foreach (JProperty prop in token.Children<JProperty>()) {
          if (prop.Name == skipProperty)
            continue;
          FillDictionaryFromJToken(prop.Value, path.Append(prop.Name), obj);
        }

        break;

      case JTokenType.Array:
      {
        int index = 0;
        foreach (JToken value in token.Children()) {
          FillDictionaryFromJToken(value, path.AppendIndex(index), obj);
          index++;
        }
      }
        break;

      default:
        obj.Add(path.ToString(), (JValue)token);
        break;
    }
  }

  [SuppressMessage("ReSharper", "HeapView.PossibleBoxingAllocation")]
  public static List<T> FlattenToObject<T>(JToken token, string flattenAgainst) where T : new()
  {
    if (token == null) {
      throw new ArgumentNullException(nameof(token));
    }

    var objType = typeof(T);
    var fields = objType.GetFields(BindingFlags.Instance | BindingFlags.Public)
                        .Where(x => x.GetCustomAttribute<FlattenerMappingAttribute>() is not null)
                        .Select(x => (Field: x, FlattenerPath: x.GetCustomAttribute<FlattenerMappingAttribute>()!.Path))
                        .ToList();

    var props = objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(x => x.GetCustomAttribute<FlattenerMappingAttribute>() is not null)
                       .Select(x => (Field: x, FlattenerPath: x.GetCustomAttribute<FlattenerMappingAttribute>()!.Path))
                       .ToList();

    var emitterPoints = new List<JToken>();
    EnumerateEmitterPoints((JObject)token, "", flattenAgainst, emitterPoints);

    return emitterPoints
           .Select(ProcessEmitterPoint)
           // ReSharper disable once HeapView.DelegateAllocation
           .Select(jsonObj => {
             // return new T();
             var obj = new T();
             foreach (var (field, flattenerPath) in fields) {
               try {
                 var token = jsonObj.GetByPath(flattenerPath);
                 if (token != null)
                   field.SetValue(obj, token.ToObject(field.FieldType));
               }
               catch (FormatException e) {
                 Console.WriteLine($"field: {field.Name} - {e.Message}");
                 throw;
               }
               catch (ArgumentException e) {
                 Console.WriteLine($"field: {field.Name} - {e.Message}");
                 throw;
               }
             }

             foreach (var (field, flattenerPath) in props) {
               try {
                 var token = jsonObj.GetByPath(flattenerPath);
                 if (token != null)
                   field.SetValue(obj, token.ToObject(field.PropertyType));
               }
               catch (FormatException e) {
                 Console.WriteLine($"field: {field.Name} - {e.Message}");
                 throw;
               }
               catch (ArgumentException e) {
                 Console.WriteLine($"field: {field.Name} - {e.Message}");
                 throw;
               }
             }

             return obj;
           }).ToList();
  }
}