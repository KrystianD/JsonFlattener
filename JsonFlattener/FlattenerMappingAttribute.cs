using JetBrains.Annotations;

namespace JsonFlattener;

[PublicAPI]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FlattenerMappingAttribute : Attribute
{
  internal Path Path { get; }

  public string[] AlternativePaths
  {
    set { _alternativePaths = value.Select(x => new Path(x)).ToArray(); }
    get { return _alternativePaths.Select(x => x.ToString()).ToArray(); }
  }

  internal Path[] _alternativePaths = Array.Empty<Path>();

  public FlattenerMappingAttribute(string path)
  {
    Path = new Path(path);
  }
}