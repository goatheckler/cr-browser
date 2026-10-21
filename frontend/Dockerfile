# Frontend production Dockerfile (static build served by nginx)

# --- Build stage ---
FROM node:22.23.4-alpine@sha256:ce8d97f7cfc7c5386421eb932df20a29df99b1a7f469f6157d5c9956702ffadc AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
ENV NODE_ENV=production
RUN npm run build

# --- Runtime stage ---
FROM nginx:1.31-alpine@sha256:892bb2152464f0c5d8972593a5f5afaf8054552580b699d95f8ff4af08c2d33d AS runtime
COPY --from=build /app/nginx.conf /etc/nginx/conf.d/default.conf
# Copy SvelteKit static output (adapter-static emits into build/ by default)
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
