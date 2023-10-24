using System.Collections.Generic;
using System.Threading.Tasks;
using Impostor.Api.Games;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Inner;
using Impostor.Server.Net.State;

namespace Impostor.Server.Net;

public class AntiManager
{
    private Dictionary<GameCode, ClientPlayer> ALLAUM = new Dictionary<GameCode, ClientPlayer>();

    internal bool Check(ClientPlayer sender, ClientPlayer? target, RpcCalls rpcCalls, out string? log, out KickReason reason)
    {
        log = null;
        reason = KickReason.None;
        if ((int)rpcCalls == 101 && !(ALLAUM.ContainsKey(sender.Game.Code) && ALLAUM.ContainsValue(sender)))
        {
           ALLAUM.Add(sender.Game.Code, sender);
           log = $"存在AUM{sender.Game.Code.ToString()} : {sender.Client.Name}";
           reason = KickReason.AUM;
        }

        return true;
    }

    internal async Task StartKickAsync(ClientPlayer player, KickReason reason)
    {
        if (reason == KickReason.AUM)
        {
            await player.Game.Host!.Character!.SendChatAsync($"{player.Client.Name} 疑似使用AUM");
            if (!player.IsMod)
            {
                await player.Client.DisconnectAsync(DisconnectReason.Custom, "疑似使用AUM");
            }
        }
    }

    internal enum KickReason
    {
        AUM,
        None,
    }
}
