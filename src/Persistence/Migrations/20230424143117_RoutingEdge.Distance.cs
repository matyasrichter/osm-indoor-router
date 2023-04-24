using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoutingEdgeDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Distance",
                table: "RoutingEdges",
                type: "double precision",
                nullable: true,
                defaultValue: 0.0
            );
            migrationBuilder.Sql("UPDATE \"RoutingEdges\" SET \"Distance\" = \"Cost\";");
            migrationBuilder.AlterColumn<double>(
                name: "Distance",
                table: "RoutingEdges",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Distance", table: "RoutingEdges");
        }
    }
}
