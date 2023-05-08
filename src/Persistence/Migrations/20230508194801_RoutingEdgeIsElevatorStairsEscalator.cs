using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoutingEdgeIsElevatorStairsEscalator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsElevator",
                table: "RoutingEdges",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsEscalator",
                table: "RoutingEdges",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsStairs",
                table: "RoutingEdges",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsElevator", table: "RoutingEdges");

            migrationBuilder.DropColumn(name: "IsEscalator", table: "RoutingEdges");

            migrationBuilder.DropColumn(name: "IsStairs", table: "RoutingEdges");
        }
    }
}
