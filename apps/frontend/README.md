# Konfigo Frontend

[![SvelteKit](https://img.shields.io/badge/SvelteKit-2-FF3E00?logo=svelte&logoColor=white)](package.json)
[![Svelte](https://img.shields.io/badge/Svelte-5-FF3E00?logo=svelte&logoColor=white)](package.json)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?logo=typescript&logoColor=white)](package.json)
[![Vite](https://img.shields.io/badge/Vite-8-646CFF?logo=vite&logoColor=white)](vite.config.ts)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-3.4-06B6D4?logo=tailwindcss&logoColor=white)](tailwind.config.cjs)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](Dockerfile)
[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.0.1-blue)](../../VERSION)

SvelteKit web UI for managing Konfigo services, config versions, and config entries.

## Features

- Browse and search registered services
- View and edit config versions and their entries
- Set config values with optimistic concurrency (generation-guarded)
- Role-aware UI — admin actions hidden for developer-role users
- Real-time Svelte stores backed by the Konfigo REST API

## Development

```bash
npm install
npm run dev        # http://localhost:5173
```

For local end-to-end work, start the backend compose stack and open nginx at
`http://localhost:3000`. It proxies `/` to the Vite dev server and backend paths to
`http://localhost:8080`, keeping browser requests on one origin.

Set relative backend URLs in `.env`:

```env
PUBLIC_API_URL=/api
PUBLIC_SIGNALR_URL=/hubs/config
```

## Production build

```bash
npm run build
npm run preview    # preview the production build locally
```

## Docker

```bash
docker build -t konfigo-frontend .
docker run -p 3000:3000 \
  -e PUBLIC_API_URL=https://your-backend/api \
  -e PUBLIC_SIGNALR_URL=https://your-backend/hubs/config \
  konfigo-frontend
```

The image uses `@sveltejs/adapter-node` and runs as a plain Node.js server on port 3000.

## More Information

- Main repository: https://github.com/maximiliysiss/konfigo
- Full documentation: https://maximiliysiss.github.io/konfigo/
- Deployment guide: https://github.com/maximiliysiss/konfigo/blob/master/docs/deployment.md
- Authentication guide: https://github.com/maximiliysiss/konfigo/blob/master/docs/authentication.md
- Backend API reference: https://github.com/maximiliysiss/konfigo/blob/master/docs/api.md

## Tech stack

| Package | Purpose |
|---------|---------|
| SvelteKit | Framework + routing |
| Tailwind CSS | Utility-first styling |
| TypeScript | Type safety |
