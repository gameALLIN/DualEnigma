#!/bin/bash
echo "===================================="
echo " DualEnigma Excel Export Tool v1.0"
echo "===================================="
echo

cd "$(dirname "$0")/.."

if [ -z "$1" ]; then
    echo "Exporting all tables..."
    python src/main.py export
else
    echo "Exporting table: $1"
    python src/main.py export --table "$1"
fi

echo
echo "Done."
