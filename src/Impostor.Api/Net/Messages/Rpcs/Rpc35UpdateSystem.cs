using Impostor.Api.Games;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Inner.Objects;

namespace Impostor.Api.Net.Messages.Rpcs
{
    public static class Rpc35UpdateSystem
    {
        public static void Serialize(IMessageWriter writer, SystemTypes systemType, IInnerPlayerControl playerControl, ushort sequenceId, byte state, byte ventId)
        {
            writer.Write((byte)systemType);
            writer.Write(playerControl);
            writer.Write(sequenceId);
            writer.Write(state);
            writer.Write(ventId);
        }

        public static void Deserialize(IMessageReader reader, IGame game, out SystemTypes systemType, out IInnerPlayerControl? playerControl, out ushort sequenceId, out byte state, out byte ventId)
        {
            if (game.Host!.IsOldVersion)
            {
                DeserializeOld(reader, game, out systemType, out playerControl, out sequenceId, out state, out ventId);
                return;
            }

            systemType = (SystemTypes)reader.ReadByte();
            playerControl = reader.ReadNetObject<IInnerPlayerControl>(game);
            sequenceId = 0;
            state = 0;
            ventId = 0;
        }

        public static void DeserializeOld(IMessageReader reader, IGame game, out SystemTypes systemType, out IInnerPlayerControl? playerControl, out ushort sequenceId, out byte state, out byte ventId)
        {
            systemType = (SystemTypes)reader.ReadByte();
            playerControl = reader.ReadNetObject<IInnerPlayerControl>(game);
            sequenceId = reader.ReadUInt16();
            state = reader.ReadByte();
            ventId = reader.ReadByte();
        }
    }
}
