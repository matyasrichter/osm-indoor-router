﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Persistence;

#nullable disable

namespace Persistence.Migrations
{
    [DbContext(typeof(RoutingDbContext))]
    [Migration("20230508194801_RoutingEdgeIsElevatorStairsEscalator")]
    partial class RoutingEdgeIsElevatorStairsEscalator
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "hstore");
            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "pgrouting");
            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "postgis");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Persistence.Entities.PgRouting.PgRoutingAStarOneToOneResult", b =>
                {
                    b.Property<double>("AggCost")
                        .HasColumnType("double precision")
                        .HasColumnName("agg_cost");

                    b.Property<double>("Cost")
                        .HasColumnType("double precision")
                        .HasColumnName("cost");

                    b.Property<long?>("Edge")
                        .HasColumnType("bigint")
                        .HasColumnName("edge");

                    b.Property<long>("Node")
                        .HasColumnType("bigint")
                        .HasColumnName("node");

                    b.Property<long>("PathSeq")
                        .HasColumnType("bigint")
                        .HasColumnName("path_seq");

                    b.Property<long>("Seq")
                        .HasColumnType("bigint")
                        .HasColumnName("seq");

                    b.HasIndex("Edge");

                    b.HasIndex("Node");

                    b.ToTable((string)null);

                    b.ToView(null, (string)null);
                });

            modelBuilder.Entity("Persistence.Entities.Processed.RoutingEdge", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<double>("Cost")
                        .HasColumnType("double precision");

                    b.Property<double>("Distance")
                        .HasColumnType("double precision");

                    b.Property<long>("FromId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsElevator")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsEscalator")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsStairs")
                        .HasColumnType("boolean");

                    b.Property<double>("ReverseCost")
                        .HasColumnType("double precision");

                    b.Property<long?>("SourceId")
                        .HasColumnType("bigint");

                    b.Property<int?>("SourceType")
                        .HasColumnType("integer");

                    b.Property<long>("ToId")
                        .HasColumnType("bigint");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("FromId");

                    b.HasIndex("ToId");

                    b.ToTable("RoutingEdges");
                });

            modelBuilder.Entity("Persistence.Entities.Processed.RoutingGraphVersion", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("RoutingGraphVersions");
                });

            modelBuilder.Entity("Persistence.Entities.Processed.RoutingNode", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<Point>("Coordinates")
                        .IsRequired()
                        .HasColumnType("geometry(Point,4326)");

                    b.Property<bool>("IsLevelConnection")
                        .HasColumnType("boolean");

                    b.Property<decimal>("Level")
                        .HasColumnType("numeric");

                    b.Property<long?>("SourceId")
                        .HasColumnType("bigint");

                    b.Property<int?>("SourceType")
                        .HasColumnType("integer");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("RoutingNodes");
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmLine", b =>
                {
                    b.Property<long>("WayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("way_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("WayId"));

                    b.Property<LineString>("Geometry")
                        .IsRequired()
                        .HasColumnType("geometry")
                        .HasColumnName("geom");

                    b.Property<List<long>>("Nodes")
                        .IsRequired()
                        .HasColumnType("bigint[]")
                        .HasColumnName("nodes");

                    b.Property<IReadOnlyDictionary<string, string>>("Tags")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("tags");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("WayId");

                    b.ToTable("osm_lines", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmMultiPolygon", b =>
                {
                    b.Property<long>("AreaId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("area_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("AreaId"));

                    b.Property<MultiPolygon>("Geometry")
                        .IsRequired()
                        .HasColumnType("geometry")
                        .HasColumnName("geom");

                    b.Property<IReadOnlyDictionary<string, string>>("Tags")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("tags");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("AreaId");

                    b.ToTable("osm_multipolygons", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmMultiPolygonM2M", b =>
                {
                    b.Property<long>("OsmLineId")
                        .HasColumnType("bigint")
                        .HasColumnName("l_id");

                    b.Property<long>("OsmMultiPolygonId")
                        .HasColumnType("bigint")
                        .HasColumnName("mp_id");

                    b.HasKey("OsmLineId", "OsmMultiPolygonId");

                    b.HasIndex("OsmMultiPolygonId");

                    b.ToTable("osm_multipolygons_m2m", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmPoint", b =>
                {
                    b.Property<long>("NodeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("node_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("NodeId"));

                    b.Property<Point>("Geometry")
                        .IsRequired()
                        .HasColumnType("geometry")
                        .HasColumnName("geom");

                    b.Property<IReadOnlyDictionary<string, string>>("Tags")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("tags");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("NodeId");

                    b.ToTable("osm_points", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmPolygon", b =>
                {
                    b.Property<long>("AreaId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("area_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("AreaId"));

                    b.Property<Polygon>("Geometry")
                        .IsRequired()
                        .HasColumnType("geometry")
                        .HasColumnName("geom");

                    b.Property<LineString>("GeometryAsLinestring")
                        .IsRequired()
                        .HasColumnType("geometry")
                        .HasColumnName("geom_linestring");

                    b.Property<List<long>>("Nodes")
                        .IsRequired()
                        .HasColumnType("bigint[]")
                        .HasColumnName("nodes");

                    b.Property<IReadOnlyDictionary<string, string>>("Tags")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("tags");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("AreaId");

                    b.ToTable("osm_polygons", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.PgRouting.PgRoutingAStarOneToOneResult", b =>
                {
                    b.HasOne("Persistence.Entities.Processed.RoutingEdge", "RoutingEdge")
                        .WithMany()
                        .HasForeignKey("Edge");

                    b.HasOne("Persistence.Entities.Processed.RoutingNode", "RoutingNode")
                        .WithMany()
                        .HasForeignKey("Node")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RoutingEdge");

                    b.Navigation("RoutingNode");
                });

            modelBuilder.Entity("Persistence.Entities.Processed.RoutingEdge", b =>
                {
                    b.HasOne("Persistence.Entities.Processed.RoutingNode", "From")
                        .WithMany()
                        .HasForeignKey("FromId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Persistence.Entities.Processed.RoutingNode", "To")
                        .WithMany()
                        .HasForeignKey("ToId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("From");

                    b.Navigation("To");
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmMultiPolygonM2M", b =>
                {
                    b.HasOne("Persistence.Entities.Raw.OsmLine", null)
                        .WithMany()
                        .HasForeignKey("OsmLineId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Persistence.Entities.Raw.OsmMultiPolygon", null)
                        .WithMany()
                        .HasForeignKey("OsmMultiPolygonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
