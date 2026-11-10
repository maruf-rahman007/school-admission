# ADR-0003: Frontend backend URL as a build argument

- **Status:** Accepted (2026-11-10)

## Context
The spec requires the frontend's backend URL to be changeable through an environment variable. Vite replaces `import.meta.env.VITE_*` values at **build** time, so a plain runtime environment variable on the container has no effect on the built JavaScript.

## Options considered
1. **Build arg.** Compose passes `VITE_API_BASE_URL` (from an environment variable, default `http://localhost:8080`) as a Docker build arg. Standard Vite behaviour and nothing extra to maintain, but changing the URL needs a rebuild.
2. **Runtime config file.** The nginx entrypoint writes `config.js` from an environment variable at container start. No rebuild needed, but it adds an entrypoint script and a global config object.
3. **Reverse proxy.** nginx proxies `/api` to the backend, so the frontend uses a relative URL. Avoids CORS, but the browser no longer talks to `:8080` the way the spec describes.

## Decision
Use option 1. The browser calls the backend directly, and the backend enables CORS for `CORS_ALLOWED_ORIGINS` (default `http://localhost:3000`). It also exposes the `X-Server-Time` header.

## Consequences
- After changing `VITE_API_BASE_URL`, run `docker compose up --build`. The README documents this.
- The backend must keep its CORS settings in step with the frontend's origin.
