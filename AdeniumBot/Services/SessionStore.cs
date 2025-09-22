using System.Collections.Concurrent;

namespace AdeniumBot.Services
{
    public class SessionStore
    {
        private readonly ConcurrentDictionary<string, Models.VoteSession> _sessions = new();
        private readonly ConcurrentDictionary<ulong, string> _channelToSession = new();

        public bool TryGetSession(string sessionId, out Models.VoteSession session)
            => _sessions.TryGetValue(sessionId, out session!);

        public bool TryGetSessionByChannel(ulong channelId, out string sessionId)
            => _channelToSession.TryGetValue(channelId, out sessionId!);

        public string CreateSession(ulong channelId, ulong ownerId, out Models.VoteSession session)
        {
            var sessionId = System.Guid.NewGuid().ToString("N");
            session = new Models.VoteSession(ownerId);
            _sessions[sessionId] = session;
            _channelToSession[channelId] = sessionId;
            return sessionId;
        }

        public bool TryBindMessage(string sessionId, ulong channelId, ulong messageId)
        {
            if (!_sessions.TryGetValue(sessionId, out var s)) return false;
            s.ChannelId = channelId;
            s.MessageId = messageId;
            return true;
        }

        public void RemoveSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var s) && s.ChannelId != 0)
                _channelToSession.TryRemove(s.ChannelId, out _);
        }

        public void RemoveByChannel(ulong channelId)
        {
            if (_channelToSession.TryRemove(channelId, out var sid))
                _sessions.TryRemove(sid, out _);
        }
    }
}