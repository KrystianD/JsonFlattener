using JsonFlattener;
using Newtonsoft.Json.Linq;

var json = @"
{
  ""author"": {
    ""name"": ""Vannie""
  },
  ""movies"": [
    { ""title"": ""Deadly Advice"", ""length"": 90 },
    { ""title"": ""Biloxi Blues"", ""length"": 80 },
    { ""title"": ""Dylan Moran: Monster"", ""length"": 70 }
  ]
}
";

{
  var dictionary = JsonFlattener.JsonFlattener.FlattenToDict(JToken.Parse(json));
  foreach (var (key, value) in dictionary)
    Console.WriteLine($"{key} = {value}");
}

{
  foreach (var dictionary in JsonFlattener.JsonFlattener.FlattenToDict(JToken.Parse(json), "movies")) {
    foreach (var (key, value) in dictionary)
      Console.WriteLine($"{key} = {value}");
    Console.WriteLine("---");
  }
}

{
  foreach (var model in JsonFlattener.JsonFlattener.FlattenToObject<Model>(JToken.Parse(json), "movies")) {
    Console.WriteLine($"Author = {model.Author}");
    Console.WriteLine($"Title = {model.Title}");
    Console.WriteLine("---");
  }
}

class Model
{
  [FlattenerMapping("author/name")]
  public readonly string Author = null!;

  [FlattenerMapping("movies/title")]
  public readonly string Title = null!;
}