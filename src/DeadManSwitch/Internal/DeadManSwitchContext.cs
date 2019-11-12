using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchContext : IDisposable
    {
        CancellationTokenSource CancellationTokenSource { get; set; }

        ValueTask EnqueueStatusAsync(DeadManSwitchStatus deadManSwitchStatus, CancellationToken cancellationToken);
        ValueTask<DeadManSwitchStatus> DequeueStatusAsync(CancellationToken cancellationToken);

        ValueTask AddNotificationAsync(DeadManSwitchNotification deadManSwitchNotification, CancellationToken cancellationToken);
        ValueTask<IEnumerable<DeadManSwitchNotification>> GetNotificationsAsync(CancellationToken cancellationToken);
    }
    
    internal sealed class DeadManSwitchContext : IDeadManSwitchContext
    {
        private readonly int _numberOfNotificationsToKeep;
        private readonly ChannelWriter<DeadManSwitchNotification> _notificationsWriter;
        private readonly ChannelReader<DeadManSwitchNotification> _notificationsReader;
        private readonly ChannelReader<DeadManSwitchStatus> _statusesReader;
        private readonly ChannelWriter<DeadManSwitchStatus> _statusesWriter;

        public DeadManSwitchContext(DeadManSwitchOptions deadManSwitchOptions)
        {
            if (deadManSwitchOptions == null) throw new ArgumentNullException(nameof(deadManSwitchOptions));
            
            var notifications = Channel.CreateBounded<DeadManSwitchNotification>(new BoundedChannelOptions(deadManSwitchOptions.NumberOfNotificationsToKeep)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest
            });
            var statuses = Channel.CreateUnbounded<DeadManSwitchStatus>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });
            _numberOfNotificationsToKeep = deadManSwitchOptions.NumberOfNotificationsToKeep;
            _notificationsReader = notifications.Reader;
            _notificationsWriter = notifications.Writer;
            _statusesReader = statuses.Reader;
            _statusesWriter = statuses.Writer;
            
            CancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

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
            var notifications = new List<DeadManSwitchNotification>(_numberOfNotificationsToKeep);
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