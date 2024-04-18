using Impostor.Api.Http;
using Impostor.Api.Innersloth;

namespace Impostor.Api.Net;

public class PlayerAuthInfo
{
    public GameVersion Version { get; set; }

    public string Puid { get; set; }

    public string FriendCode { get; set; }

    public string Name { get; set; }

    public uint LastId { get; set; }

    public Token Token { get; set; }

    public Language Language { get; set; }

    public Platforms Platforms { get; set; }
}
