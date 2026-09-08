<?php

declare(strict_types=1);

require_once __DIR__ . '/../config/database.php';

function findEvent(mysqli $database, int $eventId): ?array
{
    $statement = $database->prepare(
        'SELECT id, title, venue, event_date, ticket_price, available_tickets
         FROM events WHERE id = ?'
    );
    $statement->bind_param('i', $eventId);
    $statement->execute();
    $event = $statement->get_result()->fetch_assoc();
    $statement->close();

    return $event ?: null;
}

$eventId = (int) ($_POST['event_id'] ?? $_GET['event_id'] ?? 0);
$errors = [];
$name = trim((string) ($_POST['customer_name'] ?? ''));
$email = trim((string) ($_POST['customer_email'] ?? ''));
$quantity = (int) ($_POST['quantity'] ?? 1);

try {
    $database = databaseConnection();
    $event = findEvent($database, $eventId);
} catch (Throwable $exception) {
    http_response_code(500);
    exit('The booking service is temporarily unavailable. Please check the database configuration.');
}

if ($event === null) {
    http_response_code(404);
    exit('The requested event could not be found.');
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if ($name === '' || mb_strlen($name) > 120) {
        $errors[] = 'Please enter a name of up to 120 characters.';
    }

    if (!filter_var($email, FILTER_VALIDATE_EMAIL) || mb_strlen($email) > 254) {
        $errors[] = 'Please enter a valid email address.';
    }

    if ($quantity < 1 || $quantity > 10) {
        $errors[] = 'You may book between 1 and 10 tickets at one time.';
    }

    if ($errors === []) {
        $transactionStarted = false;

        try {
            $database->begin_transaction();
            $transactionStarted = true;

            $lockedStatement = $database->prepare(
                'SELECT id, title, ticket_price, available_tickets
                 FROM events WHERE id = ? FOR UPDATE'
            );
            $lockedStatement->bind_param('i', $eventId);
            $lockedStatement->execute();
            $lockedEvent = $lockedStatement->get_result()->fetch_assoc();
            $lockedStatement->close();

            if ($lockedEvent === null || (int) $lockedEvent['available_tickets'] < $quantity) {
                throw new RuntimeException('There are not enough tickets remaining for this booking.');
            }

            $bookingReference = strtoupper(bin2hex(random_bytes(8)));
            $totalAmount = (float) $lockedEvent['ticket_price'] * $quantity;

            $bookingStatement = $database->prepare(
                'INSERT INTO bookings
                    (booking_reference, event_id, customer_name, customer_email, quantity, total_amount)
                 VALUES (?, ?, ?, ?, ?, ?)'
            );
            $bookingStatement->bind_param(
                'sissid',
                $bookingReference,
                $eventId,
                $name,
                $email,
                $quantity,
                $totalAmount
            );
            $bookingStatement->execute();
            $bookingStatement->close();

            $inventoryStatement = $database->prepare(
                'UPDATE events SET available_tickets = available_tickets - ? WHERE id = ?'
            );
            $inventoryStatement->bind_param('ii', $quantity, $eventId);
            $inventoryStatement->execute();
            $inventoryStatement->close();

            $database->commit();
            header('Location: confirmation.php?ref=' . urlencode($bookingReference));
            exit;
        } catch (Throwable $exception) {
            if ($transactionStarted) {
                $database->rollback();
            }
            $errors[] = $exception instanceof RuntimeException
                ? $exception->getMessage()
                : 'Your booking could not be completed. Please try again.';
        }
    }
}

$eventDate = new DateTimeImmutable($event['event_date']);
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Book <?= escapeHtml($event['title']) ?> · TAR UMT Tickets</title>
    <link rel="stylesheet" href="assets/styles.css">
</head>
<body>
<header class="site-header">
    <div class="container header-content">
        <a class="brand" href="index.php">TAR UMT <span>Tickets</span></a>
        <a class="text-link" href="index.php">← All events</a>
    </div>
</header>

<main class="container booking-layout">
    <section class="booking-summary" aria-labelledby="event-title">
        <p class="eyebrow">YOUR SELECTION</p>
        <h1 id="event-title"><?= escapeHtml($event['title']) ?></h1>
        <dl class="summary-details">
            <div><dt>Date and time</dt><dd><?= $eventDate->format('l, d F Y · g:i A') ?></dd></div>
            <div><dt>Venue</dt><dd><?= escapeHtml($event['venue']) ?></dd></div>
            <div><dt>Price</dt><dd><?= (float) $event['ticket_price'] > 0 ? 'RM ' . number_format((float) $event['ticket_price'], 2) . ' per ticket' : 'Free entry' ?></dd></div>
            <div><dt>Availability</dt><dd><?= (int) $event['available_tickets'] ?> tickets remaining</dd></div>
        </dl>
    </section>

    <section class="booking-form-card" aria-labelledby="form-title">
        <p class="eyebrow">BOOKING DETAILS</p>
        <h2 id="form-title">Reserve your tickets</h2>

        <?php if ($errors !== []): ?>
            <div class="alert alert-error" role="alert">
                <strong>Please correct the following:</strong>
                <ul>
                    <?php foreach ($errors as $error): ?>
                        <li><?= escapeHtml($error) ?></li>
                    <?php endforeach; ?>
                </ul>
            </div>
        <?php endif; ?>

        <?php if ((int) $event['available_tickets'] < 1): ?>
            <div class="alert alert-error" role="alert">This event is sold out.</div>
        <?php else: ?>
            <form method="post" action="book.php?event_id=<?= (int) $event['id'] ?>">
                <input type="hidden" name="event_id" value="<?= (int) $event['id'] ?>">
                <label for="customer_name">Full name</label>
                <input id="customer_name" name="customer_name" type="text" maxlength="120" value="<?= escapeHtml($name) ?>" required autocomplete="name">

                <label for="customer_email">Email address</label>
                <input id="customer_email" name="customer_email" type="email" maxlength="254" value="<?= escapeHtml($email) ?>" required autocomplete="email">

                <label for="quantity">Number of tickets</label>
                <select id="quantity" name="quantity">
                    <?php for ($ticketNumber = 1; $ticketNumber <= min(10, (int) $event['available_tickets']); $ticketNumber++): ?>
                        <option value="<?= $ticketNumber ?>" <?= $quantity === $ticketNumber ? 'selected' : '' ?>><?= $ticketNumber ?></option>
                    <?php endfor; ?>
                </select>

                <button class="button button-full" type="submit">Confirm booking</button>
            </form>
        <?php endif; ?>
    </section>
</main>

<footer class="site-footer">
    <div class="container">Your details are used only to create this booking record.</div>
</footer>
</body>
</html>

