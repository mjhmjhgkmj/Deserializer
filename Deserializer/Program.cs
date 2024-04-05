//using Newtonsoft.Json;
using System.Reflection;
using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
namespace Deserializer;
public record Person(string Name, int Age, bool IsStudent)
{
    public Person() : this(string.Empty, 0, false){}
}

public record Address(string Street = "", string City = "", string Country = "")
{
    public Address() :this("","",""){ }
}

public record Friend(string Name, int Age);

public record Rec(
    string Name,
    int Age,
    Address Address,
    List<string> Interests,
    List<Friend> Friends
)
{
    public Rec() : this("", 0, new Address(), new List<string>(), new List<Friend>())
    {
    }
}

static public class Program
{
    public static void Main()
    {
        // Десериализация JSON в объект
        //string jsonString = "{\"Name\":\"Alice\",\"Age\":25,\"IsStudent\":false}";

        string jsonString = @"{
                ""name"": ""John"",
                ""age"": 30,
                ""address"": {
                    ""street"": ""123 Main St"",
                    ""city"": ""New York"",
                    ""country"": ""USA""
                },
                ""interests"": [
                    ""programming"",
                    ""reading"",
                    ""music""
                ],
                ""friends"": [
                    {
                        ""name"": ""Alice"",
                        ""age"": 28
                    },
                    {
                        ""name"": ""Bob"",
                        ""age"": 32
                    }
                ]
            }";

        var desRec = JsonDeserializer.Deserialize<Rec>(jsonString);
        if (desRec != null)
        {
            Console.WriteLine($"Name: {desRec.Name}");
            Console.WriteLine($"Age: {desRec.Age}");
            Console.WriteLine($"Is Student: {desRec.Address}");
            Console.WriteLine($"Is Student: {desRec.Interests}");
            Console.WriteLine($"Is Student: {desRec.Friends}");
        }
    }
}

