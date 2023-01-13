using Newtonsoft.Json.Linq;

var json = @"{
""objects"": [
{
  ""args"": 1
},
{
  ""args"": 2
}
]
}
";

var flattened = JsonFlattener.JsonFlattener.Flatten(JToken.Parse(json), "");
foreach (var dictionary in flattened) {
  foreach (var (key, value) in dictionary) {
    Console.WriteLine($"{key} = {value}");
  }

  Console.WriteLine();
}