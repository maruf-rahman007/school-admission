# Frontend image. Build from the repo root: docker build -t school-admission-frontend ./frontend

# --- Build: install from the lockfile and bundle with Vite ---
FROM node:22-alpine AS build
WORKDIR /app

# Install first, from the manifests only, so the layer is cached until a dependency changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Vite bakes this into the JavaScript at build time (ADR-0003): change it, then `docker compose up --build`.
ARG VITE_API_BASE_URL=http://localhost:8080
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# --- Runtime: static files served by nginx ---
FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
