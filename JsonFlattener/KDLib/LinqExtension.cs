namespace JsonFlattener.KDLib;

public static class LinqExtension
{
  public static string JoinString(this IEnumerable<string> source, string separator = "")
  {
    return string.Join(separator, source);
  }

  public static string JoinString(this IEnumerable<char> source, string separator = "")
  {
    return string.Join(separator, source);
  }
}