<?php
require 'config.php';
require 'auth.php';
require 'helpers.php';

$search = trim($_GET['q'] ?? '');
$events = event_presentation_list($conn->query('SELECT *, (total_tickets - tickets_sold) AS remaining FROM events ORDER BY event_date')->fetch_all(MYSQLI_ASSOC));
if ($search !== '') {
    $events = array_values(array_filter($events, function ($event) use ($search) {
        return stripos($event['event_name'] . ' ' . $event['venue'] . ' ' . ($event['category'] ?? ''), $search) !== false;
    }));
}

$myOrders = [];
if ($uid = current_user_id()) {
    $stmt = $conn->prepare('
        SELECT o.id, e.id AS event_id, e.event_name, e.has_seating, o.quantity, o.total_price
        FROM orders o
        JOIN events e ON e.id = o.event_id
        WHERE o.user_id = ?
        ORDER BY o.created_at DESC
    ');
    $stmt->bind_param('i', $uid);
    $stmt->execute();
    $myOrders = $stmt->get_result()->fetch_all(MYSQLI_ASSOC);
    $stmt->close();
    $myOrders = array_map(function ($order) {
        $event = event_presentation(['id' => $order['event_id'], 'event_name' => $order['event_name']]);
        return $event ? array_merge($order, ['event_name' => $event['event_name']]) : $order;
    }, $myOrders);
}

$pageTitle = 'QIGLO - Event Ticketing';
require 'partials/header.php';
?>
<section class="hero hero-home">
<div class="hero-copy">
<span class="eyebrow"><span class="eyebrow-dot"></span> Campus events, all in one place</span>
<h1>Find your next <em>experience.</em></h1>
<p>Discover and book amazing events, concerts and activities in one place.</p>
<div class="hero-actions">
<a class="btn btn-lime" href="events.php">Browse Events <span aria-hidden="true">→</span></a>
<a class="btn btn-ghost" href="contact.php">Host an Event</a>
</div>
</div>
<div class="hero-visual" aria-label="Featured event preview">
<?php if (!empty($events)): $featured = $events[0]; ?>
<img src="<?= htmlspecialchars(entity_image_url($featured)) ?>" alt="<?= htmlspecialchars($featured['event_name']) ?>">
<div class="hero-float-card hero-date-card"><span><?= htmlspecialchars(date('M', strtotime($featured['event_date']))) ?></span><strong><?= htmlspecialchars(date('d', strtotime($featured['event_date']))) ?></strong></div>
<div class="hero-float-card hero-event-card"><span class="mini-label">FEATURED EVENT</span><strong><?= htmlspecialchars($featured['event_name']) ?></strong><small><?= htmlspecialchars($featured['venue']) ?></small></div>
<?php else: ?>
<div class="hero-empty-visual"><span>✦</span><strong>Your next event starts here.</strong></div>
<?php endif; ?>
</div>
</section>

<section class="feature-section">
<div class="section-intro">
<span class="eyebrow eyebrow-dark">WHAT YOU CAN DO</span>
<h2>Everything you need for a great day out.</h2>
</div>
<div class="feature-grid">
<article class="feature-card"><div class="feature-icon">◉</div><h3>Buy Tickets</h3><p>Discover concerts, workshops and events near you.</p><a href="events.php">Explore events <span>→</span></a></article>
<article class="feature-card"><div class="feature-icon">⌂</div><h3>Book Venues</h3><p>Reserve halls, courts and spaces easily.</p><a href="contact.php">Get in touch <span>→</span></a></article>
<article class="feature-card"><div class="feature-icon">✦</div><h3>Host an Event</h3><p>Create your event and reach more people.</p><a href="contact.php">Start hosting <span>→</span></a></article>
</div>
</section>

<section class="events-section">
<div class="section-heading">
<div><span class="eyebrow eyebrow-dark">DON'T MISS OUT</span><h2>Upcoming events</h2></div>
<a class="text-link" href="events.php">View all events <span>→</span></a>
</div>
<form method="get" class="filter-bar" id="event-filter-form">
<label><span class="sr-only">Search events</span><input type="text" name="q" id="event-search" placeholder="Search events, artists or venues..." value="<?= htmlspecialchars($search) ?>" autocomplete="off"></label>
<button type="submit">Search</button>
<?php if ($search !== ''): ?><a class="btn btn-secondary" href="index.php">Clear</a><?php endif; ?>
</form>
<script>
(function () {
    var input = document.getElementById('event-search');
    var form = document.getElementById('event-filter-form');
    if (!input || !form) return;
    var timer;
    input.addEventListener('input', function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            form.submit();
        }, 500);
    });
})();
</script>

<?php if (empty($events)): ?>
<div class="empty-state">
<div class="empty-state-icon">&#128269;</div>
<p>No events match your search.</p>
<a class="btn btn-small btn-secondary" href="index.php">Clear filters</a>
</div>
<?php else: ?>
<div class="card-grid event-grid">
<?php foreach ($events as $e): ?>
<article class="card event-card">
<div class="event-card-image-wrap">
<img class="card-thumb" src="<?= htmlspecialchars(entity_image_url($e)) ?>" alt="<?= htmlspecialchars($e['event_name']) ?>" loading="lazy">
<span class="event-category"><?= htmlspecialchars(strtoupper($e['category'] ?? 'Campus Event')) ?></span>
</div>
<h3><?= htmlspecialchars($e['event_name']) ?></h3>
<p class="event-meta"><span>◷ <?= htmlspecialchars(date('D, d M Y', strtotime($e['event_date']))) ?> · <?= htmlspecialchars($e['event_time'] ?? '') ?></span><span>⌖ <?= htmlspecialchars($e['venue']) ?></span></p>
<div class="event-card-footer"><div><small>Tickets from</small><strong><?= htmlspecialchars($e['price_label'] ?? ('RM' . number_format($e['ticket_price'], 2))) ?></strong></div><a class="btn btn-small btn-secondary" href="event_detail.php?id=<?= (int)$e['id'] ?>">View Event</a></div>
</article>
<?php endforeach; ?>
</div>
<?php endif; ?>
</section>

<section id="my-orders" class="orders-section">
<h2>My Orders</h2>
<?php if (!current_user_id()): ?>
<p><a href="login.php">Login</a> or <a href="register.php">register</a> to view and manage your ticket orders.</p>
<?php elseif (empty($myOrders)): ?>
<div class="empty-state">
<div class="empty-state-icon">&#127903;</div>
<p>You haven't bought any tickets yet.</p>
</div>
<?php else: ?>
<table>
<tr><th>Event</th><th>Qty</th><th>Total (RM)</th><th>Actions</th></tr>
<?php foreach ($myOrders as $o): ?>
<tr>
<td><?= htmlspecialchars($o['event_name']) ?></td>
<td><?= (int)$o['quantity'] ?></td>
<td><?= number_format($o['total_price'], 2) ?></td>
<td>
<a class="btn btn-secondary btn-small" href="confirmation.php?id=<?= (int)$o['id'] ?>">View Tickets</a>
<?php if (!$o['has_seating']): ?>
<a class="btn btn-secondary btn-small" href="edit.php?id=<?= (int)$o['id'] ?>">Edit</a>
<?php endif; ?>
<form action="delete.php" method="post" style="display:inline" onsubmit="return confirm('Cancel this order?');">
<input type="hidden" name="id" value="<?= (int)$o['id'] ?>">
<button type="submit" class="btn-small btn-danger">Cancel</button>
</form>
</td>
</tr>
<?php endforeach; ?>
</table>
<?php endif; ?>
</section>
<?php require 'partials/footer.php'; ?>
