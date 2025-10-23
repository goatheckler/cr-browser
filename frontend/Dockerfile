# Frontend production Dockerfile (static build served by nginx)

# --- Build stage ---
FROM node:22.21.0-alpine@sha256:bd26af08779f746650d95a2e4d653b0fd3c8030c44284b6b98d701c9b5eb66b9 AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
ENV NODE_ENV=production
RUN npm run build

# --- Runtime stage ---
FROM nginx:1.29-alpine@sha256:61e01287e546aac28a3f56839c136b31f590273f3b41187a36f46f6a03bbfe22 AS runtime
COPY --from=build /app/nginx.conf /etc/nginx/conf.d/default.conf
# Copy SvelteKit static output (adapter-static emits into build/ by default)
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
