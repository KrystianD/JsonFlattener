using System.Reflection;
using Newtonsoft.Json.Linq;

namespace JsonFlattener
{
  public static class JsonFlattener
  {
    public static List<Dictionary<string, JValue>> Flatten(JToken token, string flattenAgainst)
    {
      if (token == null) {
        throw new ArgumentNullException(nameof(token));
      }

      var s = new PropsStack();
      var objects = new List<Dictionary<string, JValue>>();
      FillDictionaryFromJToken(token, new PropPath(), 0, flattenAgainst, s, null, null, objects);
      return objects;
    }

    public static List<T> FlattenToObject<T>(JToken token, string flattenAgainst) where T : new()
    {
      if (token == null) {
        throw new ArgumentNullException(nameof(token));
      }

      var objType = typeof(T);
      var fields = objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                          .Where(x => x.GetCustomAttribute<FlattenerMappingAttribute>() is not null)
                          .Select(x => (Field: x, FlattenerPath: x.GetCustomAttribute<FlattenerMappingAttribute>()!.Path))
                          .ToList();

      return Flatten(token, flattenAgainst)
             .Select(objDict => {
               var obj = new T();
               foreach (var (field, flattenerPath) in fields) {
                 try {
                   if (objDict.TryGetValue(flattenerPath, out var objValue))
                     field.SetValue(obj, objValue.ToObject(field.PropertyType));
                   else
                     field.SetValue(obj, null);
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

    private static void FillDictionaryFromJToken(JToken token,
                                                 PropPath path,
                                                 int level,
                                                 string flattenAgainst, PropsStack propsStack,
                                                 Dictionary<string, JValue> obj,
                                                 int? objLevelStart, List<Dictionary<string, JValue>> objects)
    {
      switch (token.Type) {
        case JTokenType.Object:
          var emitter = false;
          if (path.GetSimplePath() == flattenAgainst) {
            emitter = true;
            obj = new Dictionary<string, JValue>();
            for (var index = 0; index < propsStack.propObjects.Count; index++) {
              var propObject = propsStack.propObjects[index];

              var toRemove = 0;

              if (index > 0 && propObject.Count > 0) {
                // var prefixFull = path.Take(index);
                //
                // var firstProp = propObject.First().Key;
                // if (firstProp.Take(index).Equals(prefixFull)) {
                toRemove = index;
                // }
              }

              foreach (var keyValuePair in propObject) {
                obj[keyValuePair.Key.MakeStartSimple(toRemove).ToString()] = keyValuePair.Value;
              }
            }

            objLevelStart = level;
          }

          propsStack.Enter();
          foreach (JProperty prop in token.Children<JProperty>()) {
            FillDictionaryFromJToken(prop.Value,
                                     path.Append(prop.Name),
                                     level + 1,
                                     flattenAgainst, propsStack, obj,
                                     objLevelStart, objects);
          }

          propsStack.Exit();

          if (emitter) {
            objects.Add(obj);
          }

          break;

        case JTokenType.Array:
        {
          int index = 0;
          foreach (JToken value in token.Children()) {
            FillDictionaryFromJToken(value,
                                     path.AppendIndex(index),
                                     level,
                                     flattenAgainst, propsStack, obj,
                                     objLevelStart, objects);
            index++;
          }
        }
          break;

        default:
          propsStack.curObj[path] = (JValue)token;
          obj?.Add(path.MakeStartSimple(objLevelStart.Value).ToString(), (JValue)token);
          break;
      }
    }
  }
}