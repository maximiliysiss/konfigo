# Konfigo Frontend

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

Set the backend URL in `.env`:

```env
PUBLIC_API_URL=http://localhost:8080
```

## Production build

```bash
npm run build
npm run preview    # preview the production build locally
```

## Docker

```bash
docker build -t konfigo-frontend .
docker run -p 3000:3000 -e PUBLIC_API_URL=https://your-backend konfigo-frontend
```

The image uses `@sveltejs/adapter-node` and runs as a plain Node.js server on port 3000.

## Tech stack

| Package | Purpose |
|---------|---------|
| SvelteKit | Framework + routing |
| Tailwind CSS | Utility-first styling |
| TypeScript | Type safety |
