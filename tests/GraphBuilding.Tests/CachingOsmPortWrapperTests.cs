namespace GraphBuilding.Tests;

using Ports;

public class CachingOsmPortWrapperTests
{
    [Fact]
    public async Task ProxiesForSingleId()
    {
        var point = new OsmPoint(123, new Dictionary<string, string>(), new(new(1, 2)));
        var osmPort = new Mock<IOsmPort>();
        osmPort.Setup(x => x.GetPointByOsmId(123)).ReturnsAsync(point).Verifiable();
        var wrapper = new CachingOsmPortWrapper(osmPort.Object);
        (await wrapper.GetPointByOsmId(123)).Should().Be(point);
        osmPort.Verify();

        // second call should be cached
        osmPort.Invocations.Clear();
        (await wrapper.GetPointByOsmId(123)).Should().Be(point);
        osmPort.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProxiesForNull()
    {
        var osmPort = new Mock<IOsmPort>();
        osmPort.Setup(x => x.GetPointByOsmId(123)).ReturnsAsync((OsmPoint?)null).Verifiable();
        var wrapper = new CachingOsmPortWrapper(osmPort.Object);
        (await wrapper.GetPointByOsmId(123)).Should().BeNull();
        osmPort.Verify();

        // second call should still not be cached
        osmPort.Invocations.Clear();
        (await wrapper.GetPointByOsmId(123)).Should().BeNull();
        osmPort.Verify();
    }

    [Fact]
    public async Task ProxiesMultiple()
    {
        var points = new OsmPoint[]
        {
            new(123, new Dictionary<string, string>(), new(new(1, 2))),
            new(456, new Dictionary<string, string>(), new(new(1, 2))),
            new(789, new Dictionary<string, string>(), new(new(1, 2))),
            new(101112, new Dictionary<string, string>(), new(new(1, 2)))
        };
        var osmPort = new Mock<IOsmPort>();
        osmPort
            .Setup(
                x =>
                    x.GetPointsByOsmIds(
                        It.Is<IEnumerable<long>>(e => e.SequenceEqual(new[] { 123L, 456, 789 }))
                    )
            )
            .ReturnsAsync(points.Take(3))
            .Verifiable();
        var wrapper = new CachingOsmPortWrapper(osmPort.Object);
        (await wrapper.GetPointsByOsmIds(new[] { 123L, 456, 789 }))
            .Should()
            .BeEquivalentTo(points.Take(3));
        osmPort.Verify();

        // second call should be cached
        osmPort
            .Setup(
                x =>
                    x.GetPointsByOsmIds(
                        It.Is<IEnumerable<long>>(e => e.SequenceEqual(new[] { 101112L }))
                    )
            )
            .ReturnsAsync(points.Skip(3))
            .Verifiable();
        (await wrapper.GetPointsByOsmIds(new[] { 789L, 101112 }))
            .Should()
            .BeEquivalentTo(points.Skip(2));
        osmPort.Verify();

        // third call should be cached
        osmPort
            .Setup(
                x =>
                    x.GetPointsByOsmIds(
                        It.Is<IEnumerable<long>>(e => e.SequenceEqual(new[] { 987L }))
                    )
            )
            .ReturnsAsync(new OsmPoint?[] { null })
            .Verifiable();
        (await wrapper.GetPointsByOsmIds(new[] { 101112L, 987 }))
            .Should()
            .BeEquivalentTo(points.Skip(3).Append(null));
        osmPort.Verify();

        osmPort.VerifyNoOtherCalls();
    }
}
