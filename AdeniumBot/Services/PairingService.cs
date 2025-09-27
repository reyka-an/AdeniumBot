using AdeniumBot.Data;
using AdeniumBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AdeniumBot.Services
{
    public class PairingService
    {
        private readonly BotDbContextFactory _dbFactory = new();

        public record Result(IReadOnlyList<(ulong A, ulong B)> Pairs, ulong? Leftover);

        public async Task<Result> MakePairsAsync(IReadOnlyList<ulong> participantDiscordIds, CancellationToken ct = default)
        {
            var rng = new Random();
            var ids = participantDiscordIds.Distinct().OrderBy(_ => rng.Next()).ToArray();
            if (ids.Length == 0) return new Result(Array.Empty<(ulong, ulong)>(), null);

            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
            
            var profiles = await db.PlayerProfiles
                .Where(p => ids.Contains((ulong)p.DiscordUserId))
                .ToListAsync(ct);

            var discordToProfile = profiles.ToDictionary(p => (ulong)p.DiscordUserId, p => p.Id);
            ids = ids.Where(d => discordToProfile.ContainsKey(d)).ToArray();
            if (ids.Length < 2) return new Result(Array.Empty<(ulong, ulong)>(), ids.FirstOrDefault());

            var profIds = ids.Select(d => discordToProfile[d]).ToArray();
            var profSet = profIds.ToHashSet();
            
            var favLinks = await db.FavoriteLinks
                .Where(l => profSet.Contains(l.OwnerId) && profSet.Contains(l.TargetId))
                .ToListAsync(ct);
            var blLinks = await db.BlacklistLinks
                .Where(l => profSet.Contains(l.OwnerId) && profSet.Contains(l.TargetId))
                .ToListAsync(ct);

            var fav = new HashSet<(long, long)>();
            foreach (var l in favLinks) fav.Add((l.OwnerId, l.TargetId));

            var banned = new HashSet<(long, long)>();
            foreach (var l in blLinks) banned.Add((l.OwnerId, l.TargetId));
            
            var n = ids.Length;
            var idxToDiscord = ids;
            var idxToProfile = ids.Select(d => discordToProfile[d]).ToArray();
            
            const double Base = 1.0;
            const double OneSided = 0.02;

            var w = new double[n, n];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                w[i, j] = double.NegativeInfinity;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var a = idxToProfile[i];
                    var b = idxToProfile[j];
                    
                    if (banned.Contains((a, b)) || banned.Contains((b, a))) continue;

                    double score = Base;
                    if (fav.Contains((a, b))) score += OneSided;
                    if (fav.Contains((b, a))) score += OneSided;
                    
                    score += rng.NextDouble() * 1e-6;

                    w[i, j] = w[j, i] = score;
                }
            }
            
            (double best, List<(int i, int j)> pairs) SolveEven(IReadOnlyList<int> subset)
            {
                var m = subset.Count;
                if (m == 0) return (0.0, new List<(int, int)>());
                if (m % 2 == 1) throw new InvalidOperationException("Subset size must be even.");

                var idx = subset.ToArray();
                var size = 1 << m;
                var dp = new double[size];
                var prev = new int[size];
                for (int s = 1; s < size; s++) { dp[s] = double.NegativeInfinity; prev[s] = -1; }
                dp[0] = 0;

                for (int mask = 0; mask < size; mask++)
                {
                    if (dp[mask] == double.NegativeInfinity) continue;

                    int first = -1;
                    for (int t = 0; t < m; t++)
                        if ((mask & (1 << t)) == 0) { first = t; break; }
                    if (first == -1) continue;

                    for (int t = first + 1; t < m; t++)
                    {
                        if ((mask & (1 << t)) != 0) continue;
                        var i = idx[first];
                        var j = idx[t];
                        var ww = w[i, j];
                        if (double.IsNegativeInfinity(ww)) continue;

                        int nmask = mask | (1 << first) | (1 << t);
                        var val = dp[mask] + ww;
                        if (val > dp[nmask])
                        {
                            dp[nmask] = val;
                            prev[nmask] = (first << 8) | t;
                        }
                    }
                }

                var full = size - 1;
                var bestScore = dp[full];
                var res = new List<(int i, int j)>();
                if (bestScore == double.NegativeInfinity) return (bestScore, res);

                int cur = full;
                while (cur != 0)
                {
                    int code = prev[cur];
                    int aLoc = (code >> 8) & 0xFF;
                    int bLoc = code & 0xFF;
                    res.Add((idx[aLoc], idx[bLoc]));
                    cur ^= (1 << aLoc);
                    cur ^= (1 << bLoc);
                }
                return (bestScore, res);
            }
            
            List<(int i, int j)> bestPairs;
            ulong? leftoverDiscord = null;

            if (n % 2 == 0)
            {
                var all = Enumerable.Range(0, n).ToArray();
                var (_, pairsIdx) = SolveEven(all);
                bestPairs = pairsIdx;
            }
            else
            {
                double bestScore = double.NegativeInfinity;
                bestPairs = new List<(int, int)>();
                int bestLeft = -1;

                for (int skip = 0; skip < n; skip++)
                {
                    var subset = Enumerable.Range(0, n).Where(k => k != skip).ToArray();
                    var (score, pairsIdx) = SolveEven(subset);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPairs = pairsIdx;
                        bestLeft = skip;
                    }
                }

                if (bestLeft >= 0)
                    leftoverDiscord = idxToDiscord[bestLeft];
            }

            var pairsOut = bestPairs
                .Select(p => (A: idxToDiscord[p.i], B: idxToDiscord[p.j]))
                .ToList();

            return new Result(pairsOut, leftoverDiscord);
        }
    }
}
