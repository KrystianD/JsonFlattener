namespace JsonFlattener;

public readonly struct PropPath
{
  private struct Part
  {
    public string Name;
    public int? Index;

    public override string ToString()
    {
      return Index == null ? Name : $"{Name}[{Index}]";
    }
  }

  private Part[] Parts { get; }

  public PropPath()
  {
    Parts = Array.Empty<Part>();
  }

  private PropPath(IEnumerable<Part> parts)
  {
    Parts = parts.ToArray();
  }

  public PropPath Append(string name)
  {
    var newParts = new List<Part>(Parts);
    newParts.Add(new Part() {
        Name = name,
    });
    return new PropPath(newParts);
  }

  public PropPath AppendIndex(int index)
  {
    var newParts = new List<Part>(
        Parts.Take(Parts.Length - 1).Append(new Part() {
            Name = Parts.Last().Name,
            Index = index,
        }));
    return new PropPath(newParts);
  }

  public string GetSimplePath()
  {
    return string.Join('/', Parts.Select(x => x.Name));
  }

  public override bool Equals(object? obj)
  {
    return obj != null && ToString().Equals(obj.ToString());
  }

  public override int GetHashCode()
  {
    return ToString().GetHashCode();
  }

  public override string ToString()
  {
    return string.Join('/', Parts.Select(x => x.ToString()));
  }
}