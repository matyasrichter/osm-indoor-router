using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SourceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "RoutingNodes",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "RoutingEdges",
                type: "integer",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SourceType", table: "RoutingNodes");

            migrationBuilder.DropColumn(name: "SourceType", table: "RoutingEdges");
        }
    }
}
