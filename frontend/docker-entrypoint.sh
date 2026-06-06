#!/bin/sh
set -e

# Replace env vars in nginx config
envsubst '${PORT} ${API_URL}' < /etc/nginx/conf.d/default.conf > /tmp/default.conf
mv /tmp/default.conf /etc/nginx/conf.d/default.conf

exec nginx -g 'daemon off;'
