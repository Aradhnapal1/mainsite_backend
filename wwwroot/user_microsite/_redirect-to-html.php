<?php
$page = basename($_SERVER['SCRIPT_NAME'] ?? 'index.php', '.php');
$target = $page . '.html';
$query = $_SERVER['QUERY_STRING'] ?? '';
$location = $target . ($query !== '' ? '?' . $query : '');
header('Location: ' . $location, true, 302);
exit;
