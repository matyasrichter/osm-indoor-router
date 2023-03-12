namespace Persistence.Tests;

using Entities;
using Graph;
using Repositories;
using TestUtils;

[Collection("DB")]
[Trait("Category", "DB")]
public sealed class MapRepositoryTests : DbTestClass
{
    [Fact]
    public async Task CanSaveAndRetrievePoints()
    {
        var repo = new MapRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0) }
        );

        var version = Guid.NewGuid();
        var points = new HashSet<Node>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(1, 2),
                Level = 0,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(2, 3),
                Level = 0,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(3, 4),
                Level = 0,
                SourceId = null
            }
        };

        var graph = await repo.GetGraph(version);
        graph.Should().BeNull();

        await repo.SaveNodes(points, version);

        graph = await repo.GetGraph(version);
        graph.Should().NotBeNull();
        graph!.Nodes.Should().NotBeEmpty().And.BeEquivalentTo(points);
    }

    [Fact]
    public async Task FailsForEdgesWithoutPoints()
    {
        var repo = new MapRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0) }
        );

        var version = Guid.NewGuid();
        var graph = await repo.GetGraph(version);
        graph.Should().BeNull();
        var node = new MapNode()
        {
            Id = Guid.NewGuid(),
            Coordinates = new(1, 2),
            Level = 0,
            SourceId = null
        };
        var edges = new HashSet<Edge>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                FromId = node.Id,
                ToId = node.Id,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                FromId = node.Id,
                ToId = node.Id,
                SourceId = null
            }
        };

        var saving = async () => await repo.SaveEdges(edges, version);

        await saving.Should().ThrowAsync<Exception>();
        graph = await repo.GetGraph(version);
        graph.Should().BeNull();
    }

    [Fact]
    public async Task CanSaveAndRetrieveGraph()
    {
        var repo = new MapRepository(
            DbContext,
            new TestingTimeMachine() { Now = new(2021, 1, 20, 11, 0, 0) }
        );

        var version = Guid.NewGuid();
        var points = new List<Node>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(1, 2),
                Level = 0,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(2, 3),
                Level = 0,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Coordinates = new(3, 4),
                Level = 0,
                SourceId = null
            }
        };
        var edges = new HashSet<Edge>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                FromId = points[0].Id,
                ToId = points[1].Id,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                FromId = points[2].Id,
                ToId = points[1].Id,
                SourceId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                FromId = points[0].Id,
                ToId = points[2].Id,
                SourceId = null
            }
        };

        await repo.SaveNodes(points, version);
        await repo.SaveEdges(edges, version);

        var result = await repo.GetGraph(version);

        result.Should().NotBeNull();
        result!.Nodes.Should().BeEquivalentTo(points);
        result.GetEdgesFromNode(points[0]).Should().HaveCount(2);
        result.GetEdgesFromNode(points[2]).Should().HaveCount(1);
        result.GetEdgesFromNode(points[1]).Should().BeEmpty();
    }

    public MapRepositoryTests(DatabaseFixture dbFixture)
        : base(dbFixture) { }
}
