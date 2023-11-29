using System;
using Impostor.Api.Utils;

namespace Impostor.Tools.ServerReplay
{
    /// <summary>
    /// 
    /// </summary>
    public class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
