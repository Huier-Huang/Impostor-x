namespace Impostor.Server.Net;

public enum ReactorProtocolVersion : byte
{
    /// <summary>
    ///     First public Reactor Protocol version.
    /// </summary>
    V2 = 1,

    /// <summary>
    ///     Version introducing vanilla server support, syncer concept and registries.
    /// </summary>
    V3 = 2,

    /// <summary>
    ///     Latest version.
    /// </summary>
    Latest = V3,
}
