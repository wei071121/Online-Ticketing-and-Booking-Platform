<?php $currentPage = basename($_SERVER['PHP_SELF']); ?>
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<script>
(function () {
    var saved = localStorage.getItem('theme');
    if (saved === 'dark' || saved === 'light') {
        document.documentElement.setAttribute('data-theme', saved);
    }
})();
</script>
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>QIGLO - Event Ticketing</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=Poppins:wght@600;700;800&display=swap" rel="stylesheet">
<link rel="icon" type="image/png" href="../assets/qiglo-mark.png">
<link rel="stylesheet" href="../style.css?v=<?= @filemtime(__DIR__ . '/../../style.css') ?>">
</head>
<body>
<nav class="navbar">
<a class="brand brand-qiglo" href="events.php" aria-label="QIGLO Event Ticketing admin home">
<img class="brand-qiglo-mark" src="../assets/qiglo-mark.png" alt="">
<span class="brand-qiglo-name">QIGLO <small>Event Ticketing · Admin</small></span>
</a>
<div class="nav-links">
<a href="events.php" class="<?= $currentPage === 'events.php' ? 'active' : '' ?>">Events</a>
<a href="orders.php" class="<?= $currentPage === 'orders.php' ? 'active' : '' ?>">Orders</a>
<a href="checkin.php" class="<?= $currentPage === 'checkin.php' ? 'active' : '' ?>">Check-In</a>
<a href="testimonials.php" class="<?= $currentPage === 'testimonials.php' ? 'active' : '' ?>">Testimonials</a>
<a href="messages.php" class="<?= $currentPage === 'messages.php' ? 'active' : '' ?>">Messages</a>
<a href="users.php" class="<?= $currentPage === 'users.php' ? 'active' : '' ?>">Users</a>
<a href="../logout.php">Logout</a>
<button id="theme-toggle" class="theme-toggle" type="button" aria-label="Toggle dark mode">&#9728;</button>
</div>
</nav>
<main class="container">
