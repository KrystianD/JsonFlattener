namespace JsonFlattener;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FlattenerMappingAttribute : Attribute
{
  internal Path Path { get; }

  internal Path[] AlternativePaths = Array.Empty<Path>();

  public FlattenerMappingAttribute(string path)
  {
    Path = new Path(path);
  }
}