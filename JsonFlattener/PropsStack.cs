using Newtonsoft.Json.Linq;

namespace JsonFlattener;

class PropsStack
{
  public Dictionary<PropPath, JValue>? curObj;
  public List<Dictionary<PropPath, JValue>> propObjects = new();

  public PropsStack()
  {
    // Enter();
  }

  public void Enter()
  {
    curObj = new();
    propObjects.Add(curObj);
  }

  public void Exit()
  {
    propObjects.Remove(curObj);
    curObj = propObjects.LastOrDefault();
  }
}