using NUnit.Framework;

namespace W3ChampionsPlayerService.Tests;

[TestFixture]
public class FriendTests : IntegrationTestBase
{
    [Test]
    public void TestMethod1()
    {
    int result = 1;
    Assert.That(result, Is.EqualTo(1), "Result should be 1");
    }
}
