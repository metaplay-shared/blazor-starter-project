using System;
using static System.FormattableString;

namespace Game.Server.Player
{
    /// <summary>
    /// The name a new player gets before choosing one, such as "Guest 12345".
    /// </summary>
    public static class GuestNames
    {
        public static string Random(Random random)
        {
            return Invariant($"Guest {random.Next(100_000)}");
        }
    }
}
