using System.Reflection;

namespace Wajub.Tests;

public class ListenTests
{
    [Theory]
    [InlineData(typeof(ListenService), "ConfigAsync")]
    [InlineData(typeof(ListenService), "AuthAsync")]
    [InlineData(typeof(WajubClient), "Listen")]
    public void ListenIsObsoleteAndPointsToTheCli(Type type, string member)
    {
        var obsolete = Assert.Single(type.GetMember(member)).GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.False(obsolete.IsError);
        Assert.Contains("wajub listen", obsolete.Message);
    }
}
