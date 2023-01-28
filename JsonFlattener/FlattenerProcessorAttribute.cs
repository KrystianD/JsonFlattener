using Newtonsoft.Json.Linq;

namespace JsonFlattener;

public interface FlattenerProcessor
{
  public object? Processor(JValue value);
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FlattenerProcessorAttribute : Attribute
{
  public FlattenerProcessor Processor { get; }

  public FlattenerProcessorAttribute(Type processorType)
  {
    Processor = (FlattenerProcessor)Activator.CreateInstance(processorType)!;
  }
}