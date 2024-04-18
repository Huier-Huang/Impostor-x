using System;
using Impostor.Api.Innersloth;

namespace Impostor.Api.Net.Messages.C2S;

public static class HandshakeC2S
{
    public static void Deserialize(
        IMessageReader reader,
        bool useDtl,
        out GameVersion clientVersion,
        out string name,
        out Language language,
        out QuickChatModes chatMode,
        out PlatformSpecificData? platformSpecificData,
        out string matchmakerToken, out uint lastId,
        out string friendCode
        )
    {
        clientVersion = reader.ReadGameVersion();
        name = reader.ReadString();
        if (useDtl)
        {
            matchmakerToken = reader.ReadString();
            lastId = 0;
        }
        else
        {
            matchmakerToken = string.Empty;
            lastId = reader.ReadUInt32();
        }


        language = (Language)reader.ReadUInt32();
        chatMode = (QuickChatModes)reader.ReadByte();


        using var platformReader = reader.ReadMessage();
        platformSpecificData = new PlatformSpecificData(platformReader);


        friendCode = reader.ReadString();
        if (!useDtl)
        {
            reader.ReadUInt32();
        }
    }
}
