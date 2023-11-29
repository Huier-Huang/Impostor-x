using Impostor.Api.Games;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Inner.Objects;

namespace Impostor.Api.Net.Messages.Rpcs
{
    public static class Rpc12MurderPlayer
    {
        public static void Serialize(IMessageWriter writer, IInnerPlayerControl target, MurderResultFlags resultFlags)
        {
            writer.Write(target);
            writer.Write((int)resultFlags);
        }

        public static void Deserialize(IMessageReader reader, IGame game, out IInnerPlayerControl? target, out MurderResultFlags resultFlags)
        {
            target = reader.ReadNetObject<IInnerPlayerControl>(game);
            resultFlags = (MurderResultFlags)reader.ReadInt32();
        }
    }
}
