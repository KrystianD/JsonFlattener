using Newtonsoft.Json.Linq;

namespace JsonFlattener;

internal static class Utils
{
  public static JObject ShallowCloneWithoutKey(JObject obj, string keyToExclude)
  {
    var newObj = new JObject();

    foreach (var jProperty in obj.Properties())
      if (jProperty.Name != keyToExclude)
        newObj[jProperty.Name] = jProperty.Value;

    return newObj;
  }
}