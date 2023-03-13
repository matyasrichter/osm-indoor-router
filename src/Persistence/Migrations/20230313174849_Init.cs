using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase().Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "RoutingGraphVersions",
                columns: table =>
                    new
                    {
                        Id = table
                            .Column<long>(type: "bigint", nullable: false)
                            .Annotation(
                                "Npgsql:ValueGenerationStrategy",
                                NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                            ),
                        CreatedAt = table.Column<DateTime>(
                            type: "timestamp with time zone",
                            nullable: false
                        ),
                        IsActive = table.Column<bool>(type: "boolean", nullable: false)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingGraphVersions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "RoutingNodes",
                columns: table =>
                    new
                    {
                        Id = table
                            .Column<long>(type: "bigint", nullable: false)
                            .Annotation(
                                "Npgsql:ValueGenerationStrategy",
                                NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                            ),
                        Version = table.Column<long>(type: "bigint", nullable: false),
                        Coordinates = table.Column<Point>(type: "geometry", nullable: false),
                        Level = table.Column<decimal>(type: "numeric", nullable: false),
                        SourceId = table.Column<long>(type: "bigint", nullable: true)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingNodes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "RoutingEdges",
                columns: table =>
                    new
                    {
                        Id = table
                            .Column<long>(type: "bigint", nullable: false)
                            .Annotation(
                                "Npgsql:ValueGenerationStrategy",
                                NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                            ),
                        Version = table.Column<long>(type: "bigint", nullable: false),
                        FromId = table.Column<long>(type: "bigint", nullable: false),
                        ToId = table.Column<long>(type: "bigint", nullable: false),
                        Cost = table.Column<double>(type: "double precision", nullable: false),
                        ReverseCost = table.Column<double>(
                            type: "double precision",
                            nullable: false
                        ),
                        SourceId = table.Column<long>(type: "bigint", nullable: true)
                    },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingEdges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingEdges_RoutingNodes_FromId",
                        column: x => x.FromId,
                        principalTable: "RoutingNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RoutingEdges_RoutingNodes_ToId",
                        column: x => x.ToId,
                        principalTable: "RoutingNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_RoutingEdges_FromId",
                table: "RoutingEdges",
                column: "FromId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RoutingEdges_ToId",
                table: "RoutingEdges",
                column: "ToId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RoutingEdges");

            migrationBuilder.DropTable(name: "RoutingGraphVersions");

            migrationBuilder.DropTable(name: "RoutingNodes");
        }
    }
}
