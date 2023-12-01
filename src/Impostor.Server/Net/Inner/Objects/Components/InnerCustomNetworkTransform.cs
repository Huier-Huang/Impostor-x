using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Impostor.Api.Events.Managers;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Impostor.Api.Net.Custom;
using Impostor.Api.Net.Inner;
using Impostor.Api.Net.Messages.Rpcs;
using Impostor.Server.Events.Player;
using Impostor.Server.Net.Inner.Objects.ShipStatus;
using Impostor.Server.Net.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Impostor.Server.Net.Inner.Objects.Components
{
    internal partial class InnerCustomNetworkTransform : InnerNetObject
    {
        private static readonly Vector2 ColliderOffset = new Vector2(0f, -0.4f);

        private readonly ILogger<InnerCustomNetworkTransform> _logger;
        private readonly InnerPlayerControl _playerControl;
        private readonly IEventManager _eventManager;
        private readonly ObjectPool<PlayerMovementEvent> _pool;

        private ushort _lastSequenceId;
        private AirshipSpawnState _spawnState;

        public InnerCustomNetworkTransform(ICustomMessageManager<ICustomRpc> customMessageManager, Game game, ILogger<InnerCustomNetworkTransform> logger, InnerPlayerControl playerControl, IEventManager eventManager, ObjectPool<PlayerMovementEvent> pool) : base(customMessageManager, game)
        {
            _logger = logger;
            _playerControl = playerControl;
            _eventManager = eventManager;
            _pool = pool;

            SendQueue = new Queue<Vector2>();
            IncomingPosQueue = new Queue<Vector2>();
        }

        private enum AirshipSpawnState : byte
        {
            PreSpawn,
            SelectingSpawn,
            Spawned,
        }

        public Vector2 Position { get; private set; }

        public Vector2 LastPosSent { get; private set; }

        public Queue<Vector2> SendQueue { get; private set; }

        public Queue<Vector2> IncomingPosQueue { get; private set; }
        

        public override ValueTask<bool> SerializeAsync(IMessageWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_lastSequenceId);
                writer.Write(Position);
                return new ValueTask<bool>(true);
            }

            if (SendQueue.Count == 0)
            {
                return new ValueTask<bool>(false);
            }

            var num = (ushort)SendQueue.Count;
            _lastSequenceId++;
            writer.Write(_lastSequenceId);
            writer.WritePacked(num);
            foreach (var vec in SendQueue)
            {
                writer.Write(vec);
                LastPosSent = vec;
            }

            SendQueue.Clear();
            _lastSequenceId += (ushort)(num - 1);

            writer.Write(_lastSequenceId);
            writer.Write(Position);
            return new ValueTask<bool>(true);
        }

        public override async ValueTask DeserializeAsync(IClientPlayer sender, IClientPlayer? target, IMessageReader reader, bool initialState)
        {
            if (initialState)
            {
                IncomingPosQueue.Clear();
                _lastSequenceId = reader.ReadUInt16();
                await SetPositionAsync(sender, reader.ReadVector2());
                return;
            }

            var num = reader.ReadUInt16();
            var max = reader.ReadPackedInt32();

            var position = IncomingPosQueue.Count > 0 ? IncomingPosQueue.Last() : Position;

            for (var index = 0; index < max; index++)
            {
                var newSid = (ushort)(num + index);
                var varVector = reader.ReadVector2();
                if (!SidGreaterThan(newSid, _lastSequenceId))
                {
                    continue;
                }

                _lastSequenceId = newSid;
                IncomingPosQueue.Enqueue(varVector);
                position = varVector;
            }

            await SetPositionAsync(sender, position);
        }

        public override async ValueTask<bool> HandleRpcAsync(ClientPlayer sender, ClientPlayer? target, RpcCalls call, IMessageReader reader)
        {
            if (call == RpcCalls.SnapTo)
            {
                if (!await ValidateOwnership(call, sender))
                {
                    return false;
                }

                Rpc21SnapTo.Deserialize(reader, out var position, out var minSid);

                if (Game.GameNet.ShipStatus is InnerAirshipStatus airshipStatus)
                {
                    // As part of airship spawning, clients are sending snap to -25 40 to move themself out of view
                    if (_spawnState == AirshipSpawnState.PreSpawn && Approximately(position, airshipStatus.PreSpawnLocation))
                    {
                        _spawnState = AirshipSpawnState.SelectingSpawn;
                        return true;
                    }

                    // Once the spawn has been selected, the client sends a second snap to the select spawn location
                    if (_spawnState == AirshipSpawnState.SelectingSpawn && airshipStatus.SpawnLocations.Any(location => Approximately(position, location)))
                    {
                        _spawnState = AirshipSpawnState.Spawned;
                        return true;
                    }
                }

                if (!await ValidateCanVent(call, sender, _playerControl.PlayerInfo))
                {
                    return false;
                }

                if (Game.GameNet.ShipStatus == null)
                {
                    // Cannot perform vent position check on unknown ship statuses
                    if (await sender.Client.ReportCheatAsync(call, "Failed vent position check on unknown map"))
                    {
                        return false;
                    }
                }
                else
                {
                    var vents = Game.GameNet.ShipStatus!.Data.Vents.Values;

                    var vent = vents.SingleOrDefault(x => Approximately(x.Position, position + ColliderOffset));

                    if (vent == null)
                    {
                        if (await sender.Client.ReportCheatAsync(call, "Failed vent position check"))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        await _eventManager.CallAsync(new PlayerVentEvent(Game, sender, _playerControl, vent));
                    }
                }

                await SnapToAsync(sender, position, minSid);
                return true;
            }

            return await base.HandleRpcAsync(sender, target, call, reader);
        }

        internal async ValueTask SetPositionAsync(IClientPlayer sender, Vector2 position)
        {
            Position = position;
            IncomingPosQueue.Enqueue(position);

            var playerMovementEvent = _pool.Get();
            playerMovementEvent.Reset(Game, sender, _playerControl);
            await _eventManager.CallAsync(playerMovementEvent);
            _pool.Return(playerMovementEvent);
        }

        internal void OnPlayerSpawn()
        {
            _spawnState = AirshipSpawnState.PreSpawn;
        }

        private static bool SidGreaterThan(ushort newSid, ushort prevSid)
        {
            var num = (ushort)(prevSid + (uint)short.MaxValue);

            return prevSid < num
                ? newSid > prevSid && newSid <= num
                : newSid > prevSid || newSid <= num;
        }

        private static bool Approximately(Vector2 a, Vector2 b, float tolerance = 0.1f)
        {
            var abs = Vector2.Abs(a - b);
            return abs.X <= tolerance && abs.Y <= tolerance;
        }

        private ValueTask SnapToAsync(IClientPlayer sender, Vector2 position, ushort minSid)
        {
            if (!SidGreaterThan(minSid, _lastSequenceId))
            {
                return default;
            }

            _lastSequenceId = minSid;
            return SetPositionAsync(sender, position);
        }
    }
}
