using System.Collections.Generic;
using System.Linq;
using Impostor.Api.Http;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;

namespace Impostor.Server.Net;

public class PlayerAuthInfoManager
{
    private readonly List<PlayerAuthInfo> _playerAuthInfos = [];

    public bool TryGet(Token token, out PlayerAuthInfo? playerAuthInfo)
    {
        playerAuthInfo = _playerAuthInfos.FirstOrDefault(n => n.Token == token);
        return playerAuthInfo == null;
    }

    public bool TryGet(uint lastId, out PlayerAuthInfo? playerAuthInfo)
    {
        playerAuthInfo = _playerAuthInfos.FirstOrDefault(n => n.LastId == lastId);
        return playerAuthInfo == null;
    }

    public void Register(TokenRequest request, Token token)
    {
        var info = _playerAuthInfos.FirstOrDefault(n => n.Puid == request.ProductUserId);
        if (info != null)
        {
            info.Name = request.Username;
            info.Language = request.Language;
            info.Version = new GameVersion(request.ClientVersion);
            info.Token = token;
            return;
        }

        info = new PlayerAuthInfo
        {
            Puid = request.ProductUserId,
            Name = request.Username,
            Language = request.Language,
            Version = new GameVersion(request.ClientVersion),
            Token = token,
        };
        _playerAuthInfos.Add(info);
    }
}
