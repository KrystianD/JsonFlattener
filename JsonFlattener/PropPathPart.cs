namespace JsonFlattener;

public struct PropPathPart
{
  // public PropPathTypeEnum Type;
  public string Name;
  public int? Index;

  public override string ToString()
  {
    if (Index == null)
      return Name;
    else
      return $"{Name}[{Index}]";
  }
}