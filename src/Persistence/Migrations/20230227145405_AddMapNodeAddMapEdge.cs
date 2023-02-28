#nullable disable

namespace Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

/// <inheritdoc />
public partial class AddMapNodeAddMapEdge : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:postgis", ",,");

        migrationBuilder.CreateTable(
            name: "MapNodes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<Guid>(type: "uuid", nullable: false),
                Coordinates = table.Column<Point>(type: "geometry", nullable: false),
                Level = table.Column<decimal>(type: "numeric", nullable: false),
                SourceId = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_MapNodes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MapEdges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<Guid>(type: "uuid", nullable: false),
                FromId = table.Column<Guid>(type: "uuid", nullable: false),
                ToId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapEdges", x => x.Id);
                table.ForeignKey(
                    name: "FK_MapEdges_MapNodes_FromId",
                    column: x => x.FromId,
                    principalTable: "MapNodes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapEdges_MapNodes_ToId",
                    column: x => x.ToId,
                    principalTable: "MapNodes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MapEdges_FromId",
            table: "MapEdges",
            column: "FromId");

        migrationBuilder.CreateIndex(
            name: "IX_MapEdges_ToId",
            table: "MapEdges",
            column: "ToId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MapEdges");

        migrationBuilder.DropTable(
            name: "MapNodes");
    }
}
