using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Adenium.Utils
{
    public static class Pairing
    {
        public static string BuildPairsText(List<ulong> ids)
        {
            // Fisher–Yates
            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }

            var sb = new StringBuilder();
            int inGroup = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                sb.Append("<@").Append(ids[i]).Append('>').AppendLine();
                inGroup++;
                if (inGroup == 2)
                {
                    sb.AppendLine();
                    inGroup = 0;
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}