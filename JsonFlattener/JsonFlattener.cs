using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace JsonFlattener;

internal class Path
{
  public readonly string[] Parts;

  public Path(string[] parts)
  {
    Parts = parts;
  }

  public Path(string path)
  {
    Parts = path.Split('/');
  }

  public override string ToString()
  {
    return string.Join('/', Parts);
  }
}

public class PathItem
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

  public ObjectProxy(List<PathItem> pathItems)
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
}

[PublicAPI]
public static class JsonFlattener
{
  public static List<ObjectProxy> FlattenToProxy(JToken token, string flattenAgainst)
  {
    if (token == null) {
      throw new ArgumentNullException(nameof(token));
    }

    var emitterPoints = new List<JToken>();
    EnumerateEmitterPoints((JObject)token, flattenAgainst, emitterPoints);

    return emitterPoints.Select(ProcessEmitterPoint).ToList();
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
          foreach (var pair in jObject)
            EnumerateEmitterPointsInner(pair.Value!, simplePath + pair.Key + "/", flattenAgainst, emitterPoints);
          break;
        case JArray jArray:
          // ReSharper disable once ForCanBeConvertedToForeach
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

    path.Add(new PathItem(null, emitterPoint));

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
      path.Add(new PathItem(curPropName, parentObject));

      var parentProp = (JProperty?)Parent(parentObject);
      if (parentProp == null) {
        path.Reverse();
        return new ObjectProxy(path);
      }

      curPropName = parentProp.Name;
      parentObject = (JObject)Parent(parentProp);
    }
  }

  private class ClassDef
  {
    public class FieldDef
    {
      public readonly string Name;

      public readonly Action<object, object?> SetValue;
      public readonly Type FieldType;

      public readonly FlattenerMappingAttribute Mapping;

      public readonly Func<JValue, object?>? Processor;

      public FieldDef(string name, Action<object, object?> setValue, Type fieldType, FlattenerMappingAttribute mapping, Func<JValue, object?>? processor)
      {
        Name = name;
        SetValue = setValue;
        FieldType = fieldType;
        Mapping = mapping;
        Processor = processor;
      }
    }

    public readonly List<FieldDef> Fields = new();
  }

  private static ClassDef PrepareClass(Type objType)
  {
    // FlattenerProcessorAttribute
    var cls = new ClassDef();

    foreach (var fieldInfo in objType.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
      var attr = fieldInfo.GetCustomAttribute<FlattenerMappingAttribute>();
      if (attr == null)
        continue;

      var processor = fieldInfo.GetCustomAttribute<FlattenerProcessorAttribute>();

      cls.Fields.Add(new ClassDef.FieldDef(
                         name: fieldInfo.Name,
                         setValue: fieldInfo.SetValue,
                         fieldType: fieldInfo.FieldType,
                         mapping: attr,
                         processor: processor == null ? null : processor.Processor.Processor
                     ));
    }

    foreach (var propertyInfo in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
      var attr = propertyInfo.GetCustomAttribute<FlattenerMappingAttribute>();
      if (attr == null)
        continue;

      var processor = propertyInfo.GetCustomAttribute<FlattenerProcessorAttribute>();

      cls.Fields.Add(new ClassDef.FieldDef(
                         name: propertyInfo.Name,
                         setValue: propertyInfo.SetValue,
                         fieldType: propertyInfo.PropertyType,
                         mapping: attr,
                         processor: processor == null ? null : processor.Processor.Processor
                     ));
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

      foreach (var path in attr._alternativePaths) {
        token = jsonObj.GetByPath(path);
        if (token != null)
          return token;
      }

      return null;
    }

    return emitterPoints
           .Select(ProcessEmitterPoint)
           .Select(jsonObj => {
             var obj = new T();
             foreach (var field in cls.Fields) {
               try {
                 var fieldToken = GetByPaths(jsonObj, field.Mapping);
                 if (fieldToken != null) {
                   if (field.Processor != null) {
                     var o = field.Processor((JValue)fieldToken);
                     if (o is JValue jValue)
                       field.SetValue(obj, jValue.ToObject(field.FieldType));
                     else
                       field.SetValue(obj, o);
                   }
                   else {
                     field.SetValue(obj, fieldToken.ToObject(field.FieldType));
                   }
                 }
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

  public static List<Dictionary<string, JValue>> FlattenToDict(JToken token, string flattenAgainst)
  {
    return FlattenToProxy(token, flattenAgainst).Select(objectProxy => objectProxy.GetAsDictionary()).ToList();
  }
}