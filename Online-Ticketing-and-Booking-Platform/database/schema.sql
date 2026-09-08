CREATE DATABASE IF NOT EXISTS ticketing_platform
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE ticketing_platform;

CREATE TABLE events (
    id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(150) NOT NULL,
    description TEXT NOT NULL,
    venue VARCHAR(150) NOT NULL,
    event_date DATETIME NOT NULL,
    ticket_price DECIMAL(10, 2) NOT NULL,
    available_tickets INT UNSIGNED NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT positive_ticket_price CHECK (ticket_price >= 0),
    CONSTRAINT positive_ticket_count CHECK (available_tickets >= 0)
) ENGINE=InnoDB;

CREATE TABLE bookings (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    booking_reference CHAR(16) NOT NULL UNIQUE,
    event_id INT UNSIGNED NOT NULL,
    customer_name VARCHAR(120) NOT NULL,
    customer_email VARCHAR(254) NOT NULL,
    quantity TINYINT UNSIGNED NOT NULL,
    total_amount DECIMAL(10, 2) NOT NULL,
    booked_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT booking_event_fk
        FOREIGN KEY (event_id) REFERENCES events(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT valid_quantity CHECK (quantity BETWEEN 1 AND 10)
) ENGINE=InnoDB;

INSERT INTO events (title, description, venue, event_date, ticket_price, available_tickets) VALUES
    ('Campus Music Night 2026', 'An evening of live music performed by TAR UMT student societies.', 'Main Hall', '2026-11-22 19:30:00', 15.00, 180),
    ('Career Ready Workshop', 'A practical workshop on interview preparation and industry networking.', 'Lecture Theatre A', '2026-10-15 14:00:00', 5.00, 80),
    ('Inter-Faculty Futsal Final', 'Reserve a spectator ticket for the campus futsal championship final.', 'Sports Complex', '2026-10-28 18:00:00', 3.00, 240),
    ('Student Leadership Forum', 'A panel discussion with student leaders and invited industry speakers.', 'DKA Auditorium', '2026-11-05 10:00:00', 0.00, 120);

