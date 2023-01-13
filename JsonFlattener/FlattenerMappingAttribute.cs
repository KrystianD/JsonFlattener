namespace JsonFlattener;

public class FlattenerMappingAttribute : Attribute
{
  public string Path { get; }

  public FlattenerMappingAttribute(string path)
  {
    Path = path;
  }
}