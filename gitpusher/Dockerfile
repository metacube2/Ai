FROM php:8.3-apache

RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates openssh-client \
    && rm -rf /var/lib/apt/lists/*

RUN a2enmod rewrite headers
RUN printf '%s\n' 'ServerName localhost' > /etc/apache2/conf-available/servername.conf \
    && a2enconf servername

COPY docker/apache-gitpusher.conf /etc/apache2/sites-available/000-default.conf
COPY docker/entrypoint.sh /usr/local/bin/gitpusher-entrypoint

WORKDIR /gitpusher
COPY . /gitpusher

RUN chmod +x /usr/local/bin/gitpusher-entrypoint \
    && mkdir -p /gitpusher/data /deployments \
    && chown -R www-data:www-data /gitpusher/data /deployments

ENTRYPOINT ["gitpusher-entrypoint"]
CMD ["apache2-foreground"]
