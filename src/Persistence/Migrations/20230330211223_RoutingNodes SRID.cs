using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoutingNodesSRID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Coordinates",
                table: "RoutingNodes",
                type: "geometry(Point,4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Coordinates",
                table: "RoutingNodes",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry(Point,4326)"
            );
        }
    }
}
