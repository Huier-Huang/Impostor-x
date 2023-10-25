using System;
using System.Collections.Generic;
using System.Linq;
using Impostor.Api.Games;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Manager;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Manager;

using CompatibilityGroup = ICompatibilityManager.CompatibilityGroup;
using VersionCompareResult = ICompatibilityManager.VersionCompareResult;

internal class CompatibilityManager : ICompatibilityManager
{
    public static readonly Dictionary<GameVersion, string> SupportedVersionNames = new();

    private static readonly CompatibilityGroup[] DefaultSupportedVersions =
    {
        new[]
        {
            new GameVersion(2022, 11, 1), // 2022.12.8
        },

        new[]
        {
            new GameVersion(2022, 11, 9), // 2022.12.14
        },

        new[]
        {
            new GameVersion(2022, 12, 2), // 2023.2.28
        },

        new[]
        {
            new GameVersion(2023, 1, 11), // 2023.3.28s
            new GameVersion(2023, 3, 13), // 2023.3.28a
            new GameVersion(2023, 4, 21), // 2023.6.13
        },

        new[]
        {
            new GameVersion(2023, 5, 20), // 2023.7.11
            new GameVersion(2222, 0, 0), // 2023.7.11 for host-only mods
        },

        new[]
        {
            new GameVersion(2023, 10, 1), // 2023.10.24
        },
    };

    private readonly List<CompatibilityGroup> _compatibilityGroups = new();
    private readonly Dictionary<GameVersion, CompatibilityGroup> _supportMap = new();
    private readonly ILogger<CompatibilityManager> _logger;
    private GameVersion _lowestVersionSupported = new(int.MaxValue);
    private GameVersion _highestVersionSupported = new(0);

    static CompatibilityManager()
    {
        SupportedVersionNames.Add(DefaultSupportedVersions[0].GameVersions[0], "2022.12.8");
        SupportedVersionNames.Add(DefaultSupportedVersions[1].GameVersions[0], "2022.12.14");
        SupportedVersionNames.Add(DefaultSupportedVersions[2].GameVersions[0], "2023.2.28");
        SupportedVersionNames.Add(DefaultSupportedVersions[3].GameVersions[0], "2023.3.28s");
        SupportedVersionNames.Add(DefaultSupportedVersions[3].GameVersions[1], "2023.3.28a");
        SupportedVersionNames.Add(DefaultSupportedVersions[3].GameVersions[2], "2023.6.13");
        SupportedVersionNames.Add(DefaultSupportedVersions[4].GameVersions[0], "2023.7.11");
        SupportedVersionNames.Add(DefaultSupportedVersions[4].GameVersions[1], "2222.0.0(mod)");
        SupportedVersionNames.Add(DefaultSupportedVersions[5].GameVersions[0], "2023.10.24");
    }
    

    public CompatibilityManager(ILogger<CompatibilityManager> logger) : this(logger, DefaultSupportedVersions)
    {
    }

    internal CompatibilityManager(ILogger<CompatibilityManager> logger, IEnumerable<CompatibilityGroup> defaultSupportedVersions)
    {
        _logger = logger;

        foreach (var compatibilityGroup in defaultSupportedVersions)
        {
            AddCompatibilityGroup(compatibilityGroup);
        }
    }

    public IEnumerable<CompatibilityGroup> CompatibilityGroups => _compatibilityGroups;

    public bool TryGetVersionName(GameVersion version, out string name)
    {
        name = string.Empty;
        foreach (var versionName in from versionName in SupportedVersionNames where versionName.Key.Year == version.Year where versionName.Key.Month == version.Month where versionName.Key.Day == version.Day select versionName)
        {
            name = versionName.Value;
            return true;
        }

        return false;
    }

    private CompatibilityGroup? TryGetCompatibilityGroup(GameVersion clientVersion)
    {
        // Innersloth servers allow disabling server authority by incrementing the version revision by 25.
        // We should allow crossplay between client versions with this flag set and those without.
        clientVersion = clientVersion.Normalize();

        if (_supportMap.TryGetValue(clientVersion, out var compatibilityGroup))
        {
            return compatibilityGroup;
        }

        return null;
    }

