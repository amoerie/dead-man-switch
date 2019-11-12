using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchContext : IDisposable
    {
        bool IsSuspended { get; }
        long LastNotifiedTicks { get; }

        CancellationTokenSource CancellationTokenSource { get; set; }

        void Suspend();
        void Resume();

        void AddNotification(DeadManSwitchNotification deadManSwitchNotification);
        IReadOnlyList<DeadManSwitchNotification> GetNotifications();
    }
    
    internal sealed class DeadManSwitchContext : IDeadManSwitchContext
    {
        public long LastNotifiedTicks => Interlocked.Read(ref _lastNotifiedTicks);
        public bool IsSuspended => _isSuspended;

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();


        private long _lastNotifiedTicks = DateTime.UtcNow.Ticks;

        private DeadManSwitchNotification[] _notifications;
        private int _notificationsNextItemIndex = 0;
        private readonly object _notificationsSyncRoot = new object();

        private volatile bool _isSuspended;


        public DeadManSwitchContext(DeadManSwitchOptions deadManSwitchOptions)
        {
            if (deadManSwitchOptions == null) throw new ArgumentNullException(nameof(deadManSwitchOptions));

            _notifications = new DeadManSwitchNotification[deadManSwitchOptions.NumberOfNotificationsToKeep];
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
        
        public IReadOnlyList<DeadManSwitchNotification> GetNotifications()
        {
            DeadManSwitchNotification[] oldNotifications;
            int oldNextItemIndex;

            lock (_notificationsSyncRoot)
            {
                oldNotifications = _notifications;
                oldNextItemIndex = _notificationsNextItemIndex;

                _notifications = new DeadManSwitchNotification[oldNotifications.Length];
                _notificationsNextItemIndex = 0;
            }


            var notificationsList = new List<DeadManSwitchNotification>(oldNotifications.Length);

            for (int x = 0; x < oldNotifications.Length; ++x)
            {
                var currentNotification = oldNotifications[(x + oldNextItemIndex) % oldNotifications.Length];

                // Expect to only occur if less than the maximum number of items have been queued.
                if (currentNotification == null)
                    continue;

                notificationsList.Add(currentNotification);
            }

            return notificationsList;
        }

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }
}