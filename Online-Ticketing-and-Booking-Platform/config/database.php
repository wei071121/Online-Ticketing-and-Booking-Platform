<?php

declare(strict_types=1);

/**
 * Returns one MySQL connection for the current request.
 * Credentials are deliberately kept in config/config.php, which Git ignores.
 */
function databaseConnection(): mysqli
{
    static $connection = null;

    if ($connection instanceof mysqli) {
        return $connection;
    }

    $configPath = __DIR__ . '/config.php';

    if (!file_exists($configPath)) {
        http_response_code(500);
        exit('Database configuration is missing. Copy config/config.example.php to config/config.php first.');
    }

    /** @var array{host: string, port: int, database: string, username: string, password: string} $config */
    $config = require $configPath;

    mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);
    $connection = new mysqli(
        $config['host'],
        $config['username'],
        $config['password'],
        $config['database'],
        $config['port']
    );
    $connection->set_charset('utf8mb4');

    return $connection;
}

function escapeHtml(?string $value): string
{
    return htmlspecialchars((string) $value, ENT_QUOTES, 'UTF-8');
}

