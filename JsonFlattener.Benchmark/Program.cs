using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JsonFlattener;
using Newtonsoft.Json.Linq;

[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Model
{
  [FlattenerMapping("objects/num1")]
  public int num1 { get; set; }

  [FlattenerMapping("objects/num2")]
  public int num2 { get; set; }

  [FlattenerMapping("objects/num3")]
  public int num3 { get; set; }

  [FlattenerMapping("objects/num4")]
  public int num4 { get; set; }

  [FlattenerMapping("objects/num5")]
  public int num5 { get; set; }

  [FlattenerMapping("objects/num6")]
  public int num6 { get; set; }

  [FlattenerMapping("objects/num7")]
  public int num7 { get; set; }

  [FlattenerMapping("objects/num8")]
  public int num8 { get; set; }

  [FlattenerMapping("objects/num9")]
  public int num9 { get; set; }


  [FlattenerMapping("objects/subobjects/num1")]
  public int subobject_num1 { get; set; }

  [FlattenerMapping("objects/subobjects/num2")]
  public int subobject_num2 { get; set; }

  [FlattenerMapping("objects/subobjects/num3")]
  public int subobject_num3 { get; set; }

  [FlattenerMapping("objects/subobjects/num4")]
  public int subobject_num4 { get; set; }

  [FlattenerMapping("objects/subobjects/num5")]
  public int subobject_num5 { get; set; }

  [FlattenerMapping("objects/subobjects/num6")]
  public int subobject_num6 { get; set; }

  [FlattenerMapping("objects/subobjects/num7")]
  public int subobject_num7 { get; set; }

  [FlattenerMapping("objects/subobjects/num8")]
  public int subobject_num8 { get; set; }

  [FlattenerMapping("objects/subobjects/num9")]
  public int subobject_num9 { get; set; }
}

internal static class Program
{
  public static void Main()
  {
    var data = new {
        objects = Enumerable.Range(0, 1000).Select(x => new {
            num1 = x,
            num2 = x,
            num3 = x,
            num4 = x,
            num5 = x,
            num6 = x,
            num7 = x,
            num8 = x,
            num9 = x,
            subobjects = Enumerable.Range(0, 100).Select(x => new {
                num1 = x,
                num2 = x,
                num3 = x,
                num4 = x,
                num5 = x,
                num6 = x,
                num7 = x,
                num8 = x,
                num9 = x,
            })
        })
    };

    var js = JObject.FromObject(data);
    // var js = JToken.Parse(File.ReadAllText("/home/krystiand/prog/_zpn/RockTech/opta/fetcher/OptaFetcher/expectedGoals.json"));

    const int Iterations = 5;

    TimeSpan total = TimeSpan.Zero;
    for (int i = 0; i < Iterations; i++) {
      var sw = Stopwatch.StartNew();
      JsonFlattener.JsonFlattener.FlattenToObject<Model>(js, "objects/subobjects");
      total += sw.Elapsed;
    }

    Console.Write((total / Iterations).TotalMilliseconds);
  }
}