CREATE DATABASE IF NOT EXISTS event_ticketing_db;
USE event_ticketing_db;

CREATE TABLE users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  email VARCHAR(150) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  id_number VARCHAR(20) NULL,
  faculty VARCHAR(150) NULL,
  date_of_birth DATE NULL,
  is_admin TINYINT(1) NOT NULL DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Seed admin account: admin@example.com / admin123
-- Change this password immediately in any real deployment.
INSERT INTO users (name, email, password_hash, is_admin) VALUES
('Admin', 'admin@example.com', '$2y$10$HI3gLmyD4OGmfNLAGUIL8.eBhhKu5nzL7wTDws.6mUNO9V44kyM5q', 1);

CREATE TABLE events (
  id INT AUTO_INCREMENT PRIMARY KEY,
  event_name VARCHAR(150) NOT NULL,
  event_date DATE NOT NULL,
  venue VARCHAR(150) NOT NULL,
  ticket_price DECIMAL(10,2) NOT NULL,
  total_tickets INT NOT NULL,
  tickets_sold INT NOT NULL DEFAULT 0,
  image_url VARCHAR(500) NULL,
  has_seating TINYINT(1) NOT NULL DEFAULT 0,
  seat_rows INT NULL,
  seats_per_row INT NULL
);

INSERT INTO events (event_name, event_date, venue, ticket_price, total_tickets, tickets_sold, image_url) VALUES
('Sunway University Ensemble Presents: Once Upon a Time', '2026-10-03', 'Sunway International School, Iskandar Puteri, Johor', 35.00, 180, 0, '/uploads/event-once-upon-a-time.jpg'),
('Lat Tali Lat: A Children''s Night Brass Recital', '2026-10-10', 'Recital Hall, Level 4, FASS, Sunway Square, Sunway University', 35.00, 160, 0, '/uploads/event-lat-tali-lat.jpg'),
('Alumni Homecoming Fiesta 2026', '2026-10-31', 'Multipurpose Hall, Level 4, TAR UMT Arena', 80.00, 300, 0, '/uploads/event-alumni-homecoming.jpg'),
('Tax Budget and Me (TBAM) 2027', '2026-11-04', 'Red Brick Theatre, Arena TAR UMT', 25.00, 220, 0, '/uploads/event-tbam-2027.jpg'),
('TARCian Run 2026', '2026-11-15', 'Sports Complex, TAR UMT KL Campus', 35.00, 500, 0, '/uploads/event-tarcian-run.jpg');

CREATE TABLE orders (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  event_id INT NOT NULL,
  quantity INT NOT NULL,
  total_price DECIMAL(10,2) NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id),
  FOREIGN KEY (event_id) REFERENCES events(id)
);

-- One row per selectable seat, only populated for events with has_seating = 1.
CREATE TABLE seats (
  id INT AUTO_INCREMENT PRIMARY KEY,
  event_id INT NOT NULL,
  row_label VARCHAR(5) NOT NULL,
  seat_number INT NOT NULL,
  is_booked TINYINT(1) NOT NULL DEFAULT 0,
  FOREIGN KEY (event_id) REFERENCES events(id),
  UNIQUE KEY unique_event_seat (event_id, row_label, seat_number)
);

-- Selected indoor events retain the existing assigned-seating experience.
UPDATE events SET has_seating = 1, seat_rows = 12, seats_per_row = 15 WHERE event_name = 'Sunway University Ensemble Presents: Once Upon a Time';
UPDATE events SET has_seating = 1, seat_rows = 10, seats_per_row = 16 WHERE event_name = 'Lat Tali Lat: A Children''s Night Brass Recital';
UPDATE events SET has_seating = 1, seat_rows = 11, seats_per_row = 20 WHERE event_name = 'Tax Budget and Me (TBAM) 2027';

INSERT INTO seats (event_id, row_label, seat_number)
WITH RECURSIVE seq AS (
  SELECT 0 AS n
  UNION ALL
  SELECT n + 1 FROM seq WHERE n < 179
)
SELECT (SELECT id FROM events WHERE event_name = 'Sunway University Ensemble Presents: Once Upon a Time'), CHAR(65 + FLOOR(n / 15)), MOD(n, 15) + 1
FROM seq;

INSERT INTO seats (event_id, row_label, seat_number)
WITH RECURSIVE seq AS (
  SELECT 0 AS n
  UNION ALL
  SELECT n + 1 FROM seq WHERE n < 159
)
SELECT (SELECT id FROM events WHERE event_name = 'Lat Tali Lat: A Children''s Night Brass Recital'), CHAR(65 + FLOOR(n / 16)), MOD(n, 16) + 1
FROM seq;

INSERT INTO seats (event_id, row_label, seat_number)
WITH RECURSIVE seq AS (
  SELECT 0 AS n
  UNION ALL
  SELECT n + 1 FROM seq WHERE n < 219
)
SELECT (SELECT id FROM events WHERE event_name = 'Tax Budget and Me (TBAM) 2027'), CHAR(65 + FLOOR(n / 20)), MOD(n, 20) + 1
FROM seq;

-- One row per individual ticket (one per unit purchased, whether seated or
-- general admission), each with its own unique QR check-in token. The token
-- is a random opaque string, not the attendee's personal info - the scanner
-- looks up attendee/event/seat details server-side from this token.
CREATE TABLE tickets (
  id INT AUTO_INCREMENT PRIMARY KEY,
  order_id INT NOT NULL,
  seat_id INT NULL,
  qr_token VARCHAR(64) NOT NULL UNIQUE,
  checked_in_at TIMESTAMP NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (order_id) REFERENCES orders(id),
  FOREIGN KEY (seat_id) REFERENCES seats(id)
);

CREATE TABLE testimonials (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  event_id INT NOT NULL,
  comment TEXT NOT NULL,
  rating TINYINT NOT NULL DEFAULT 5,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id),
  FOREIGN KEY (event_id) REFERENCES events(id)
);

CREATE TABLE contact_messages (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  email VARCHAR(150) NOT NULL,
  subject VARCHAR(150) NOT NULL,
  message TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- PHP sessions are stored here instead of on local disk, so that any EC2
-- instance behind an ALB/ASG can read a session written by a different
-- instance. See auth.php's DbSessionHandler.
CREATE TABLE sessions (
  id VARCHAR(128) PRIMARY KEY,
  data MEDIUMTEXT NOT NULL,
  last_activity INT NOT NULL,
  INDEX idx_last_activity (last_activity)
);
