using System;
using System.Collections.Generic;
using System.Linq;

namespace flight.Services
{
    public class LoginAttemptTracker
    {
        private readonly Dictionary<string, (int Count, DateTime FirstAttempt)> _attempts = new();
        private readonly int _maxAttemptsPerIP;
        private readonly TimeSpan _trackingPeriod;
        private readonly object _lock = new();

        public LoginAttemptTracker(int maxAttemptsPerIP = 10, int trackingMinutes = 15)
        {
            _maxAttemptsPerIP = maxAttemptsPerIP;
            _trackingPeriod = TimeSpan.FromMinutes(trackingMinutes);
        }

        public bool IsIPBlocked(string ipAddress)
        {
            CleanupOldAttempts();

            lock (_lock)
            {
                if (_attempts.TryGetValue(ipAddress, out var attempts))
                {
                    return attempts.Count >= _maxAttemptsPerIP;
                }
                return false;
            }
        }

        public void RecordAttempt(string ipAddress)
        {
            lock (_lock)
            {
                if (_attempts.TryGetValue(ipAddress, out var attempts))
                {
                    _attempts[ipAddress] = (attempts.Count + 1, attempts.FirstAttempt);
                }
                else
                {
                    _attempts[ipAddress] = (1, DateTime.UtcNow);
                }
            }
        }

        public int GetAttemptCount(string ipAddress)
        {
            lock (_lock)
            {
                if (_attempts.TryGetValue(ipAddress, out var attempts))
                {
                    return attempts.Count;
                }
                return 0;
            }
        }

        public int GetDelaySeconds(string ipAddress)
        {
            lock (_lock)
            {
                if (_attempts.TryGetValue(ipAddress, out var attempts))
                {
                    // Exponential backoff: 2^(attempts-1) seconds (max 30 seconds)
                    return Math.Min((int)Math.Pow(2, attempts.Count - 1), 30);
                }
                return 0;
            }
        }

        private void CleanupOldAttempts()
        {
            var now = DateTime.UtcNow;
            lock (_lock)
            {
                var keysToRemove = _attempts
                    .Where(kv => now.Subtract(kv.Value.FirstAttempt) > _trackingPeriod)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _attempts.Remove(key);
                }
            }
        }
        public void ResetAttempts(string ipAddress)
        {
            lock (_lock)
            {
                if (_attempts.ContainsKey(ipAddress))
                {
                    _attempts.Remove(ipAddress);
                }
            }
        }
    }
}