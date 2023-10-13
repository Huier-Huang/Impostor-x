namespace Impostor.Api.Config
{
    public static class DisconnectMessages
    {
        public const string Error = "There was an internal server error. " +
                                    "Check the server console for more information. " +
                                    "Please report the issue on the Impostor GitHub if it keeps happening.";

        public const string ClientOutdated = "Please update your game to play in this lobby. \nHost:{0} Client:{1}";

        public const string ClientTooNew = "Your game version is too new for this lobby. \nHost:{0} Client:{1} " +
                                           "If you want to join this lobby you need to downgrade your client.";

        public const string Destroyed = "The game you tried to join is being destroyed. " +
                                        "Please create a new game.";

        public const string UsernameLength = "Your username is too long, please make it shorter.";

        public const string UsernameIllegalCharacters = "Your username contains illegal characters, please remove them.";

        public const string VersionClientTooOld = "Please update your game to play on this server. \nClient:{0}  Server:{1}";

        public const string VersionServerTooOld = "Your client is too new, please update your Impostor server to play. \nClient:{0}  Server:{1}";

        public const string VersionUnsupported = "Your client version is unsupported, please update your Game and/or Impostor server. \nClient:{0}  Server:{1}";

        public const string UdpMatchmakingUnsupported = "UDP matchmaking is not supported anymore, migrate to a HTTP connection.";

        public const string CnError = "There was an internal server error. " +
                                    "Check the server console for more information. " +
                                    "Please report the issue on the Impostor GitHub if it keeps happening.";

        public const string CnClientOutdated = "请更新你的客户端版本\n房主:{0} 客户端:{1}";

        public const string CnClientTooNew = "请降低您的客户端版本\n房主:{0} 客户端:{1}";

        public const string CnDestroyed = "您要加入的房间已销毁, 请创建新房间";

        public const string CnUsernameLength = "您的用户名称过长";

        public const string CnUsernameIllegalCharacters = "您的用户名不合规";

        public const string CnVersionClientTooOld = "请更新您的客户端版本\n客户端版本:{0}  服务器支持版本{1}";

        public const string CnVersionServerTooOld = "服务器版本过低, 请更新您的服务器版本\n客户端版本:{0}  服务器支持版本{1}";

        public const string CnVersionUnsupported = "服务器不支持该版本,请联系腐竹\n客户端版本:{0}  服务器支持版本{1}";
      
    }
}
