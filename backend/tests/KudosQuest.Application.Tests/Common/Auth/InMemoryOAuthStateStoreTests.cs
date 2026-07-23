using FluentAssertions;
using KudosQuest.Application.Common.Auth;

namespace KudosQuest.Application.Tests.Common.Auth;

public sealed class InMemoryOAuthStateStoreTests
{
    [Fact]
    public void Create_ThenTryConsume_SucceedsOnce()
    {
        var sut = new InMemoryOAuthStateStore();

        var state = sut.Create();

        sut.TryConsume(state).Should().BeTrue();
        sut.TryConsume(state).Should().BeFalse();
    }

    [Fact]
    public void TryConsume_UnknownState_ReturnsFalse()
    {
        var sut = new InMemoryOAuthStateStore();

        sut.TryConsume("missing").Should().BeFalse();
    }
}
