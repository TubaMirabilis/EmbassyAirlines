﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Flights.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241019031120_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Flights.Api.Entities.Flight", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("FlightNumber")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("flight_number");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.ComplexProperty<Dictionary<string, object>>("AvailableSeats", "Flights.Api.Entities.Flight.AvailableSeats#AvailableSeats", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<int>("Business")
                                .HasColumnType("integer")
                                .HasColumnName("available_seats_business");

                            b1.Property<int>("Economy")
                                .HasColumnType("integer")
                                .HasColumnName("available_seats_economy");
                        });

                    b.ComplexProperty<Dictionary<string, object>>("Pricing", "Flights.Api.Entities.Flight.Pricing#FlightPricing", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<decimal>("BusinessPrice")
                                .HasColumnType("numeric")
                                .HasColumnName("pricing_business_price");

                            b1.Property<decimal>("EconomyPrice")
                                .HasColumnType("numeric")
                                .HasColumnName("pricing_economy_price");
                        });

                    b.ComplexProperty<Dictionary<string, object>>("Schedule", "Flights.Api.Entities.Flight.Schedule#FlightSchedule", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<ZonedDateTime>("ArrivalTime")
                                .HasColumnType("timestamp with time zone")
                                .HasColumnName("schedule_arrival_time");

                            b1.Property<ZonedDateTime>("DepartureTime")
                                .HasColumnType("timestamp with time zone")
                                .HasColumnName("schedule_departure_time");

                            b1.ComplexProperty<Dictionary<string, object>>("DepartureAirport", "Flights.Api.Entities.Flight.Schedule#FlightSchedule.DepartureAirport#Airport", b2 =>
                                {
                                    b2.IsRequired();

                                    b2.Property<string>("IataCode")
                                        .IsRequired()
                                        .HasMaxLength(3)
                                        .IsUnicode(false)
                                        .HasColumnType("character varying(3)")
                                        .HasColumnName("schedule_departure_airport_iata_code")
                                        .HasAnnotation("Npgsql:CheckConstraint", "Departure = upper(Departure)");

                                    b2.Property<string>("TimeZone")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .IsUnicode(false)
                                        .HasColumnType("character varying(50)")
                                        .HasColumnName("schedule_departure_airport_time_zone");
                                });

                            b1.ComplexProperty<Dictionary<string, object>>("DestinationAirport", "Flights.Api.Entities.Flight.Schedule#FlightSchedule.DestinationAirport#Airport", b2 =>
                                {
                                    b2.IsRequired();

                                    b2.Property<string>("IataCode")
                                        .IsRequired()
                                        .HasMaxLength(3)
                                        .IsUnicode(false)
                                        .HasColumnType("character varying(3)")
                                        .HasColumnName("schedule_destination_airport_iata_code")
                                        .HasAnnotation("Npgsql:CheckConstraint", "Destination = upper(Destination)");

                                    b2.Property<string>("TimeZone")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .IsUnicode(false)
                                        .HasColumnType("character varying(50)")
                                        .HasColumnName("schedule_destination_airport_time_zone");
                                });
                        });

                    b.HasKey("Id")
                        .HasName("pk_flights");

                    b.ToTable("flights", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}