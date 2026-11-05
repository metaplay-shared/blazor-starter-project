using Game.Server.Player;
using NUnit.Framework;
using System;

namespace Game.Server.Tests
{
    /// <summary>
    /// Example test fixture that shows how to test logic only the server runs, here the name a new player gets.
    /// </summary>
    // TODO: Example content, replace with your own. - test your own server-side logic here.
    [TestFixture]
    public class GuestNamesTests
    {
        [Test]
        public void GuestName_IsGuestAndANumber()
        {
            Random random = new Random(1234);
            for (int i = 0; i < 100; i++)
                Assert.That(GuestNames.Random(random), Does.Match(@"^Guest \d{1,5}$"));
        }
    }
}
