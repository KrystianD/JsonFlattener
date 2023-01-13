using JsonFlattener.KDLib;

namespace JsonFlattener;

public readonly struct PropPath
{
  public PropPathPart[] Parts { get; private init; }

  public PropPath()
  {
    Parts = Array.Empty<PropPathPart>();
  }

  private PropPath(IEnumerable<PropPathPart> parts)
  {
    Parts = parts.ToArray();
  }

  public PropPath Append(string name)
  {
    var newParts = new List<PropPathPart>(Parts);
    newParts.Add(new PropPathPart() {
        Name = name,
    });
    return new PropPath(newParts);
  }

  public PropPath AppendIndex(int index)
  {
    var newParts = new List<PropPathPart>(
        Parts.Take(Parts.Length - 1).Append(new PropPathPart() {
            Name = Parts.Last().Name,
            Index = index,
        }));
    // newParts.Add(new PropPathPart() {
    // Index = index,
    // });
    return new PropPath(newParts);
  }

  public string GetSimplePath()
  {
    return Parts.Select(x => x.Name).JoinString("/");
  }

  public override bool Equals(object? obj)
  {
    return ToString().Equals(obj.ToString());
  }

  public override int GetHashCode()
  {
    return ToString().GetHashCode();
  }

  public override string ToString()
  {
    return Parts.Select(x => x.ToString()).JoinString("/");
  }

  public PropPath Take(int length)
  {
    if (length == Parts.Length)
      return this;
    return new PropPath(Parts.Take(length));
  }

  public PropPath Skip(int length)
  {
    if (length == 0)
      return this;
    return new PropPath(Parts.Skip(length));
  }

  public PropPath MakeStartSimple(int length)
  {
    if (length == 0)
      return this;
    return new PropPath(Parts.Take(length).Select(x => new PropPathPart() { Name = x.Name }).Concat(Parts.Skip(length)));
  }
}