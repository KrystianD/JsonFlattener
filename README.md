JsonFlattener
=====

Library for flattening JSON structures into key-value pairs with unwrapping support.

## Features
* Unwrapping against particular, nested field. Useful when inserting JSON into databases
in denormalized form,
* Flattening to POCO using attributes with path information.

# Usage

Example JSON object:

```json
{
  "author": {
    "name": "Vannie"
  },
  "movies": [
    { "title": "Deadly Advice", "length": 90 },
    { "title": "Biloxi Blues", "length": 80 },
    { "title": "Dylan Moran: Monster", "length": 70 }
  ]
}
```

## Basic

```csharp
var dictionary = JsonFlattener.JsonFlattener.FlattenToDict(JToken.Parse(json));
foreach (var (key, value) in dictionary)
  Console.WriteLine($"{key} = {value}");
```

result - dictionary of key-value pairs:

```
author/name = Vannie
movies[0]/title = Deadly Advice
movies[0]/length = 90
movies[1]/title = Biloxi Blues
movies[1]/length = 80
movies[2]/title = Dylan Moran: Monster
movies[2]/length = 70
```

## Advanced

The example below shows flattening and unwrapping JSON against `movies` array.

```csharp
foreach (var dictionary in JsonFlattener.JsonFlattener.FlattenToDict(JToken.Parse(json), "movies")) {
  foreach (var (key, value) in dictionary)
    Console.WriteLine($"{key} = {value}");
  Console.WriteLine("---");
}
```

result - array of 3 dictionaries with data from `movies` array item and surroundings:

```
author/name = Vannie
movies/title = Deadly Advice
movies/length = 90
---
author/name = Vannie
movies/title = Biloxi Blues
movies/length = 80
---
author/name = Vannie
movies/title = Dylan Moran: Monster
movies/length = 70
```

## Flatten to C# objects

The example below shows flattening and unwrapping JSON against `movies` array into C# model object.

```csharp
class Model
{
  [FlattenerMapping("author/name")]
  public string Author;

  [FlattenerMapping("movies/title")]
  public string Title;
}

foreach (var model in JsonFlattener.JsonFlattener.FlattenToObject<Model>(JToken.Parse(json), "movies")) {
  Console.WriteLine($"Author = {model.Author}");
  Console.WriteLine($"Title = {model.Title}");
  Console.WriteLine("---");
}
```

result - array of 3 objects with movie title and author name:

```
Author = Vannie
Title = Deadly Advice
---
Author = Vannie
Title = Biloxi Blues
---
Author = Vannie
Title = Dylan Moran: Monster
```