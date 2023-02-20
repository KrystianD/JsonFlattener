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

    var emitterPoints = new List<JToken>();
    EnumerateEmitterPoints((JObject)token, flattenAgainst, emitterPoints);

    foreach (var emitterPoint in emitterPoints) {
      var pobj = ProcessEmitterPoint(emitterPoint);

      var obj = new Dictionary<string, JValue>();
      FillDictionaryFromObjectProxy(pobj, new PropPath(), obj);
      objects.Add(obj);
    }

    return objects;
  }

  private static void EnumerateEmitterPoints(JToken token,
                                             string flattenAgainst,
                                             List<JToken> emitterPoints)
  {
    flattenAgainst = ("/" + flattenAgainst.Trim('/') + "/").Replace("//", "/");

    EnumerateEmitterPointsInner((JObject)token, "/", flattenAgainst, emitterPoints);
  }

  // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
  // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
  private static void EnumerateEmitterPointsInner(JToken token,
                                                  string simplePath,
                                                  string flattenAgainst,
                                                  List<JToken> emitterPoints)
  {
    if (simplePath == flattenAgainst) {
      switch (token) {
        case JArray jArray:
          emitterPoints.AddRange(jArray);
          break;
        default:
          emitterPoints.Add(token);
          break;
      }
    }
    else {
      switch (token) {
        case JObject jObject:
          foreach (var pair in jObject) {
            EnumerateEmitterPointsInner(pair.Value, simplePath + pair.Key + "/", flattenAgainst, emitterPoints);
          }

          break;
        case JArray jArray:
          for (int i = 0; i < jArray.Count; i++)
            EnumerateEmitterPointsInner(jArray[i], simplePath, flattenAgainst, emitterPoints);
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

  private class ClassDef
  {
    public class FieldDef
    {
      public string Name;
      
      public Action<object, object?> SetValue;
      public Type FieldType;
      
      public FlattenerMappingAttribute Mapping;
    }

    public List<FieldDef> fields = new();
  }

  private static ClassDef PrepareClass(Type objType)
  {
    // FlattenerProcessorAttribute
    var cls = new ClassDef();

    foreach (var fieldInfo in objType.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
      var attr = fieldInfo.GetCustomAttribute<FlattenerMappingAttribute>();
      if (attr == null)
        continue;

      cls.fields.Add(new ClassDef.FieldDef() {
          Name = fieldInfo.Name,
          SetValue = fieldInfo.SetValue,
          FieldType = fieldInfo.FieldType,
          Mapping = attr,
      });
    }

    foreach (var propertyInfo in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
      var attr = propertyInfo.GetCustomAttribute<FlattenerMappingAttribute>();
      if (attr == null)
        continue;

      cls.fields.Add(new ClassDef.FieldDef() {
          Name = propertyInfo.Name,
          SetValue = propertyInfo.SetValue,
          FieldType = propertyInfo.PropertyType,
          Mapping = attr,
      });
    }

    return cls;
  }

  [SuppressMessage("ReSharper", "HeapView.PossibleBoxingAllocation")]
  public static List<T> FlattenToObject<T>(JToken token, string flattenAgainst) where T : new()
  {
    if (token == null) {
      throw new ArgumentNullException(nameof(token));
    }

    var cls = PrepareClass(typeof(T));

    var emitterPoints = new List<JToken>();
    EnumerateEmitterPoints((JObject)token, flattenAgainst, emitterPoints);

    static JToken? GetByPaths(ObjectProxy jsonObj, FlattenerMappingAttribute attr)
    {
      var token = jsonObj.GetByPath(attr.Path);
      if (token != null)
        return token;

      foreach (var path in attr.AlternativePaths) {
        token = jsonObj.GetByPath(path);
        if (token != null)
          return token;
      }

      return null;
    }

    return emitterPoints
           .Select(ProcessEmitterPoint)
           // ReSharper disable once HeapView.DelegateAllocation
           .Select(jsonObj => {
             var obj = new T();
             foreach (var field in cls.fields) {
               try {
                 var fieldToken = GetByPaths(jsonObj, field.Mapping);
                 if (fieldToken != null)
                   field.SetValue(obj, fieldToken.ToObject(field.FieldType));
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