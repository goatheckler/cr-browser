using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrBrowser.Tests.Contract;

public class RegistryTypeContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void RegistryType_Enum_Should_Have_All_Required_Values()
    {
        var enumType = typeof(CrBrowser.Api.RegistryType);
        
        Assert.True(enumType.IsEnum, "RegistryType should be an enum");
        
        var values = Enum.GetNames(enumType);
        Assert.Contains("Ghcr", values);
        Assert.Contains("DockerHub", values);
        Assert.Contains("Quay", values);
        Assert.Contains("Gcr", values);
        Assert.Equal(4, values.Length);
    }

    [Theory]
    [InlineData("Ghcr", "ghcr")]
    [InlineData("DockerHub", "dockerHub")]
    [InlineData("Quay", "quay")]
    [InlineData("Gcr", "gcr")]
    public void RegistryType_Should_Serialize_As_Lowercase_String(string enumValue, string expectedJson)
    {
        var enumType = typeof(CrBrowser.Api.RegistryType);
        var value = Enum.Parse(enumType, enumValue);
        
        var json = JsonSerializer.Serialize(value, JsonOptions);
        
        Assert.Equal($"\"{expectedJson}\"", json);
    }

    [Theory]
    [InlineData("\"ghcr\"", "Ghcr")]
    [InlineData("\"dockerHub\"", "DockerHub")]
    [InlineData("\"quay\"", "Quay")]
    [InlineData("\"gcr\"", "Gcr")]
    public void RegistryType_Should_Deserialize_From_Lowercase_String(string json, string expectedEnumValue)
    {
        var enumType = typeof(CrBrowser.Api.RegistryType);
        
        var value = JsonSerializer.Deserialize(json, enumType, JsonOptions);
        
        Assert.NotNull(value);
        Assert.Equal(expectedEnumValue, value.ToString());
    }
}
