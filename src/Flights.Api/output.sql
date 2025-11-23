CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE aircraft (
    id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    tail_number character varying(12) NOT NULL,
    equipment_code character varying(4) NOT NULL,
    CONSTRAINT pk_aircraft PRIMARY KEY (id)
);

CREATE TABLE airports (
    id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    time_zone_id character varying(100) NOT NULL,
    iata_code character varying(3) NOT NULL,
    icao_code character varying(4) NOT NULL,
    name character varying(100) NOT NULL,
    CONSTRAINT pk_airports PRIMARY KEY (id)
);

CREATE TABLE flights (
    id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    flight_number_iata character varying(6) NOT NULL,
    flight_number_icao character varying(7) NOT NULL,
    departure_local_time timestamp without time zone NOT NULL,
    arrival_local_time timestamp without time zone NOT NULL,
    scheduling_ambiguity_policy character varying(20) NOT NULL,
    departure_airport_id uuid NOT NULL,
    arrival_airport_id uuid NOT NULL,
    aircraft_id uuid NOT NULL,
    business_price_amount numeric NOT NULL,
    economy_price_amount numeric NOT NULL,
    CONSTRAINT pk_flights PRIMARY KEY (id),
    CONSTRAINT fk_flights_aircraft_aircraft_id FOREIGN KEY (aircraft_id) REFERENCES aircraft (id) ON DELETE RESTRICT,
    CONSTRAINT fk_flights_airports_arrival_airport_id FOREIGN KEY (arrival_airport_id) REFERENCES airports (id) ON DELETE RESTRICT,
    CONSTRAINT fk_flights_airports_departure_airport_id FOREIGN KEY (departure_airport_id) REFERENCES airports (id) ON DELETE RESTRICT
);

CREATE INDEX ix_flights_aircraft_id ON flights (aircraft_id);

CREATE INDEX ix_flights_arrival_airport_id ON flights (arrival_airport_id);

CREATE INDEX ix_flights_departure_airport_id ON flights (departure_airport_id);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20251115051619_InitialCreate', '10.0.0-rc.2.25502.107');

COMMIT;

