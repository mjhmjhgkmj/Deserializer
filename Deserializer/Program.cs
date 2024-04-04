//using Newtonsoft.Json;
using System.Reflection;
using CSharpFunctionalExtensions;
namespace Deserializer;
public record Person(string Name, int Age, bool IsStudent)
{
    public Person() : this(string.Empty, 0, false){}
}

static public class Program
{
    public static void Main()
    {
        // Десериализация JSON в объект
        string jsonString = "{\"Name\":\"Alice\",\"Age\":25,\"IsStudent\":false}";
        //var deserializedPerson = JsonConvert.DeserializeObject<Person>(jsonString);
        var deserializedPerson = JsonDeserializer.Deserialize<Person>(jsonString);
        if (deserializedPerson != null)
        {
            Console.WriteLine($"Name: {deserializedPerson.Name}");
            Console.WriteLine($"Age: {deserializedPerson.Age}");
            Console.WriteLine($"Is Student: {deserializedPerson.IsStudent}");
        }
    } 
}

static public class JsonDeserializer
{
    public record JsonValue(object? Value);

    private static JsonValue ParseValue(string input) =>
        input switch
        {
            "null" => new JsonValue(null),
            "true" => new JsonValue(true),
            "false" => new JsonValue(false),
            var str when str.StartsWith('"') => new JsonValue(ParseString(str)),
            var str when str.StartsWith('[') => new JsonValue(ParseArray(str)),
            var str when str.StartsWith('{') => new JsonValue(ParseObject(str)),
            var str when double.TryParse(str, out var number) => new JsonValue(number),
            _ => throw new Exception("Invalid JSON format")
        };

    private static string ParseString(string input) =>
        input[1..^1] // для удаления кавычек из начала и конца строки
        .Replace("\\\"", "\"")
        .Replace("\\\\", "\\")
        .Replace("\\/", "/")
        .Replace("\\b", "\b")
        .Replace("\\f", "\f")
        .Replace("\\n", "\n")
        .Replace("\\r", "\r")
        .Replace("\\t", "\t")
        .Replace("\\u", "\\u");

    private static List<JsonValue> ParseArray(string input) =>
        input[1..^1].Split(',')
                    .Select(ParseValue)
                    .ToList();

    private static Dictionary<string, JsonValue> ParseObject(string input) =>
        input[1..^1].Split(',')
                    .Select(pair => pair.Split(':'))
                    .ToDictionary(parts => ParseString(parts[0].Trim()),
                                  parts => ParseValue(parts[1].Trim()));

    public static T? Deserialize<T>(string json) =>
        BuildObject<T>(ParseJson(json));


    private static Dictionary<string, object?> ParseJson(string json) =>
        ParseObject(json).ToDictionary(pair => pair.Key, pair => pair.Value.Value);

    private static T? BuildObject<T>(Dictionary<string, object?> parsedJson)
    {
        Type objectType = typeof(T);
        T? obj = (T?)Activator.CreateInstance(objectType);

        foreach (var keyValuePair in parsedJson)
        {
            if (keyValuePair.Value == null || keyValuePair.Key == null)
                throw new Exception("Invalid JSON format");

            object propertyValue = keyValuePair.Value;
            string propertyName = keyValuePair.Key;
            PropertyInfo? property = objectType.GetProperty(propertyName);

            if (property != null && property.CanWrite)
            {
                Type propertyType = property.PropertyType;

                if (propertyType.IsPrimitive || propertyType == typeof(string))
                {
                    // Приведение значения свойства к соответствующему типу
                    object? convertedValue = Convert.ChangeType(propertyValue, propertyType);
                    property.SetValue(obj, convertedValue);
                }
                else if (propertyType.IsClass)
                {
                    // Рекурсивная десериализация вложенного объекта
                    object? nestedObject = BuildObject<object>((Dictionary<string, object?>)propertyValue);
                    property.SetValue(obj, nestedObject);
                }
            }
        }

        return obj;
    }
}