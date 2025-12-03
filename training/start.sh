#!/bin/bash

# Mail Fine-Tuning App Startup Script

echo "🚀 Starting Mail Fine-Tuning App..."
echo ""

# Check if venv exists
if [ ! -d "venv" ]; then
    echo "❌ Virtual environment not found!"
    echo "Please run: python3 -m venv venv && source venv/bin/activate && pip install -r requirements.txt"
    exit 1
fi

# Activate venv
source venv/bin/activate

# Check if dependencies are installed
if ! python -c "import fastapi" 2>/dev/null; then
    echo "❌ Dependencies not installed!"
    echo "Please run: pip install -r requirements.txt"
    exit 1
fi

# Create necessary directories
mkdir -p data models output

# Start server
echo "✅ Starting server on http://localhost:8000"
echo ""
echo "Press Ctrl+C to stop"
echo ""

cd backend
python main.py
