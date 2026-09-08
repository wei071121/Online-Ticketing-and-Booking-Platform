# TAR UMT Campus Event Ticketing Platform

A proof-of-concept online booking and ticketing platform for TAR UMT campus events. It is designed for the AMIT3253 Cloud Computing for Business assignment.

## Current features

- Browse available campus events
- View price, date, venue, and tickets remaining
- Choose a ticket quantity and submit a booking
- Store bookings in MySQL using prepared statements
- Prevent overselling by locking the event row while processing a booking
- Provide a lightweight `/health.php` endpoint for future load-balancer health checks

## Technology

- PHP 8+
- MySQL 8+ locally, then Amazon RDS for MySQL on AWS
- Apache on Amazon EC2 in the deployment phase

## Local setup

1. Create a local MySQL database by importing `database/schema.sql`.
2. Copy `config/config.example.php` to `config/config.php`.
3. Update the database credentials in `config/config.php`.
4. From the project root, start the PHP development server:

   ```powershell
   php -S localhost:8000 -t public
   ```

5. Open `http://localhost:8000` in a browser.

## AWS deployment plan

1. Deploy the PHP application to EC2 for the initial working application.
2. Move MySQL data to Amazon RDS in private subnets.
3. Put EC2 instances behind an Application Load Balancer.
4. Create an Auto Scaling Group across two Availability Zones.
5. Run a load test and capture AWS metrics for the report.

## Security reminder

Never commit `config/config.php`, AWS credentials, database passwords, `.pem` key files, or `.env` files to GitHub.

