# Frontend production Dockerfile (static build served by nginx)

# --- Build stage ---
FROM node:22.22.3-alpine@sha256:968df39aedcea65eeb078fb336ed7191baf48f972b4479711397108be0966920 AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
ENV NODE_ENV=production
RUN npm run build

# --- Runtime stage ---
FROM nginx:1.31-alpine@sha256:7e8ff0a32da368869608f285124b4375b901401d88f5027865d8f88984d35d38 AS runtime
COPY --from=build /app/nginx.conf /etc/nginx/conf.d/default.conf
# Copy SvelteKit static output (adapter-static emits into build/ by default)
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
