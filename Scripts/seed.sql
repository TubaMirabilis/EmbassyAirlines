CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

INSERT INTO airports (id, created_at, updated_at, name, iata_code, time_zone_id) VALUES
    ('12ddca75-728b-418d-92e1-7af259e735db', now(), now(), 'Denver International Airport', 'DEN', 'America/Denver'),
    ('c00a4554-b247-4626-b917-134a4c0a26c2', now(), now(), 'Hartsfield-Jackson Atlanta International Airport', 'ATL', 'America/New_York'),
    ('d0b6e528-0f53-4a88-93e7-6028510c39ce', now(), now(), 'San Francisco International Airport', 'SFO', 'America/Los_Angeles'),
    ('e07a7f7f-b1fc-431a-a97d-172af07bc755', now(), now(), 'Seattle-Tacoma International Airport', 'SEA', 'America/Los_Angeles'),
    ('a7872ef3-e25c-4662-878c-40a5b5a195f8', now(), now(), 'McCarran International Airport', 'LAS', 'America/Los_Angeles'),
    ('166ceac5-582d-49b2-bb21-2d325324f4f5', now(), now(), 'Orlando International Airport', 'MCO', 'America/New_York'),
    ('ae92419e-748b-44c0-b446-a6eb2f9a1af9', now(), now(), 'Miami International Airport', 'MIA', 'America/New_York'),
    ('bbcfcf4d-2ebe-4170-aadc-e5f2d41b9f58', now(), now(), 'Phoenix Sky Harbor International Airport', 'PHX', 'America/Phoenix');

INSERT INTO flights (id, created_at, updated_at, flight_number, departure_local_time, arrival_local_time, departure_airport_id, arrival_airport_id) VALUES
    (uuid_generate_v4(), now(), now(), 'EX123', now() + interval '1 day', now() + interval '1 day 2 hours', '12ddca75-728b-418d-92e1-7af259e735db', 'c00a4554-b247-4626-b917-134a4c0a26c2'),
    (uuid_generate_v4(), now(), now(), 'EX124', now() + interval '1 day', now() + interval '1 day 2 hours', 'c00a4554-b247-4626-b917-134a4c0a26c2', 'd0b6e528-0f53-4a88-93e7-6028510c39ce'),
    (uuid_generate_v4(), now(), now(), 'EX125', now() + interval '1 day', now() + interval '1 day 2 hours', 'd0b6e528-0f53-4a88-93e7-6028510c39ce', 'e07a7f7f-b1fc-431a-a97d-172af07bc755'),
    (uuid_generate_v4(), now(), now(), 'EX126', now() + interval '1 day', now() + interval '1 day 2 hours', 'e07a7f7f-b1fc-431a-a97d-172af07bc755', 'a7872ef3-e25c-4662-878c-40a5b5a195f8'),
    (uuid_generate_v4(), now(), now(), 'EX127', now() + interval '1 day', now() + interval '1 day 2 hours', 'a7872ef3-e25c-4662-878c-40a5b5a195f8', '166ceac5-582d-49b2-bb21-2d325324f4f5'),
    (uuid_generate_v4(), now(), now(), 'EX128', now() + interval '1 day', now() + interval '1 day 2 hours', '166ceac5-582d-49b2-bb21-2d325324f4f5', 'ae92419e-748b-44c0-b446-a6eb2f9a1af9');