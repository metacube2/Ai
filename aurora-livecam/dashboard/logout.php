<?php
/**
 * Dashboard Logout
 */

require_once dirname(__DIR__) . '/vendor/autoload.php';

if (file_exists(dirname(__DIR__) . '/src/bootstrap.php')) {
    require_once dirname(__DIR__) . '/src/bootstrap.php';
}

use AuroraLivecam\Auth\AuthManager;

$auth = new AuthManager();
$auth->logout();

header('Location: /dashboard/login.php');
exit;
