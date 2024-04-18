using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Impostor.Api.Config;
using Impostor.Api.Events.Managers;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Impostor.Api.Net.Messages.C2S;
using Impostor.Hazel;
using Impostor.Hazel.Dtls;
using Impostor.Server.Events.Client;
using Impostor.Server.Net.Hazel;
using Impostor.Server.Net.Manager;
using Impostor.Server.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Net;

internal class Matchmaker
{
    private readonly ClientManager _clientManager;
    private readonly ILogger<HazelConnection> _connectionLogger;
    private readonly IEventManager _eventManager;
    private readonly ListenerConfig _listenerConfig;
    private readonly NetListenerManager _netListenerManager;
    private X509Certificate2? _certificate;

    private int _lastId;

    public Matchmaker(
        IEventManager eventManager,
        ClientManager clientManager,
        ObjectPool<MessageReader>? readerPool,
        ILogger<HazelConnection> connectionLogger,
        NetListenerManager netListenerManager,
        IOptions<ListenerConfig> listenerConfig)
    {
        _listenerConfig = listenerConfig.Value;
        _eventManager = eventManager;
        _clientManager = clientManager;
        _connectionLogger = connectionLogger;
        _netListenerManager = netListenerManager;

        _netListenerManager.SetObjectPool(readerPool);
    }

    public async ValueTask StartAsync(IPAddress address, ushort port)
    {
        GetCertificate();

        _netListenerManager.CreateListener(address, port, OnNewConnectionAsync);

        if (_listenerConfig.EnabledAuthListener)
        {
            var authPort = (ushort)(port + 2);
            var listener = _listenerConfig.EnabledUDPAuthListener
                ? _netListenerManager.CreateListener(address, authPort, OnAuthNewConnectionAsync)
                : _netListenerManager.CreateListener(address, authPort, OnAuthNewConnectionAsync, true);

            if (!_listenerConfig.EnabledUDPAuthListener)
            {
                ((DtlsConnectionListener)listener!).SetCertificate(_certificate);
            }
        }

        if (_listenerConfig.EnabledDtlListener)
        {
            var dtlPort = (ushort)(port + 3);
            var listener =
                (DtlsConnectionListener)_netListenerManager.CreateListener(address, dtlPort, OnNewConnectionAsync, true)!;
            listener.SetCertificate(_certificate);
        }

        await _netListenerManager.StartAllAsync();
    }

    public async ValueTask StopAsync()
    {
        await _netListenerManager.StopAllAsync();
    }

    private async ValueTask OnAuthNewConnectionAsync(NewConnectionEventArgs e)
    {
        var reader = e.HandshakeData;

        var version = reader.ReadGameVersion();
        var platformType = (Platforms)reader.ReadByte();
        var matchmakerToken = reader.ReadString();
        var friendCode = reader.ReadString();
        _connectionLogger.LogInformation(
            $"version:{version}, platform{platformType} token{matchmakerToken} friendCode{friendCode}");
        var writer = MessageWriter.Get(MessageType.Reliable);
        writer.StartMessage(1);
        writer.Write(_lastId);
        writer.EndMessage();
        await e.Connection.SendAsync(writer);
        _lastId++;
    }

    private async ValueTask OnNewConnectionAsync(NewConnectionEventArgs e)
    {
        // Handshake.
        HandshakeC2S.Deserialize(e.HandshakeData, out var clientVersion, out var name, out var language,
            out var chatMode, out var platformSpecificData);

        var connection = new HazelConnection(e.Connection, _connectionLogger);

        await _eventManager.CallAsync(new ClientConnectionEvent(connection, e.HandshakeData));

        // Register client
        await _clientManager.RegisterConnectionAsync(connection, name, clientVersion, language, chatMode,
            platformSpecificData);
    }

    private void GetCertificate()
    {
        if (_certificate != null)
        {
            return;
        }

        var privateKey = CertificateUtils.DecodeRSAKeyFromPEM(_listenerConfig.PrivateKeyString);
        _certificate = new X509Certificate2(CertificateUtils.DecodePEM(_listenerConfig.CertificateString))
            .CopyWithPrivateKey(privateKey);
    }
}
