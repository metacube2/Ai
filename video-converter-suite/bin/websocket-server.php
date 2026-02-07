#!/usr/bin/env php
<?php

/**
 * Video Converter Suite - WebSocket Server
 *
 * Provides real-time status updates to connected clients.
 * Usage: php bin/websocket-server.php
 */

require_once __DIR__ . '/../vendor/autoload.php';

use Ratchet\Server\IoServer;
use Ratchet\Http\HttpServer;
use Ratchet\WebSocket\WsServer;
use VideoConverter\WebSocket\StatusServer;

$config = require __DIR__ . '/../config/app.php';
$host = $config['websocket']['host'];
$port = $config['websocket']['port'];

echo "=== Video Converter Suite - WebSocket Server ===\n";
echo "Starting on {$host}:{$port}\n\n";

$statusServer = new StatusServer();

$server = IoServer::factory(
    new HttpServer(
        new WsServer($statusServer)
    ),
    $port,
    $host
);

// Broadcast status every 2 seconds
$server->loop->addPeriodicTimer(2, function () use ($statusServer) {
    $statusServer->broadcastStatus();
});

echo "WebSocket server running. Press Ctrl+C to stop.\n";
$server->run();
