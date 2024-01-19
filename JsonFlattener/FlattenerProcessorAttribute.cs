using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace JsonFlattener;

public interface IFlattenerProcessor
{
  public object? Processor(JToken value);
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FlattenerProcessorAttribute : Attribute
{
  public IFlattenerProcessor Processor { get; }

  public FlattenerProcessorAttribute(Type processorType)
  {
    Processor = (IFlattenerProcessor)Activator.CreateInstance(processorType)!;
  }
}