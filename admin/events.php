<?php
require '../config.php';
require '../auth.php';
require '../helpers.php';
require_admin();

$flashError = $_SESSION['flash_error'] ?? null;
unset($_SESSION['flash_error']);

$events = $conn->query('SELECT *, (total_tickets - tickets_sold) AS remaining FROM events ORDER BY event_date')->fetch_all(MYSQLI_ASSOC);
$totalEvents = count($events);
$totalCapacity = array_sum(array_column($events, 'total_tickets'));
$totalSold = array_sum(array_column($events, 'tickets_sold'));

$pageTitle = 'Manage Events';
require 'partials/header.php';
?>
<div class="admin-page-header">
<div><span class="eyebrow eyebrow-dark">EVENT MANAGEMENT</span><h1>Events</h1><p>Create, update, and oversee every live experience.</p></div>
<a class="btn" href="event_create.php">+ Add Event</a>
</div>
<div class="admin-summary-grid">
<div class="admin-summary-card"><span>Live events</span><strong><?= (int)$totalEvents ?></strong><small>listed in your marketplace</small></div>
<div class="admin-summary-card"><span>Tickets sold</span><strong><?= (int)$totalSold ?></strong><small>out of <?= (int)$totalCapacity ?> total capacity</small></div>
<div class="admin-summary-card"><span>Availability</span><strong><?= (int)($totalCapacity - $totalSold) ?></strong><small>tickets remaining</small></div>
</div>
<?php if ($flashError): ?><p class="alert alert-error"><?= htmlspecialchars($flashError) ?></p><?php endif; ?>
<table>
<tr><th>Photo</th><th>Name</th><th>Date</th><th>Venue</th><th>Price (RM)</th><th>Type</th><th>Sold / Total</th><th>Actions</th></tr>
<?php foreach ($events as $e): ?>
<tr>
<td><img class="table-thumb" src="<?= htmlspecialchars(entity_image_url($e)) ?>" alt="<?= htmlspecialchars($e['event_name']) ?>" loading="lazy"></td>
<td><?= htmlspecialchars($e['event_name']) ?></td>
<td><?= htmlspecialchars($e['event_date']) ?></td>
<td><?= htmlspecialchars($e['venue']) ?></td>
<td><?= number_format($e['ticket_price'], 2) ?></td>
<td><?php if ($e['has_seating']): ?><span class="badge badge-neutral">Seated (<?= (int)$e['seat_rows'] ?>&times;<?= (int)$e['seats_per_row'] ?>)</span><?php else: ?><span class="badge badge-neutral">General</span><?php endif; ?></td>
<td><?= (int)$e['tickets_sold'] ?> / <?= (int)$e['total_tickets'] ?></td>
<td>
<a class="btn btn-secondary btn-small" href="event_edit.php?id=<?= (int)$e['id'] ?>">Edit</a>
<form action="event_delete.php" method="post" style="display:inline" onsubmit="return confirm('Delete this event? Any existing orders for it must be removed first.');">
<input type="hidden" name="id" value="<?= (int)$e['id'] ?>">
<button type="submit" class="btn-small btn-danger">Delete</button>
</form>
</td>
</tr>
<?php endforeach; ?>
</table>
<?php require 'partials/footer.php'; ?>
