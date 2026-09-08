<?php

declare(strict_types=1);

require_once __DIR__ . '/../config/database.php';

$events = [];
$loadError = null;

try {
    $database = databaseConnection();
    $result = $database->query(
        'SELECT id, title, description, venue, event_date, ticket_price, available_tickets
         FROM events
         ORDER BY event_date ASC'
    );
    $events = $result->fetch_all(MYSQLI_ASSOC);
} catch (Throwable $exception) {
    $loadError = 'The event list is temporarily unavailable. Please check the database configuration.';
}
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>TAR UMT Campus Tickets</title>
    <link rel="stylesheet" href="assets/styles.css">
</head>
<body>
<header class="site-header">
    <div class="container header-content">
        <a class="brand" href="index.php">TAR UMT <span>Tickets</span></a>
        <p>Campus events, made simple</p>
    </div>
</header>

<main>
    <section class="hero">
        <div class="container">
            <p class="eyebrow">ONLINE BOOKING AND TICKETING PLATFORM</p>
            <h1>Find your next campus event.</h1>
            <p class="hero-copy">Browse upcoming events, select your tickets, and receive an instant booking reference.</p>
        </div>
    </section>

    <section class="container event-section" aria-labelledby="events-heading">
        <div class="section-heading">
            <div>
                <p class="eyebrow">UPCOMING EVENTS</p>
                <h2 id="events-heading">Choose an event</h2>
            </div>
            <span class="event-count"><?= count($events) ?> available</span>
        </div>

        <?php if ($loadError !== null): ?>
            <div class="alert alert-error" role="alert"><?= escapeHtml($loadError) ?></div>
        <?php elseif ($events === []): ?>
            <div class="empty-state">There are no events available right now. Please check back later.</div>
        <?php else: ?>
            <div class="event-grid">
                <?php foreach ($events as $event): ?>
                    <?php $date = new DateTimeImmutable($event['event_date']); ?>
                    <article class="event-card">
                        <div class="event-date">
                            <span><?= $date->format('M') ?></span>
                            <strong><?= $date->format('d') ?></strong>
                        </div>
                        <div class="event-content">
                            <p class="event-time"><?= $date->format('D, d M Y · g:i A') ?></p>
                            <h3><?= escapeHtml($event['title']) ?></h3>
                            <p class="event-description"><?= escapeHtml($event['description']) ?></p>
                            <dl class="event-details">
                                <div><dt>Venue</dt><dd><?= escapeHtml($event['venue']) ?></dd></div>
                                <div><dt>Tickets left</dt><dd><?= (int) $event['available_tickets'] ?></dd></div>
                            </dl>
                        </div>
                        <div class="event-action">
                            <p class="price"><?= (float) $event['ticket_price'] > 0 ? 'RM ' . number_format((float) $event['ticket_price'], 2) : 'Free' ?></p>
                            <?php if ((int) $event['available_tickets'] > 0): ?>
                                <a class="button" href="book.php?event_id=<?= (int) $event['id'] ?>">Book tickets</a>
                            <?php else: ?>
                                <span class="button button-disabled" aria-disabled="true">Sold out</span>
                            <?php endif; ?>
                        </div>
                    </article>
                <?php endforeach; ?>
            </div>
        <?php endif; ?>
    </section>
</main>

<footer class="site-footer">
    <div class="container">AMIT3253 Cloud Computing for Business · Proof of Concept</div>
</footer>
</body>
</html>

