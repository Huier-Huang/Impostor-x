using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Impostor.Api.Http;


/// <summary>
///     Token that is returned to the user with a "signature".
/// </summary>
public sealed class Token
{
    [JsonPropertyName("Content")]
    public required TokenPayload Content { get; init; }

    [JsonPropertyName("Hash")]
    public required string Hash { get; init; }

    public string Serialize()
    {
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(this));
    }

    public static Token? Deserialize(string tokenString)
    {
        return JsonSerializer.Deserialize<Token>(Convert.FromBase64String(tokenString));
    }
}
