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