﻿// <auto-generated />
using System;
using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Flights.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Flights.Api.Domain.Airports.Airport", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("IataCode")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)")
                        .HasColumnName("iata_code");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<string>("TimeZoneId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("time_zone_id");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_airports");

                    b.ToTable("airports", (string)null);
                });

            modelBuilder.Entity("Flights.Api.Domain.Bookings.Booking", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid>("FlightId")
                        .HasColumnType("uuid")
                        .HasColumnName("flight_id");

                    b.Property<bool>("IsCancelled")
                        .HasColumnType("boolean")
                        .HasColumnName("is_cancelled");

                    b.Property<Guid>("ItineraryId")
                        .HasColumnType("uuid")
                        .HasColumnName("itinerary_id");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_bookings");

                    b.HasIndex("FlightId")
                        .HasDatabaseName("ix_bookings_flight_id");

                    b.HasIndex("ItineraryId")
                        .HasDatabaseName("ix_bookings_itinerary_id");

                    b.ToTable("bookings", (string)null);
                });

            modelBuilder.Entity("Flights.Api.Domain.Flights.Flight", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("ArrivalAirportId")
                        .HasColumnType("uuid")
                        .HasColumnName("arrival_airport_id");

                    b.Property<LocalDateTime>("ArrivalLocalTime")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("arrival_local_time");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid>("DepartureAirportId")
                        .HasColumnType("uuid")
                        .HasColumnName("departure_airport_id");

                    b.Property<LocalDateTime>("DepartureLocalTime")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("departure_local_time");

                    b.Property<string>("FlightNumber")
                        .IsRequired()
                        .HasMaxLength(6)
                        .IsUnicode(false)
                        .HasColumnType("character varying(6)")
                        .HasColumnName("flight_number");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_flights");

                    b.HasIndex("ArrivalAirportId")
                        .HasDatabaseName("ix_flights_arrival_airport_id");

                    b.HasIndex("DepartureAirportId")
                        .HasDatabaseName("ix_flights_departure_airport_id");

                    b.ToTable("flights", (string)null);
                });

            modelBuilder.Entity("Flights.Api.Domain.Itineraries.Itinerary", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("LeadPassengerEmail")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("lead_passenger_email");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("reference");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_itineraries");

                    b.ToTable("itineraries", (string)null);
                });

            modelBuilder.Entity("Flights.Api.Domain.Passengers.Passenger", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("BookingId")
                        .HasColumnType("uuid")
                        .HasColumnName("booking_id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("first_name");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("last_name");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_passenger");

                    b.HasIndex("BookingId")
                        .HasDatabaseName("ix_passenger_booking_id");

                    b.ToTable("passenger", (string)null);
                });

            modelBuilder.Entity("Flights.Api.Domain.Seats.Seat", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid>("FlightId")
                        .HasColumnType("uuid")
                        .HasColumnName("flight_id");

                    b.Property<Guid?>("PassengerId")
                        .HasColumnType("uuid")
                        .HasColumnName("passenger_id");

                    b.Property<decimal>("Price")
                        .HasColumnType("NUMERIC(9,2)")
                        .HasColumnName("price");

                    b.Property<string>("SeatNumber")
                        .IsRequired()
                        .HasMaxLength(3)
                        .IsUnicode(false)
                        .HasColumnType("character varying(3)")
                        .HasColumnName("seat_number");

                    b.Property<string>("SeatType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("seat_type");

                    b.Property<Instant>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<byte[]>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("bytea")
                        .HasColumnName("version");

                    b.HasKey("Id")
                        .HasName("pk_seats");

                    b.HasIndex("PassengerId")
                        .HasDatabaseName("ix_seats_passenger_id");

                    b.HasIndex("FlightId", "PassengerId")
                        .IsUnique()
                        .HasDatabaseName("ix_seats_flight_id_passenger_id")
                        .HasFilter("passenger_id IS NOT NULL");

                    b.HasIndex("FlightId", "SeatNumber")
                        .IsUnique()
                        .HasDatabaseName("ix_seats_flight_id_seat_number");

                    b.ToTable("seats", (string)null);
                });

            modelBuilder.Entity("Flights.Api.Domain.Bookings.Booking", b =>
                {
                    b.HasOne("Flights.Api.Domain.Flights.Flight", "Flight")
                        .WithMany()
                        .HasForeignKey("FlightId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_bookings_flights_flight_id");

                    b.HasOne("Flights.Api.Domain.Itineraries.Itinerary", null)
                        .WithMany("Bookings")
                        .HasForeignKey("ItineraryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_bookings_itineraries_itinerary_id");

                    b.Navigation("Flight");
                });

            modelBuilder.Entity("Flights.Api.Domain.Flights.Flight", b =>
                {
                    b.HasOne("Flights.Api.Domain.Airports.Airport", "ArrivalAirport")
                        .WithMany()
                        .HasForeignKey("ArrivalAirportId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_flights_airports_arrival_airport_id");

                    b.HasOne("Flights.Api.Domain.Airports.Airport", "DepartureAirport")
                        .WithMany()
                        .HasForeignKey("DepartureAirportId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_flights_airports_departure_airport_id");

                    b.Navigation("ArrivalAirport");

                    b.Navigation("DepartureAirport");
                });

            modelBuilder.Entity("Flights.Api.Domain.Passengers.Passenger", b =>
                {
                    b.HasOne("Flights.Api.Domain.Bookings.Booking", null)
                        .WithMany("Passengers")
                        .HasForeignKey("BookingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_passenger_bookings_booking_id");
                });

            modelBuilder.Entity("Flights.Api.Domain.Seats.Seat", b =>
                {
                    b.HasOne("Flights.Api.Domain.Flights.Flight", "Flight")
                        .WithMany("Seats")
                        .HasForeignKey("FlightId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_seats_flights_flight_id");

                    b.HasOne("Flights.Api.Domain.Passengers.Passenger", "Passenger")
                        .WithMany()
                        .HasForeignKey("PassengerId")
                        .HasConstraintName("fk_seats_passenger_passenger_id");

                    b.Navigation("Flight");

                    b.Navigation("Passenger");
                });

            modelBuilder.Entity("Flights.Api.Domain.Bookings.Booking", b =>
                {
                    b.Navigation("Passengers");
                });

            modelBuilder.Entity("Flights.Api.Domain.Flights.Flight", b =>
                {
                    b.Navigation("Seats");
                });

            modelBuilder.Entity("Flights.Api.Domain.Itineraries.Itinerary", b =>
                {
                    b.Navigation("Bookings");
                });
#pragma warning restore 612, 618
        }
    }
}
