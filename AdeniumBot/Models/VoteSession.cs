using System;
using System.Collections.Generic;
using System.Threading;

namespace YourBot.Models
{
    public class VoteSession
    {
        public ulong OwnerId { get; }
        public HashSet<ulong> Participants { get; } = new();
        public object SyncRoot { get; } = new();
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }

        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
        public CancellationTokenSource Cts { get; } = new();

        public VoteSession(ulong ownerId) => OwnerId = ownerId;
    }
}