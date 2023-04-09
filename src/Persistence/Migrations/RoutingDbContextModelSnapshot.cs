﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Persistence;

#nullable disable

namespace Persistence.Migrations
{
    [DbContext(typeof(RoutingDbContext))]
    partial class RoutingDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
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

                    b.Property<long>("FromId")
                        .HasColumnType("bigint");

                    b.Property<double>("ReverseCost")
                        .HasColumnType("double precision");

                    b.Property<long?>("SourceId")
                        .HasColumnType("bigint");

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

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("RoutingNodes");
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmLine", b =>
                {
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

                    b.Property<long>("WayId")
                        .HasColumnType("bigint")
                        .HasColumnName("way_id");

                    b.ToTable("osm_lines", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmPoint", b =>
                {
                    b.Property<Point>("Geometry")
                        .IsRequired()
                        .HasColumnType("geometry")
                        .HasColumnName("geom");

                    b.Property<long>("NodeId")
                        .HasColumnType("bigint")
                        .HasColumnName("node_id");

                    b.Property<IReadOnlyDictionary<string, string>>("Tags")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("tags");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.ToTable("osm_points", t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("Persistence.Entities.Raw.OsmPolygon", b =>
                {
                    b.Property<long>("AreaId")
                        .HasColumnType("bigint")
                        .HasColumnName("area_id");

                    b.Property<Polygon>("Geometry")
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
#pragma warning restore 612, 618
        }
    }
}
