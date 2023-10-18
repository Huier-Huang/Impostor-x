using System.Net;
using System.Threading.Tasks;
using Impostor.Api.Net;
using Impostor.Hazel.Abstractions;

namespace Impostor.Tools.ServerReplay.Mocks
{
    /// <summary>
    /// 
    /// </summary>
    public class MockHazelConnection : IHazelConnection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        public MockHazelConnection(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
            IsConnected = true;
            Client = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint EndPoint { get; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsConnected { get; }
        /// <summary>
        /// 
        /// </summary>
        public IClient Client { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float AveragePing => 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public ValueTask SendAsync(IMessageWriter writer)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public ValueTask DisconnectAsync(string reason, IMessageWriter writer = null)
        {
            return ValueTask.CompletedTask;
        }
    }
}
