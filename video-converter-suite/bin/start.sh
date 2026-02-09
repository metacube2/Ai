#!/bin/bash

# Video Converter Suite - Startup Script
# Starts all services: Web Server, WebSocket Server, Queue Worker

echo "================================================"
echo "  VIDEO CONVERTER SUITE - Starting Services"
echo "================================================"
echo ""

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$DIR"

# Create storage directories
mkdir -p storage/{uploads,outputs,thumbnails,logs,temp}

# Check FFmpeg
if command -v ffmpeg &> /dev/null; then
    echo "[OK] FFmpeg: $(ffmpeg -version 2>&1 | head -1)"
else
    echo "[!!] FFmpeg not found. Install with: apt install ffmpeg"
    echo "     The application will work but conversions will fail."
fi

# Check PHP
if command -v php &> /dev/null; then
    echo "[OK] PHP: $(php -v 2>&1 | head -1)"
else
    echo "[!!] PHP not found."
    exit 1
fi

# Install dependencies if needed
if [ ! -d "vendor" ]; then
    echo ""
    echo "Installing dependencies..."
    if command -v composer &> /dev/null; then
        composer install
    else
        echo "[!!] Composer not found. WebSocket server won't work."
        echo "     The web interface will still work without it."
    fi
fi

echo ""
echo "Starting services..."
echo ""

# Start Web Server
echo "[1/3] Web Server on http://localhost:8080"
php -S 0.0.0.0:8080 -t public public/router.php \
    -d upload_max_filesize=5G \
    -d post_max_size=5G \
    -d memory_limit=512M \
    -d max_execution_time=3600 \
    > storage/logs/web.log 2>&1 &
WEB_PID=$!

# Start WebSocket Server (optional, requires Ratchet)
if [ -f "vendor/autoload.php" ]; then
    echo "[2/3] WebSocket Server on ws://localhost:8081"
    php bin/websocket-server.php > storage/logs/websocket.log 2>&1 &
    WS_PID=$!
else
    echo "[2/3] WebSocket Server: SKIPPED (run composer install first)"
    WS_PID=""
fi

# Start Queue Worker
echo "[3/3] Queue Worker"
php bin/queue-worker.php > storage/logs/worker.log 2>&1 &
WORKER_PID=$!

echo ""
echo "================================================"
echo "  All services started!"
echo ""
echo "  Web UI:    http://localhost:8080"
echo "  WebSocket: ws://localhost:8081"
echo ""
echo "  PIDs: Web=$WEB_PID WS=$WS_PID Worker=$WORKER_PID"
echo "  Logs: storage/logs/"
echo ""
echo "  Press Ctrl+C to stop all services"
echo "================================================"

# Trap exit to kill all processes
cleanup() {
    echo ""
    echo "Stopping all services..."
    kill $WEB_PID 2>/dev/null
    [ -n "$WS_PID" ] && kill $WS_PID 2>/dev/null
    kill $WORKER_PID 2>/dev/null
    echo "All services stopped."
    exit 0
}

trap cleanup EXIT INT TERM

# Wait for any process to exit
wait
