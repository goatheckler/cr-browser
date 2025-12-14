# Frontend production Dockerfile (static build served by nginx)

# --- Build stage ---
FROM node:22.21.1-alpine@sha256:9632533eda8061fc1e9960cfb3f8762781c07a00ee7317f5dc0e13c05e15166f AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
ENV NODE_ENV=production
RUN npm run build

# --- Runtime stage ---
FROM nginx:1.29-alpine@sha256:052b75ab72f690f33debaa51c7e08d9b969a0447a133eb2b99cc905d9188cb2b AS runtime
COPY --from=build /app/nginx.conf /etc/nginx/conf.d/default.conf
# Copy SvelteKit static output (adapter-static emits into build/ by default)
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
