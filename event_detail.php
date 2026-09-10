<?php
require 'config.php';
require 'auth.php';
require 'helpers.php';

$eventId = (int)($_GET['id'] ?? 0);
$stmt = $conn->prepare('SELECT *, (total_tickets - tickets_sold) AS remaining FROM events WHERE id = ?');
$stmt->bind_param('i', $eventId);
$stmt->execute();
$event = $stmt->get_result()->fetch_assoc();
$stmt->close();
$event = $event ? event_presentation($event) : null;

if (!$event) {
    http_response_code(404);
    $pageTitle = 'Event not found';
    require 'partials/header.php';
    echo '<div class="empty-state"><div class="empty-state-icon">⌕</div><p>We could not find that event.</p><a class="btn btn-secondary btn-small" href="events.php">Browse events</a></div>';
    require 'partials/footer.php';
    exit;
}

$pageTitle = 'QIGLO - Event Ticketing';
require 'partials/header.php';
?>
<section class="event-detail">
    <div class="event-detail-image">
        <img src="<?= htmlspecialchars(entity_image_url($event)) ?>" alt="<?= htmlspecialchars($event['event_name']) ?>" decoding="async">
        <span class="event-category"><?= htmlspecialchars(strtoupper($event['category'] ?? 'Campus Event')) ?></span>
    </div>
    <div class="event-detail-content">
        <a class="back-link" href="events.php">← All events</a>
        <span class="eyebrow eyebrow-dark"><?= htmlspecialchars($event['category'] ?? 'Live Experience') ?></span>
        <h1><?= htmlspecialchars($event['event_name']) ?></h1>
        <div class="event-detail-facts">
            <div><span class="fact-icon">◷</span><p><strong><?= htmlspecialchars(date('l, d F Y', strtotime($event['event_date']))) ?></strong><small>Event date</small></p></div>
            <div><span class="fact-icon">◴</span><p><strong><?= htmlspecialchars($event['event_time'] ?? 'Time to be announced') ?></strong><small>Event time</small></p></div>
            <div><span class="fact-icon">⌖</span><p><strong><?= htmlspecialchars($event['venue']) ?></strong><small>Location</small></p></div>
        </div>
        <div class="booking-panel">
            <div><small>Registration from</small><strong><?= htmlspecialchars($event['price_label'] ?? ('RM' . number_format($event['ticket_price'], 2))) ?></strong></div>
            <span class="availability <?= $event['remaining'] > 0 ? '' : 'sold-out' ?>"><?= $event['remaining'] > 0 ? (int)$event['remaining'] . ' tickets remaining' : 'Sold out' ?></span>
        </div>
        <p class="ticket-selection-hint"><strong>One session available</strong><span><?= htmlspecialchars($event['event_time'] ?? 'Booking details shown below.') ?></span></p>
        <?php if ($event['remaining'] <= 0): ?>
            <button class="btn reserve-btn" disabled>Sold Out</button>
        <?php elseif (!current_user_id()): ?>
            <a class="btn reserve-btn" href="login.php">Sign in to reserve ticket <span aria-hidden="true">→</span></a>
        <?php elseif ($event['has_seating']): ?>
            <a class="btn reserve-btn" href="seat_select.php?event_id=<?= (int)$event['id'] ?>">Reserve Ticket <span aria-hidden="true">→</span></a>
        <?php else: ?>
            <a class="btn reserve-btn" href="create.php?event_id=<?= (int)$event['id'] ?>">Reserve Ticket <span aria-hidden="true">→</span></a>
        <?php endif; ?>
        <p class="booking-note">Secure booking · Instant ticket confirmation</p>
    </div>
</section>

<section class="event-info-grid">
    <article class="event-overview-card">
        <span class="eyebrow eyebrow-dark">EVENT OVERVIEW</span>
        <h2><?= htmlspecialchars($event['intro'] ?? 'A memorable campus experience.') ?></h2>
        <?php foreach (($event['description'] ?? []) as $paragraph): ?>
            <p><?= htmlspecialchars($paragraph) ?></p>
        <?php endforeach; ?>

        <?php if (!empty($event['highlights'])): ?>
            <h3>What to expect</h3>
            <ul class="event-highlights">
                <?php foreach ($event['highlights'] as $highlight): ?>
                    <li><?= htmlspecialchars($highlight) ?></li>
                <?php endforeach; ?>
            </ul>
        <?php endif; ?>
    </article>

    <aside class="event-side-stack">
        <article class="event-side-card organizer-card">
            <span class="side-card-label">ORGANIZED BY</span>
            <div class="organizer-monogram"><?= htmlspecialchars(mb_strtoupper(mb_substr($event['organizer'] ?? 'E', 0, 1))) ?></div>
            <h3><?= htmlspecialchars($event['organizer'] ?? 'QIGLO') ?></h3>
            <p><?= htmlspecialchars($event['organizer_copy'] ?? 'Your campus event organizer.') ?></p>
        </article>
        <article class="event-side-card registration-card">
            <span class="side-card-label">REGISTRATION DETAILS</span>
            <p><?= htmlspecialchars($event['registration'] ?? 'Reserve your place through the secure booking flow.') ?></p>
        </article>
    </aside>
</section>

<section class="sessions-section">
    <div class="section-heading">
        <div><span class="eyebrow eyebrow-dark">BOOK YOUR PLACE</span><h2>Available session</h2></div>
    </div>
    <article class="session-card">
        <h3><?= htmlspecialchars($event['event_name']) ?></h3>
        <div class="session-facts">
            <span>◷ <?= htmlspecialchars(date('D, d M Y', strtotime($event['event_date']))) ?></span>
            <span>◴ <?= htmlspecialchars($event['event_time'] ?? 'Time to be announced') ?></span>
            <span>⌖ <?= htmlspecialchars($event['venue']) ?></span>
        </div>
        <div class="session-card-footer">
            <div><small>Starting from</small><strong><?= htmlspecialchars($event['price_label'] ?? ('RM' . number_format($event['ticket_price'], 2))) ?></strong></div>
            <?php if ($event['remaining'] <= 0): ?>
                <button class="btn" disabled>Sold Out</button>
            <?php elseif (!current_user_id()): ?>
                <a class="btn" href="login.php">Sign in to book</a>
            <?php elseif ($event['has_seating']): ?>
                <a class="btn" href="seat_select.php?event_id=<?= (int)$event['id'] ?>">Reserve Ticket</a>
            <?php else: ?>
                <a class="btn" href="create.php?event_id=<?= (int)$event['id'] ?>">Reserve Ticket</a>
            <?php endif; ?>
        </div>
    </article>
</section>
<?php require 'partials/footer.php'; ?>
