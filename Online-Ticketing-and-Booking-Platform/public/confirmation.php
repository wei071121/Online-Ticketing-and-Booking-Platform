<?php

declare(strict_types=1);

require_once __DIR__ . '/../config/database.php';

$reference = strtoupper(trim((string) ($_GET['ref'] ?? '')));

if (!preg_match('/^[A-F0-9]{16}$/', $reference)) {
    http_response_code(400);
    exit('A valid booking reference is required.');
}

try {
    $database = databaseConnection();
    $statement = $database->prepare(
        'SELECT b.booking_reference, b.customer_name, b.quantity, b.total_amount, b.booked_at,
                e.title, e.venue, e.event_date
         FROM bookings b
         INNER JOIN events e ON e.id = b.event_id
         WHERE b.booking_reference = ?'
    );
    $statement->bind_param('s', $reference);
    $statement->execute();
    $booking = $statement->get_result()->fetch_assoc();
    $statement->close();
} catch (Throwable $exception) {
    http_response_code(500);
    exit('The booking service is temporarily unavailable.');
}

if ($booking === null) {
    http_response_code(404);
    exit('This booking reference was not found.');
}

$eventDate = new DateTimeImmutable($booking['event_date']);
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Booking confirmed · TAR UMT Tickets</title>
    <link rel="stylesheet" href="assets/styles.css">
</head>
<body>
<header class="site-header">
    <div class="container header-content">
        <a class="brand" href="index.php">TAR UMT <span>Tickets</span></a>
    </div>
</header>

<main class="container confirmation-layout">
    <section class="confirmation-card">
        <p class="success-mark" aria-hidden="true">✓</p>
        <p class="eyebrow">BOOKING CONFIRMED</p>
        <h1>Thank you, <?= escapeHtml($booking['customer_name']) ?>.</h1>
        <p class="confirmation-copy">Your booking has been saved successfully. Keep this reference for the demonstration.</p>

        <div class="booking-reference">
            <span>Booking reference</span>
            <strong><?= escapeHtml($booking['booking_reference']) ?></strong>
        </div>

        <dl class="receipt-details">
            <div><dt>Event</dt><dd><?= escapeHtml($booking['title']) ?></dd></div>
            <div><dt>When</dt><dd><?= $eventDate->format('D, d M Y · g:i A') ?></dd></div>
            <div><dt>Venue</dt><dd><?= escapeHtml($booking['venue']) ?></dd></div>
            <div><dt>Tickets</dt><dd><?= (int) $booking['quantity'] ?></dd></div>
            <div><dt>Total</dt><dd>RM <?= number_format((float) $booking['total_amount'], 2) ?></dd></div>
        </dl>

        <a class="button" href="index.php">Browse more events</a>
    </section>
</main>

<footer class="site-footer">
    <div class="container">AMIT3253 Cloud Computing for Business · Proof of Concept</div>
</footer>
</body>
</html>

