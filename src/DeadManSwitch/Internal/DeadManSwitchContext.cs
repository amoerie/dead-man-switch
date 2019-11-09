﻿using System;
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
        public bool IsSuspended => _isSuspended;
        public long LastNotifiedTicks => Interlocked.Read(ref _lastNotifiedTicks);
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();


        private readonly object _notificationsSyncRoot = new object();
        private volatile DeadManSwitchNotification[] _notifications;

        private volatile int _notificationsStartIndex = 0;

        private volatile bool _isSuspended;
        private long _lastNotifiedTicks = DateTimeOffset.UtcNow.UtcTicks;


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
            _isSuspended = false;
        }

        public void AddNotification(DeadManSwitchNotification deadManSwitchNotification)
        {
            Interlocked.Exchange(ref _lastNotifiedTicks, DateTimeOffset.UtcNow.UtcTicks);

            lock (_notificationsSyncRoot)
            {
                _notifications[_notificationsStartIndex] = deadManSwitchNotification;
                _notificationsStartIndex = (_notificationsStartIndex + 1) % _notifications.Length;
            }
        }
        
        public IReadOnlyList<DeadManSwitchNotification> GetNotifications()
        {
            DeadManSwitchNotification[] oldNotifications;
            int oldStartIndex;

            lock (_notificationsSyncRoot)
            {
                oldNotifications = _notifications;
                oldStartIndex = _notificationsStartIndex;

                _notifications = new DeadManSwitchNotification[oldNotifications.Length];
                _notificationsStartIndex = 0;
            }


            var notificationsList = new List<DeadManSwitchNotification>(oldNotifications.Length);

            for (int x = 0; x < oldNotifications.Length; ++x)
            {
                var currentNotification = oldNotifications[(x + oldStartIndex) % oldNotifications.Length];

                if (currentNotification == null)
                    break;

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