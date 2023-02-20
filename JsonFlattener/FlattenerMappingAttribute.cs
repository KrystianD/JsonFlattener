namespace JsonFlattener;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FlattenerMappingAttribute : Attribute
{
  public string Path { get; }

  public string[] AlternativePaths = Array.Empty<string>();

  public FlattenerMappingAttribute(string path)
  {
    Path = path;
  }
}