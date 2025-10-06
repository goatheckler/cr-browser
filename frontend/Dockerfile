# Frontend production Dockerfile (static build served by nginx)

# --- Build stage ---
FROM node:22.20.0-alpine@sha256:cb3143549582cc5f74f26f0992cdef4a422b22128cb517f94173a5f910fa4ee7 AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ENV NODE_ENV=production
RUN npm run build

# --- Runtime stage ---
FROM nginx:1.29-alpine@sha256:42a516af16b852e33b7682d5ef8acbd5d13fe08fecadc7ed98605ba5e3b26ab8 AS runtime
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
# Copy SvelteKit static output (adapter-static emits into build/ by default)
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
