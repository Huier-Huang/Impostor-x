using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;

namespace Impostor.Server.Net
{
    public partial class GameCodeFactory : IGameCodeFactory
    {
        private readonly HashSet<string> _gameCodes = new();
        private readonly IGameManager _gameManager;

        public GameCodeFactory(IGameManager gameManager)
        {
            _gameManager = gameManager;
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            var codeNameFile = directory.GetFiles().FirstOrDefault(n => n.Name == "CodeName.txt");
            if (codeNameFile == null)
            {
                return;
            }

            var streamReader = new StreamReader(codeNameFile.FullName);
            while (streamReader.ReadLine() is { } code)
            {
                var regex = MyRegex();
                if (!regex.IsMatch(code))
                {
                    continue;
                }

                _gameCodes.Add(code);
            }
        }

        public GameCode Create()
        {
            var codes = _gameCodes.Where(FindCode).ToList();
            return codes.Count == 0 ? GameCode.Create() : new GameCode(codes.First());
        }

        private bool FindCode(string code)
        {
            var games = _gameManager.Games.Select(n => n.Code.Code);
            return !games.Contains(code);
        }

        [GeneratedRegex("[A-Z]{6}")]
        private static partial Regex MyRegex();
    }
}