    public VersionCompareResult CanConnectToServer(GameVersion clientVersion)
    {
        if (this.TryGetCompatibilityGroup(clientVersion) != null)
        {
            return VersionCompareResult.Compatible;
        }

        if (clientVersion < _lowestVersionSupported)
        {
            return VersionCompareResult.ClientTooOld;
        }

        if (clientVersion > _highestVersionSupported)
        {
            return VersionCompareResult.ServerTooOld;
        }

        return VersionCompareResult.Unknown;
    }

    public bool CanJoinGame(GameVersion hostVersion, GameVersion clientVersion, out GameJoinError joinError)
    {
        joinError = GameJoinError.None;

        if (hostVersion == clientVersion)
        {
            // Optimize a common case: a player on version X should always be able to join version X
            return true;
        }

        var hostCompatGroup = this.TryGetCompatibilityGroup(hostVersion);
        var playerCompatGroup = this.TryGetCompatibilityGroup(clientVersion);

        if (hostCompatGroup == null || playerCompatGroup == null)
        {
            joinError = GameJoinError.InvalidClient;
            return false;
        }

        if (hostCompatGroup == playerCompatGroup)
        {
            return true;
        }

        joinError = clientVersion < hostVersion
            ? GameJoinError.ClientOutdated
            : GameJoinError.ClientTooNew;
        return false;
    }

    void ICompatibilityManager.AddCompatibilityGroup(CompatibilityGroup compatibilityGroup)
    {
        _logger.LogWarning($"{nameof(AddCompatibilityGroup)} was called by a plugin, this can create unexpected issues. Please proceed carefully");

        AddCompatibilityGroup(compatibilityGroup);
    }

    void ICompatibilityManager.AddSupportedVersion(CompatibilityGroup compatibilityGroup, GameVersion gameVersion)
    {
        _logger.LogWarning($"{nameof(AddSupportedVersion)} was called by a plugin, this can create unexpected issues. Please proceed carefully");

        if (compatibilityGroup.GameVersions.Contains(gameVersion))
        {
            return;
        }

        AddSupportedVersion(compatibilityGroup, gameVersion, true);
    }

    private void AddCompatibilityGroup(CompatibilityGroup compatibilityGroup)
    {
        foreach (var gameVersion in compatibilityGroup.GameVersions)
        {
            if (_supportMap.ContainsKey(gameVersion))
            {
                throw new InvalidOperationException($"Can't add this compatibility group because one if its versions ({gameVersion}) is already added");
            }
        }

        _compatibilityGroups.Add(compatibilityGroup);

        foreach (var gameVersion in compatibilityGroup.GameVersions)
        {
            AddSupportedVersion(compatibilityGroup, gameVersion, false);
        }
    }

    private void AddSupportedVersion(CompatibilityGroup compatibilityGroup, GameVersion gameVersion, bool addToGroup)
    {
        if (!_compatibilityGroups.Contains(compatibilityGroup))
        {
            throw new InvalidOperationException("You have to add the compatibility group first");
        }

        if (_supportMap.ContainsKey(gameVersion))
        {
            throw new InvalidOperationException("Can't add this game version because it's already in another compatibility group");
        }

        if (addToGroup)
        {
            compatibilityGroup.Add(gameVersion);
        }

        _supportMap.Add(gameVersion, compatibilityGroup);

        // We special case the host-only 2023.7.11 here
        // Ideally it should never have existed so remove once 2023.7.11 is unsupported TODO
        var includeInSupportRange = gameVersion != new GameVersion(2222, 0, 0);

        if (includeInSupportRange)
        {
            if (gameVersion < _lowestVersionSupported)
            {
                _lowestVersionSupported = gameVersion;
            }

            if (gameVersion > _highestVersionSupported)
            {
                _highestVersionSupported = gameVersion;
            }
        }
    }

    public bool RemoveSupportedVersion(GameVersion removedVersion)
    {
        if (_supportMap.Remove(removedVersion, out var compatibilityGroup))
        {
            if (!compatibilityGroup.Remove(removedVersion))
            {
                throw new InvalidOperationException("Removed the version from the support map but it was missing from it's compatibility group");
            }

            if (!compatibilityGroup.GameVersions.Any())
            {
                _compatibilityGroups.Remove(compatibilityGroup);
            }

            return true;
        }

        return false;
    }
}
