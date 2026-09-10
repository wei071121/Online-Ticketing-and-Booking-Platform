<?php
$loggedIn = current_user_id() !== null;
$currentPage = basename($_SERVER['PHP_SELF']);
function nav_active($page, $current) {
    return $page === $current ? ' active' : '';
}
?>
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
<meta name="description" content="<?= htmlspecialchars($pageDescription ?? 'QIGLO Event Ticketing - discover, reserve and enjoy unforgettable live experiences.') ?>">
<title>QIGLO - Event Ticketing</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=Poppins:wght@600;700;800&display=swap" rel="stylesheet">
<link rel="icon" type="image/png" href="assets/qiglo-mark.png">
<link rel="stylesheet" href="style.css?v=<?= @filemtime(__DIR__ . '/../style.css') ?>">
</head>
<body>
<nav class="navbar">
<a class="brand brand-qiglo" href="index.php" aria-label="QIGLO Event Ticketing home">
<img class="brand-qiglo-mark" src="assets/qiglo-mark.png" alt="">
<span class="brand-qiglo-name">QIGLO <small>Event Ticketing</small></span>
</a>
<div class="nav-links">
<a href="events.php" class="<?= trim(nav_active('events.php', $currentPage)) ?>">Browse Events</a>
<a href="<?= $loggedIn ? 'index.php#my-orders' : 'login.php' ?>" class="<?= trim(nav_active('account.php', $currentPage)) ?>">My Tickets</a>
<a href="about.php" class="<?= trim(nav_active('about.php', $currentPage)) ?>">About</a>
<?php if ($loggedIn): ?>
<div class="user-menu">
<button type="button" class="nav-user user-menu-trigger" aria-haspopup="true" aria-expanded="false">
<span class="user-avatar"><?= htmlspecialchars(mb_strtoupper(mb_substr(current_user_name(), 0, 1))) ?></span> Hi, <?= htmlspecialchars(current_user_name()) ?>
</button>
<div class="user-menu-dropdown">
<a href="account.php">My Account</a>
<a href="index.php#my-orders">My Tickets</a>
<a href="schedule.php">Schedule</a>
<a href="testimonials.php">Testimonials</a>
<a href="contact.php">Contact</a>
<a href="logout.php">Logout</a>
</div>
</div>
<?php else: ?>
<a class="nav-signin" href="login.php">Sign In</a>
<a class="nav-cta" href="register.php">Get Started <span aria-hidden="true">→</span></a>
<?php endif; ?>
<button id="theme-toggle" class="theme-toggle" type="button" aria-label="Toggle dark mode">&#9728;</button>
</div>
</nav>
<main class="container">
