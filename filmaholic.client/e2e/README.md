# FilmAholic – Playwright E2E Tests

This directory contains the end-to-end (automation / acceptance) tests for the
**FilmAholic Angular client**, written with [Playwright](https://playwright.dev/).

## Structure

```
e2e/
├── fixtures/
│   └── api-mocks.ts          # Shared mock data & route-interception helpers
├── search-movies.spec.ts     # AC: Search for movies
├── search-actors.spec.ts     # AC: Search for actors
├── favorites.spec.ts         # AC: Add / remove a movie from favorites
└── README.md                 # This file
```

## Prerequisites

| Requirement | Version |
|---|---|
| Node.js | ≥ 18 |
| npm | ≥ 9 |
| .NET SDK | 8 (for ASP.NET dev-cert) |

The Playwright package and browsers are already installed as part of the project
dev dependencies.

## Running E2E tests locally

### 1 – Install dependencies (first time only)

```bash
cd filmaholic.client
npm install
npx playwright install --with-deps chromium
```

### 2 – Start the Angular dev-server

The `playwright.config.ts` includes a `webServer` section that starts the
Angular app automatically before each test run. However, the dev-server uses
the ASP.NET HTTPS dev certificate, which must be set up first.

**Generate the dev certificate (once):**

```bash
# From the repo root
dotnet dev-certs https --trust
```

The Angular start script (`npm run start:default` on Linux/macOS,
`npm run start:windows` on Windows) reads the generated cert from:

| OS | Path |
|---|---|
| Linux / macOS | `$HOME/.aspnet/https/filmaholic.client.pem` |
| Windows | `%APPDATA%\ASP.NET\https\filmaholic.client.pem` |

### 3 – Run the tests

```bash
# From filmaholic.client/

# Headless (default, CI-friendly)
npm run test:e2e

# Headed (browser visible – useful for local debugging)
npm run test:e2e:headed

# Interactive Playwright UI mode
npm run test:e2e:ui
```

### Viewing the HTML report

After a headless run, Playwright generates an HTML report:

```bash
npx playwright show-report
```

## API mocking strategy

All external API calls (TMDB search, local DB, actors, favorites) are
intercepted via Playwright's `page.route()` before the page is loaded.
This means the tests:

- **Do not require a running backend** (FilmAholic.Server).
- **Do not consume API credits** (no live TMDB/OMDB calls).
- Produce **deterministic results** regardless of network conditions.

Mock data lives in `e2e/fixtures/api-mocks.ts`.

## Authentication (favorites tests)

The favorites feature requires an authenticated user. The tests simulate
authentication by pre-seeding `localStorage` with a mock auth token
(`mock-e2e-token`) before the page loads. The `/api/Profile/favorites`
endpoint is fully intercepted.

> **TODO (CI with real backend):** If you want to test against a live backend
> with a real user, replace the `simulateAuthenticatedSession` helper in
> `favorites.spec.ts` with a programmatic login via `/api/auth/login` and
> store the resulting cookie. See the Playwright docs on
> [API testing](https://playwright.dev/docs/api-testing) for guidance.

## CI integration

Set the `CI` environment variable to `1` (most CI systems do this
automatically). When `CI=1` the config:

- **Fails the build** if a test has been accidentally left in `test.only()`.
- **Retries** failed tests up to 2 times.
- Uses the **GitHub Actions reporter** instead of HTML.
- Does **not** reuse an existing dev-server (starts a fresh one each run).

Example GitHub Actions step:

```yaml
- name: Run Playwright E2E tests
  working-directory: filmaholic.client
  run: |
    npx playwright install --with-deps chromium
    npm run test:e2e
  env:
    CI: true
```

## Acceptance criteria traceability

| Test file | AC | Story |
|---|---|---|
| `search-movies.spec.ts` | AC1: Movie cards appear for a search query | US-Search-Movies |
| `search-movies.spec.ts` | AC2: Clicking a movie card opens movie detail | US-Search-Movies |
| `search-actors.spec.ts` | AC1: Actor cards appear for a search query | US-Search-Actors |
| `search-actors.spec.ts` | AC2: Clicking an actor shows their filmography | US-Search-Actors |
| `search-actors.spec.ts` | AC3: Clicking an actor's film opens movie detail | US-Search-Actors |
| `favorites.spec.ts` | AC1: "Favorito" button adds movie to favorites | US-Favorites |
| `favorites.spec.ts` | AC2: Clicking again removes movie from favorites | US-Favorites |
