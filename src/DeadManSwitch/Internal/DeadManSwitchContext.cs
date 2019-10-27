using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DeadManSwitch.Internal
{
    public interface IDeadManSwitchContext : IDisposable
    {
        CancellationTokenSource CancellationTokenSource { get; }
        
        ValueTask EnqueueStatusAsync(DeadManSwitchStatus deadManSwitchStatus, CancellationToken cancellationToken);
        ValueTask<DeadManSwitchStatus> DequeueStatusAsync(CancellationToken cancellationToken);

        ValueTask AddNotificationAsync(DeadManSwitchNotification deadManSwitchNotification, CancellationToken cancellationToken);
        ValueTask<IEnumerable<DeadManSwitchNotification>> GetNotificationsAsync(CancellationToken cancellationToken);
    }
    
    public sealed class DeadManSwitchContext : IDeadManSwitchContext
    {
        private readonly DeadManSwitchOptions _deadManSwitchOptions;
        private readonly ChannelWriter<DeadManSwitchNotification> _notificationsWriter;
        private readonly ChannelReader<DeadManSwitchNotification> _notificationsReader;
        private readonly ChannelReader<DeadManSwitchStatus> _statusesReader;
        private readonly ChannelWriter<DeadManSwitchStatus> _statusesWriter;

        public DeadManSwitchContext(DeadManSwitchOptions deadManSwitchOptions)
        {
            _deadManSwitchOptions = deadManSwitchOptions ?? throw new ArgumentNullException(nameof(deadManSwitchOptions));
            var notifications = Channel.CreateBounded<DeadManSwitchNotification>(new BoundedChannelOptions(deadManSwitchOptions.NumberOfNotificationsToKeep)
            {
                SingleWriter = false,
                SingleReader = true
            });
            var statuses = Channel.CreateUnbounded<DeadManSwitchStatus>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });
            _notificationsReader = notifications.Reader;
            _notificationsWriter = notifications.Writer;
            _statusesReader = statuses.Reader;
            _statusesWriter = statuses.Writer;
            
            CancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationTokenSource CancellationTokenSource { get; }

        public ValueTask EnqueueStatusAsync(DeadManSwitchStatus deadManSwitchStatus, CancellationToken cancellationToken)
        {
            return _statusesWriter.WriteAsync(deadManSwitchStatus, cancellationToken);
        }

        public ValueTask<DeadManSwitchStatus> DequeueStatusAsync(CancellationToken cancellationToken)
        {
            return _statusesReader.ReadAsync(cancellationToken);
        }

        public ValueTask AddNotificationAsync(DeadManSwitchNotification deadManSwitchNotification, CancellationToken cancellationToken)
        {
            return _notificationsWriter.WriteAsync(deadManSwitchNotification, cancellationToken);
        }
        
        public ValueTask<IEnumerable<DeadManSwitchNotification>> GetNotificationsAsync(CancellationToken cancellationToken)
        {
            var notifications = new List<DeadManSwitchNotification>(_deadManSwitchOptions.NumberOfNotificationsToKeep);
            while (_notificationsReader.TryRead(out var notification))
                notifications.Add(notification);
            return new ValueTask<IEnumerable<DeadManSwitchNotification>>(notifications);
        }

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }
}