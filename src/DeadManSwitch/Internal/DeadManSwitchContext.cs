using System;
using System.Collections.Generic;
using System.Threading;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchContext : IDisposable
    {
        bool IsSuspended { get; }
        long LastNotifiedTicks { get; }
        CancellationToken CancellationToken { get; }
        IEnumerable<DeadManSwitchNotification> Notifications { get; }

        void Suspend();
        void Resume();
        void AddNotification(DeadManSwitchNotification deadManSwitchNotification);
        void Cancel();
    }

    internal sealed class DeadManSwitchContext : IDeadManSwitchContext
    {
        private readonly DeadManSwitchOptions _deadManSwitchOptions;
        private readonly DeadManSwitchNotification[] _notifications;
        private readonly object _notificationsSyncRoot;

        public long LastNotifiedTicks => Interlocked.Read(ref _lastNotifiedTicks);
        public bool IsSuspended => _isSuspended;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private CancellationTokenSource _cancellationTokenSource;
        private long _lastNotifiedTicks;
        private int _notificationsNextItemIndex;

        private volatile bool _isSuspended;

        public DeadManSwitchContext(DeadManSwitchOptions deadManSwitchOptions)
        {
            _deadManSwitchOptions = deadManSwitchOptions ?? throw new ArgumentNullException(nameof(deadManSwitchOptions));
            _notifications = new DeadManSwitchNotification[_deadManSwitchOptions.NumberOfNotificationsToKeep];
            _lastNotifiedTicks = DateTime.UtcNow.Ticks;
            _notificationsNextItemIndex = 0;
            _notificationsSyncRoot = new object();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Suspend()
        {
            _isSuspended = true;
        }

        public void Resume()
        {
            Interlocked.Exchange(ref _lastNotifiedTicks, DateTime.UtcNow.Ticks);
            _isSuspended = false;
        }

        public void AddNotification(DeadManSwitchNotification deadManSwitchNotification)
        {
            Interlocked.Exchange(ref _lastNotifiedTicks, DateTime.UtcNow.Ticks);

            lock (_notificationsSyncRoot)
            {
                _notifications[_notificationsNextItemIndex] = deadManSwitchNotification;
                _notificationsNextItemIndex = (_notificationsNextItemIndex + 1) % _notifications.Length;
            }
        }

        public void Cancel()
        {
            var cancellationTokenSource = Interlocked.Exchange(ref _cancellationTokenSource, new CancellationTokenSource());

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        public IEnumerable<DeadManSwitchNotification> Notifications
        {
            get
            {
                var numberOfNotificationsToKeep = _deadManSwitchOptions.NumberOfNotificationsToKeep;
                var notifications = new List<DeadManSwitchNotification>(numberOfNotificationsToKeep);

                lock (_notificationsSyncRoot)
                {
                    for (var i = _notificationsNextItemIndex; i < numberOfNotificationsToKeep + _notificationsNextItemIndex; i++)
                    {
                        var notification = _notifications[(i + _notificationsNextItemIndex) % numberOfNotificationsToKeep];
                        if (notification == null)
                            continue;

                        notifications.Add(notification);
                    }
                }

                return notifications;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}