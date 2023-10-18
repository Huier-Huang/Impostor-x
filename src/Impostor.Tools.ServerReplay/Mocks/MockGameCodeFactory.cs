using Impostor.Api.Games;

namespace Impostor.Tools.ServerReplay.Mocks
{
    /// <summary>
    /// 
    /// </summary>
    public class MockGameCodeFactory : IGameCodeFactory
    {
        /// <summary>
        /// 
        /// </summary>
        public GameCode Result { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GameCode Create()
        {
            return Result;
        }
    }
}
