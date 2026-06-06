# ── Build stage ──
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build --configuration=production

# ── Serve stage ──
FROM nginx:alpine AS runtime

# Copy built Angular app
COPY --from=build /app/dist/hapag-portal/browser /usr/share/nginx/html

# Nginx config for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Railway injects PORT env var; nginx needs it in config
# Use envsubst to replace $PORT in nginx config at runtime
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["/docker-entrypoint.sh"]
