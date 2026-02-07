FROM php:8.2-cli

# Install FFmpeg and dependencies
RUN apt-get update && apt-get install -y \
    ffmpeg \
    libzip-dev \
    unzip \
    git \
    && docker-php-ext-install pcntl posix sockets \
    && rm -rf /var/lib/apt/lists/*

# Install Composer
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer

WORKDIR /app

# Copy composer files first for caching
COPY composer.json ./
RUN composer install --no-dev --optimize-autoloader 2>/dev/null || true

# Copy application
COPY . .

# Install dependencies
RUN composer install --no-dev --optimize-autoloader

# Create storage directories
RUN mkdir -p storage/uploads storage/outputs storage/thumbnails storage/logs storage/temp \
    && chmod -R 777 storage

# Configure PHP
RUN echo "upload_max_filesize = 5G\n\
post_max_size = 5G\n\
memory_limit = 512M\n\
max_execution_time = 3600\n\
max_input_time = 3600" > /usr/local/etc/php/conf.d/video-converter.ini

EXPOSE 8080 8081

CMD ["php", "-S", "0.0.0.0:8080", "-t", "public", "public/router.php"]
