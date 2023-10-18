using System.Collections.Generic;
using System.Threading.Tasks;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Server.Events;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Impostor.Tests.Events
{
    /// <summary>
    /// 
    /// </summary>
    public class EventManagerTests
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly IEnumerable<object[]> TestModes = new[]
        {
            new object[] { TestMode.Service },
            new object[] { TestMode.Temporary },
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        [Theory]
        [MemberData(nameof(TestModes))]
        public async ValueTask CallEventAsync(TestMode mode)
        {
            var listener = new EventListener();
            var eventManager = CreatEventManager(mode, listener);

            await eventManager.CallAsync(new SetValueEvent(1));

            Assert.Equal(1, listener.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        [Theory]
        [MemberData(nameof(TestModes))]
        public async Task CallPriorityAsync(TestMode mode)
        {
            var listener = new PriorityEventListener();
            var eventManager = CreatEventManager(mode, listener);

            await eventManager.CallAsync(new SetValueEvent(1));

            Assert.Equal(new[]
            {
                EventPriority.Monitor,
                EventPriority.Highest,
                EventPriority.High,
                EventPriority.Normal,
                EventPriority.Low,
                EventPriority.Lowest,
            }, listener.Priorities);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        [Theory]
        [MemberData(nameof(TestModes))]
        public async ValueTask CancelEventAsync(TestMode mode)
        {
            var listener = new EventListener();
            var eventManager = CreatEventManager(
                mode,
                new CancelAtHighEventListener(),
                listener
            );

            await eventManager.CallAsync(new SetValueEvent(1));

            Assert.Equal(0, listener.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        [Theory]
        [MemberData(nameof(TestModes))]
        public async Task CancelPriorityAsync(TestMode mode)
        {
            var listener = new PriorityEventListener();
            var eventManager = CreatEventManager(
                mode,
                new CancelAtHighEventListener(),
                listener
            );

            await eventManager.CallAsync(new SetValueEvent(1));

            Assert.Equal(new[]
            {
                EventPriority.Monitor,
                EventPriority.Highest,
            }, listener.Priorities);
        }

        private static IEventManager CreatEventManager(TestMode mode, params IEventListener[] listeners)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IEventManager, EventManager>();

            if (mode == TestMode.Service)
            {
                foreach (var listener in listeners)
                {
                    services.AddSingleton(listener);
                }
            }

            var eventManager = services.BuildServiceProvider().GetRequiredService<IEventManager>();

            if (mode == TestMode.Temporary)
            {
                foreach (var listener in listeners)
                {
                    eventManager.RegisterListener(listener);
                }
            }

            return eventManager;
        }

        /// <summary>
        /// 
        /// </summary>
        public enum TestMode
        {
            /// <summary>
            /// 
            /// </summary>
            Service,
            /// <summary>
            /// 
            /// </summary>
            Temporary,
        }

        /// <summary>
        /// 
        /// </summary>
        public interface ISetValueEvent : IEventCancelable
        {
            /// <summary>
            /// 
            /// </summary>
            int Value { get; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class SetValueEvent : ISetValueEvent
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            public SetValueEvent(int value)
            {
                Value = value;
            }

            /// <summary>
            /// 
            /// </summary>
            public int Value { get; }

            /// <summary>
            /// 
            /// </summary>
            public bool IsCancelled { get; set; }
        }

        private class CancelAtHighEventListener : IEventListener
        {
            [EventListener(Priority = EventPriority.High)]
            public void OnSetCalled(ISetValueEvent e) => e.IsCancelled = true;
        }

        private class EventListener : IEventListener
        {
            public int Value { get; private set; }

            [EventListener]
            public void OnSetCalled(ISetValueEvent e) => Value = e.Value;
        }

        private class PriorityEventListener : IEventListener
        {
            public List<EventPriority> Priorities { get; } = new List<EventPriority>();

            [EventListener(EventPriority.Lowest)]
            public void OnLowest(ISetValueEvent e) => Priorities.Add(EventPriority.Lowest);

            [EventListener(EventPriority.Low)]
            public void OnLow(ISetValueEvent e) => Priorities.Add(EventPriority.Low);

            [EventListener]
            public void OnNormal(ISetValueEvent e) => Priorities.Add(EventPriority.Normal);

            [EventListener(EventPriority.High)]
            public void OnHigh(ISetValueEvent e) => Priorities.Add(EventPriority.High);

            [EventListener(EventPriority.Highest)]
            public void OnHighest(ISetValueEvent e) => Priorities.Add(EventPriority.Highest);

            [EventListener(EventPriority.Monitor)]
            public void OnMonitor(ISetValueEvent e) => Priorities.Add(EventPriority.Monitor);
        }
    }
}
