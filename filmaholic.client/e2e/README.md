# FilmAholic – Playwright E2E Tests

This directory contains **end-to-end (E2E) / acceptance tests** for the FilmAholic Angular client, implemented with [Playwright](https://playwright.dev/).

## Test scenarios

| File | Scenario |
|------|----------|
| `auth.spec.ts` | Unauthenticated redirect + login flow |
| `search-movies.spec.ts` | Search for movies (e.g. "Shrek") |
| `search-actors.spec.ts` | Search for actors (e.g. "Brad Pitt") |
| `favorites.spec.ts` | Add / remove a movie from favourites |

All tests **mock** the backend API so they run fully offline and are not affected by real TMDB/OMDB availability.

## Prerequisites

- **Node.js ≥ 18** and the project's `npm install` already run.
- Playwright browsers installed:

  ```bash
  cd filmaholic.client
  npx playwright install --with-deps chromium
  ```

## Running tests

All commands must be run from the `filmaholic.client/` directory.

### Headless (CI-friendly, default)

```bash
npm run test:e2e
```

This command:
1. Starts the Angular dev server on `http://127.0.0.1:50905` (plain HTTP, no SSL needed).
2. Runs all tests in `e2e/` headlessly.
3. Shuts the dev server down when tests finish.

### Interactive Playwright UI

```bash
npm run test:e2e:ui
```

Opens the Playwright UI where you can pick individual tests, view traces, and step through them.

### Headed (visible browser)

```bash
npm run test:e2e:headed
```

Runs tests with a visible Chromium window — useful for debugging.

## HTML report

After a headless run, open the generated report:

```bash
npx playwright show-report
```

## Architecture

```
e2e/
├── helpers/
│   └── auth.ts        ← simulateLogin(), mockBackendApis(), mockLoginApis()
│                         and JSON fixtures for stubbed API responses
├── auth.spec.ts
├── search-movies.spec.ts
├── search-actors.spec.ts
└── favorites.spec.ts
```

### How authentication is handled

Because every page requires a logged-in user, tests call `simulateLogin(page)` in `beforeEach`. This injects the `user_id`, `user_nome`, and `userName` values into `localStorage` via Playwright's `addInitScript` hook — simulating what the real login flow does — without needing a running backend.

### How API calls are mocked

`mockBackendApis(page)` registers Playwright `route()` intercepts for every backend endpoint that the tested pages call (search, favourites, movie-detail, ratings, cast, …). Each intercept returns a small JSON fixture so tests are fully deterministic.

## Adding new tests

1. Create a `.spec.ts` file in `e2e/`.
2. Import `simulateLogin` and `mockBackendApis` from `./helpers/auth`.
3. Add any additional `page.route()` mocks specific to your scenario.
4. Run `npm run test:e2e` to verify.
