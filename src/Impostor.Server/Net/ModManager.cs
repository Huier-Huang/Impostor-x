using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Impostor.Api.Games;
using Impostor.Api.Net;
using Impostor.Api.Utils;
using Impostor.Server.Net.State;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net;

public class ModManager
{
    public ModManager(ILogger<ModManager> logger, IMessageWriterProvider messageWriterProvider, IServerEnvironment serverEnvironment)
    {
        _logger = logger;
        _messageWriterProvider = messageWriterProvider;
        _serverEnvironment = serverEnvironment;
    }

    public List<Mod> AllMods { get; internal set; } = new();

    internal Dictionary<IGame, List<Mod>> ModGame { get;  set; } = new();

    public Dictionary<IHazelConnection, Mod[]> ModsMap { get; private set; } = new();

    private readonly ILogger<ModManager> _logger;
    private readonly IMessageWriterProvider _messageWriterProvider;
    private readonly IServerEnvironment _serverEnvironment;

    private const ulong Magic = 0x72656163746f72;

    private static int Size => sizeof(ulong);

    internal ValueTask OnClientConnection(IHazelConnection connection, IMessageReader messageReader)
    {
        if (!TryDeserialize(messageReader, out var version))
        {
            return default;
        }

        var mods = Array.Empty<Mod>();

        if (version >= ReactorProtocolVersion.V3)
        {
            Deserialize(messageReader, out mods);
        }

        if (ModsMap.ContainsKey(connection))
        {
            ModsMap.Remove(connection);
        }

        ModsMap.Add(connection, mods);

        foreach (var mod in mods)
        {
            if (AllMods.Contains(mod))
            {
                continue;
            }

            AllMods.Add(mod);
        }

        return default;
    }

    internal ValueTask OnClientConnected(IHazelConnection connection, ClientBase clientBase)
    {
        if (ModsMap.All(n => n.Key != connection))
        {
            return default;
        }

        using var writer = _messageWriterProvider.Get(MessageType.Reliable);
        TryGetModAndCount(connection, out var mods, out var count);
        Serialize(writer, "Impostor", _serverEnvironment.Version, count);
        clientBase.Player!.IsMod = true;
        connection.SendAsync(writer);

        return default;
    }

    private void TryGetModAndCount(IHazelConnection connection, out Mod[]? mods, out int count)
    {
        mods = null;
        count = 0;
        if (!ModsMap.TryGetValue(connection, out var modsArray))
        {
            return;
        }

        mods = modsArray;
        count = mods.Length;
    }

    internal ValueTask OnGamePlayerJoining(IGame game, IClientPlayer clientPlayer, out GameJoinResult? joinResult)
    {
        var host = game.Host;
        joinResult = null;

        if (host == null)
        {
            return default;
        }

        TryGetModAndCount(host.Client.Connection!, out var hostMods, out var hostCount);
        TryGetModAndCount(clientPlayer.Client.Connection!, out var clientMods, out var clientCount);

        if (!Mod.Validate(clientMods ?? Array.Empty<Mod>(), hostMods ?? Array.Empty<Mod>(), out var reason))
        {
            joinResult = GameJoinResult.CreateCustomError(reason);
            return default;
        }

        if (!ModGame.ContainsKey(game))
        {
            ModGame.Add(game, new());
        }

        var modList = ModGame[game];
        foreach (var mod in clientMods!)
        {
            if (modList.Contains(mod))
            {
                continue;
            }

            modList.Add(mod);
        }

        return default;
    }

    internal ValueTask OnPlayerJoined(IGame game, ClientPlayer clientPlayer)
    {
        if (ModsMap.ContainsKey(clientPlayer.Client.Connection!))
        {
            clientPlayer.PlayerMod = ModsMap[clientPlayer.Client.Connection!].ToList();
        }

        return default;
    }

    private bool TryDeserialize(IMessageReader reader, [NotNullWhen(true)] out ReactorProtocolVersion? version)
    {
        if (reader.Length >= reader.Position + Size)
        {
            var value = reader.ReadUInt64();
            var magic = value >> 8;

            if (magic == Magic)
            {
                version = (ReactorProtocolVersion)(value & 0xFF);
                return true;
            }
        }

        version = null;
        return false;
    }

    private void Deserialize(IMessageReader reader, out Mod[] mods)
    {
        var modCount = reader.ReadPackedInt32();
        mods = new Mod[modCount];

        for (var i = 0; i < modCount; i++)
        {
            var id = reader.ReadString();
            var version = reader.ReadString();
            var flags = (ModFlags)reader.ReadUInt16();
            var name = (flags & ModFlags.RequireOnAllClients) != 0 ? reader.ReadString() : null;

            mods[i] = new Mod(id, version, flags, name);
        }
    }

    public static void Serialize(IMessageWriter writer, string serverName, string serverVersion, int pluginCount)
    {
        writer.StartMessage(byte.MaxValue);
        writer.Write((byte)ReactorMessageFlags.Handshake);
        writer.Write(serverName);
        writer.Write(serverVersion);
        writer.WritePacked(pluginCount);
        writer.EndMessage();
    }
}
