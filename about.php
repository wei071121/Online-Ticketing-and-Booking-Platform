<?php
require 'config.php';
require 'auth.php';

$pageTitle = 'About';
require 'partials/header.php';
?>
<div class="page-header">
<h1>About QIGLO</h1>
<p>What QIGLO Event Ticketing is, and how it works.</p>
</div>

<section>
<h2>Our Mission</h2>
<p>QIGLO Event Ticketing gives organisers and attendees one seamless place to discover,
reserve and manage tickets for memorable events — without relying on manual sign-up sheets
or spreadsheets that run out of seats.</p>
</section>

<section>
<h2>How It Works</h2>
<div class="card-grid">
<div class="card">
<div class="card-icon">&#128197;</div>
<h3>1. Browse Events</h3>
<p>See every upcoming event with its date, venue and live ticket availability.</p>
</div>
<div class="card">
<div class="card-icon">&#127903;</div>
<h3>2. Buy Tickets</h3>
<p>Choose how many tickets you need — the system checks availability in real time.</p>
</div>
<div class="card">
<div class="card-icon">&#9989;</div>
<h3>3. Manage Orders</h3>
<p>Change the quantity or cancel an order anytime from your homepage.</p>
</div>
</div>
</section>

<section>
<h2>Who Runs This</h2>
<p>QIGLO is built to make event discovery and ticket booking feel simple, clear and secure for
every attendee and organiser.</p>
</section>
<?php require 'partials/footer.php'; ?>
