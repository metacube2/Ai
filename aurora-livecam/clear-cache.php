<?php
// Clear PHP OPcache
if (function_exists('opcache_reset')) {
    opcache_reset();
    echo "OPcache cleared successfully!\n";
} else {
    echo "OPcache not available\n";
}

// Clear realpath cache
clearstatcache(true);
echo "Realpath cache cleared!\n";

echo "\nNow reload the page with CTRL+F5 (hard refresh)\n";
?>
