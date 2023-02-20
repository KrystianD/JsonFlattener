using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace JsonFlattener;

public static class JsonFlattener
{
  public static List<Dictionary<string, JValue>> Flatten(JToken token, string flattenAgainst)
  {
    if (token == null) {
      throw new ArgumentNullException(nameof(token));
    }

    var objects = new List<Dictionary<string, JValue>>();

    var emitterPoints = new List<JToken>();
    EnumerateEmitterPoints((JObject)token, new PropPath(), flattenAgainst, emitterPoints);

    foreach (var emitterPoint in emitterPoints) {
      var pobj = ProcessEmitterPoint(emitterPoint);

      var obj = new Dictionary<string, JValue>();
      FillDictionaryFromJToken(pobj, new PropPath(), obj);
      objects.Add(obj);
    }

    return objects;
  }

  // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
  // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
  private static void EnumerateEmitterPoints(JToken token,
                                             PropPath path,
                                             string flattenAgainst,
                                             List<JToken> emitterPoints)
  {
    if (path.GetSimplePath() == flattenAgainst) {
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
            EnumerateEmitterPoints(prop.Value, path.Append(prop.Name), flattenAgainst, emitterPoints);
          }

          break;
        case JTokenType.Array:
        {
          int index = 0;
          foreach (JToken arrayItem in token.Children()) {
            EnumerateEmitterPoints(arrayItem, path.AppendIndex(index), flattenAgainst, emitterPoints);
            index++;
          }
        }
          break;
      }
    }
  }

  private static JObject ProcessEmitterPoint(JToken emitterPoint)
  {
    static JToken Parent(JToken x)
    {
      JToken? p = x;
      p = p.Parent;
      while (p is JArray)
        p = p.Parent;
      return p!;
    }

    JProperty curProp;
    JObject parentObject;
    if (emitterPoint.Parent is JArray emitterArray) {
      // extract single value from the array against which we are unwrapping
      var parentProp = (JProperty)emitterArray.Parent!;
      parentObject = (JObject)parentProp.Parent!;
      curProp = new JProperty(parentProp.Name, emitterPoint);
    }
    else {
      var parentProp = (JProperty?)emitterPoint.Parent;

      // emitter point is the root object, just return it
      if (parentProp == null) {
        return (JObject)emitterPoint;
      }

      curProp = parentProp;
      parentObject = (JObject)curProp.Parent!;
    }


    while (true) {
      var prop = curProp;
      JObject newObj = new JObject(parentObject.Properties().Where(x => x.Name != prop.Name));
      newObj.Add(curProp);

      var parentProp = (JProperty?)Parent(parentObject);
      if (parentProp == null) {
        return newObj;
      }

      curProp = new JProperty(parentProp.Name, newObj);
      parentObject = (JObject)Parent(parentProp);
    }
  }

  private static void FillDictionaryFromJToken(JToken token,
                                               PropPath path,
                                               Dictionary<string, JValue> obj)
  {
    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
    switch (token.Type) {
      case JTokenType.Object:
        foreach (JProperty prop in token.Children<JProperty>()) {
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

    return Flatten(token, flattenAgainst)
           // ReSharper disable once HeapView.DelegateAllocation
           .Select(objDict => {
             var obj = new T();
             foreach (var (field, flattenerPath) in fields) {
               try {
                 if (objDict.TryGetValue(flattenerPath, out var objValue))
                   field.SetValue(obj, objValue.ToObject(field.FieldType));
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
                 if (objDict.TryGetValue(flattenerPath, out var objValue))
                   field.SetValue(obj, objValue.ToObject(field.PropertyType));
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