static public class JsonDeserializer
{
    private static object? ParseValue(string input) =>
        input switch
        {
            "null" => null,
            "true" => true,
            "false" => false,
            var str when str.StartsWith('"') => ParseString(str),
            var str when str.StartsWith('[') => ParseArray(str),
            var str when str.StartsWith('{') => ParseObject(str),
            var str when double.TryParse(str, out var number) => number,
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

    private static List<object?> ParseArray(string input)
    {
        List<object?> result = new List<object?>();

        input = input.Trim();
        if (!input.StartsWith('[') || !input.EndsWith(']'))
            throw new Exception("Invalid JSON format for array.");

        input = input[1..^1].Trim(); // Удаление открывающей и закрывающей квадратных скобок

        int currentIndex = 0;
        int beginValueIndex = 0;
        int nestingLevel = 0;

        while (currentIndex < input.Length)
        {
            char currentChar = input[currentIndex];

            if (currentChar == ' ' || currentChar == '\r' || currentChar == '\n')
            {   currentIndex++;  continue; }
            else if (currentChar == '{' || currentChar == '[')
                nestingLevel++;
            else if (currentChar == '}' || currentChar == ']')
            {
                nestingLevel--;
                if (nestingLevel < 0)
                    throw new Exception("Invalid JSON format. Unexpected '}' or ']'.");
            }
            else if ((currentChar == ',' || currentIndex == input.Length - 1) && nestingLevel == 0)
            {
                string value = input.Substring(beginValueIndex, currentIndex - beginValueIndex + 1).Trim([',', ' ', '\r', '\n']);
                object? jsonValue = ParseValue(value);
                result.Add(jsonValue);
                beginValueIndex = currentIndex + 1;
            }

            currentIndex++;
        }

        if (nestingLevel > 0)
            throw new Exception("Invalid JSON format. Unterminated array.");

        return result;
    }

    private static Dictionary<string, object?> ParseObject(string input)
    {
        Dictionary<string, object?> result = [];

        input = input.Trim();
        if (!input.StartsWith('{') || !input.EndsWith('}'))
            throw new Exception("Invalid JSON format for object.");

        input = input[1..^1].Trim(); // Удаление открывающей и закрывающей фигурных скобок
        input += " ";// чтобы облегчить проверки на закрывающие скобки и не вылетить из массива

        int currentIndex = 0;
        int beginKeyIndex = 0;
        int nestingLevel = 0;
        int keyStartIndex = -1;
        string key = string.Empty;

        while (currentIndex < input.Length)
        {
            char currentChar = input[currentIndex];
            if (currentChar == ' ' || currentChar == '\r' || currentChar == '\n')
            {currentIndex++; continue;}

            if (currentChar == '{' || currentChar == '[')
                nestingLevel++;
            else if (currentChar == '}' || currentChar == ']')
            {
                nestingLevel--;
                if (nestingLevel < 0)
                    throw new Exception("Invalid JSON format. Unexpected '}'.");
            }
            else if (currentChar == ':' && nestingLevel == 0 && keyStartIndex == -1)
            {
                key = input.Substring(beginKeyIndex, currentIndex - beginKeyIndex).Trim([',', ' ', '\r', '\n']);
                key = ParseValue(key)?.ToString()??"";
                keyStartIndex = currentIndex + 1;
            }
            else if ((currentChar == ',' || currentIndex == input.Length-2) && nestingLevel == 0) 
            {
                if (keyStartIndex == -1)
                    throw new Exception("Invalid JSON format. Unexpected ','.");

                string value = input.Substring(keyStartIndex, currentIndex - keyStartIndex + 1).Trim([',', ' ', '\r', '\n']);
                object? jsonValue = ParseValue(value);
                result[key] = jsonValue;
                beginKeyIndex = currentIndex +1;
                keyStartIndex = -1;
            }
            currentIndex++;
        }

        if (nestingLevel > 0)
            throw new Exception("Invalid JSON format. Unterminated object.");

        return result;
    }

    public static T? Deserialize<T>(string json) =>
        BuildObject<T>(ParseJson(json));

    private static Dictionary<string, object> ParseJson(string json) =>
        ParseObject(json).ToDictionary(pair => pair.Key, pair => pair.Value??"");

    public static T? BuildObject<T>(Dictionary<string, object> parsedJson)
    {
        Type objectType = typeof(T);
        T? obj = (T?)Activator.CreateInstance(objectType);

        foreach (var keyValuePair in parsedJson)
        {
            if (keyValuePair.Value == null || keyValuePair.Key == null)
                throw new Exception("Invalid JSON format");

            object propertyValue = keyValuePair.Value;
            string propertyName = keyValuePair.Key;
            string propertyCasedName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);
            PropertyInfo? property = objectType.GetProperty(propertyCasedName);


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
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // Десериализация списка
                        Type listType = propertyType.GetGenericArguments()[0];
                        var listTypeIsString = listType == typeof(string);
                        var jsonArray = (List<object>)propertyValue;
                        dynamic list = listTypeIsString
                            ? (IList<string>)Activator.CreateInstance(typeof(List<string>))
                            : (IList<object>)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                        
                        foreach (var arrayItem in jsonArray)
                        {
                            object? listItem = listTypeIsString
                                ? arrayItem?.ToString()
                                : BuildObject<object>((Dictionary<string, object>)arrayItem);

                            list.Add(listItem.ToString());
                        }

                        property.SetValue(obj, list);
                    }
                    else
                    {
                        // Рекурсивная десериализация вложенного объекта
                        Type nestedObjectType = propertyType;
                        object? nestedObject = typeof(JsonDeserializer).GetMethod("BuildObject")
                            ?.MakeGenericMethod(nestedObjectType)
                            .Invoke(null, new object[] { (Dictionary<string, object>)propertyValue });

                        property.SetValue(obj, nestedObject);
                    }
                }
            }
        }

        return obj;
    }
}