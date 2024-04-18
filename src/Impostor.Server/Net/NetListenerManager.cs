using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Impostor.Hazel;
using Impostor.Hazel.Dtls;
using Impostor.Hazel.Udp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Impostor.Server.Net;

public class NetListenerManager
{
    private readonly List<UdpConnectionListener> _allConnectionListeners = new();
    private readonly ILogger<NetListenerManager> _logger;

    private ObjectPool<MessageReader>? _readerPool;


    public NetListenerManager(
        ILogger<NetListenerManager> logger
    )
    {
        _logger = logger;
    }

    public void SetObjectPool(ObjectPool<MessageReader>? readerPool)
    {
        _readerPool = readerPool;
    }

    public UdpConnectionListener? CreateListener(string ip, ushort port,
        Func<NewConnectionEventArgs, ValueTask> onNewConnection, bool isDtl = false)
    {
        return CreateListener(IPAddress.Parse(ip), port, onNewConnection, isDtl);
    }

    public UdpConnectionListener? CreateListener(IPAddress ipAddress, ushort port,
        Func<NewConnectionEventArgs, ValueTask> onNewConnection, bool isDtl = false)
    {
        var endpoint = new IPEndPoint(ipAddress, port);
        return CreateListener(endpoint, onNewConnection, isDtl);
    }

    public UdpConnectionListener? CreateListener(IPEndPoint endPoint,
        Func<NewConnectionEventArgs, ValueTask> onNewConnection, bool isDtl = false)
    {
        if (_allConnectionListeners.Exists(n => Equals(n.EndPoint, endPoint)))
        {
            return null;
        }

        var listener = isDtl
            ? new DtlsConnectionListener(endPoint, _readerPool, GetIpMode(endPoint))
            : new UdpConnectionListener(endPoint, _readerPool, GetIpMode(endPoint))
            {
                NewConnection = onNewConnection,
            };
        _allConnectionListeners.Add(listener);
        _logger.LogInformation("AddListener isDtl:{0} Ip:{1} Port:{2}", isDtl, endPoint.Address, endPoint.Port);
        return listener;
    }

    public async ValueTask StartAllAsync()
    {
        foreach (var listener in _allConnectionListeners)
        {
            await listener.StartAsync();
        }

        _logger.LogInformation("StartAllAsync");
    }

    public async ValueTask StopAllAsync()
    {
        foreach (var listener in _allConnectionListeners)
        {
            await listener.DisposeAsync();
            _allConnectionListeners.Remove(listener);
        }

        _logger.LogInformation("StopAllAsync");
    }

    private IPMode GetIpMode(EndPoint endPoint)
    {
        return endPoint.AddressFamily switch
        {
            AddressFamily.InterNetwork => IPMode.IPv4,
            AddressFamily.InterNetworkV6 => IPMode.IPv6,
            _ => throw new InvalidOperationException(),
        };
    }
}
