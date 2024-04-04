using Newtonsoft.Json;
using Xunit;

namespace json_test;

public class JsonSerializationTests
{
    [Fact]
    public void SerializeDeserializePerson()
    {
        // Arrange
        Person person = new("John", 30, true);
        string json = JsonConvert.SerializeObject(person);

        // Act
        var deserializedPerson = JsonConvert.DeserializeObject<Person>(json);

        // Assert
        Assert.NotNull(deserializedPerson);
        Assert.Equal("John", deserializedPerson.Name);
        Assert.Equal(30, deserializedPerson.Age);
        Assert.True(deserializedPerson.IsStudent);
    }
}
