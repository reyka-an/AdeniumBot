namespace AdeniumBot.Services
{
    public class PairingService
    {
        public record Result(IReadOnlyList<(ulong A, ulong B)> Pairs, ulong? Leftover);

        /// <summary>
        /// Берём всех участников, перемешиваем и разбиваем по двое.
        /// Если количество нечётное — один остаётся без пары.
        /// </summary>
        public Task<Result> MakePairsAsync(IReadOnlyList<ulong> participantDiscordIds, CancellationToken ct = default)
        {
            var rng = new Random();
            var ids = participantDiscordIds
                .Distinct()
                .OrderBy(_ => rng.Next())
                .ToList();

            if (ids.Count == 0)
                return Task.FromResult(new Result(Array.Empty<(ulong, ulong)>(), null));
            
            var pairs = new List<(ulong A, ulong B)>();
            for (int i = 0; i + 1 < ids.Count; i += 2)
                pairs.Add((ids[i], ids[i + 1]));
            
            ulong? leftover = (ids.Count % 2 == 1) ? ids[^1] : null;

            return Task.FromResult(new Result(pairs, leftover));
        }
    }
}