<?php
require 'config.php';
require 'auth.php';
require 'helpers.php';

$events = event_presentation_list($conn->query('SELECT *, (total_tickets - tickets_sold) AS remaining FROM events ORDER BY event_date')->fetch_all(MYSQLI_ASSOC));

$totalEvents = count($events);
$totalRemaining = array_sum(array_column($events, 'remaining'));

$pageTitle = 'All Events';
require 'partials/header.php';
?>
<div class="page-header marketplace-header">
<span class="eyebrow eyebrow-dark">DISCOVER EXPERIENCES</span>
<h1>Find an event you'll love.</h1>
<p>From late-night concerts to thoughtful campus talks, your next great memory is waiting.</p>
</div>

<section>
<div class="marketplace-stats"><span><strong><?= (int)$totalEvents ?></strong> upcoming events</span><span><strong><?= (int)$totalRemaining ?></strong> tickets available</span></div>
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
</section>
<?php require 'partials/footer.php'; ?>
