# 🍳 Forkfolio

Forkfolio is a personal recipe collection, shopping companion, and dinner decider for a small household or friend group. It's a monorepo with two apps:

- `frontend/`: Angular web app
- `backend/`: .NET 10 Web API

## What it does

- **Browse and manage recipes.** A shared, admin-curated recipe collection with ingredients, step-by-step instructions, tips, a photo gallery, and an optional public note from the recipe's author. Search combines free text, ingredient, category, diet type, and max-total-time filters at once, with sortable, paginated results and a "similar recipes" section on each recipe page.
- **"What should we eat tonight?"** The Wheel of Fortune spins a random recipe out of whatever's currently filtered by ingredient, category, diet type, or time budget.
- **Scale servings.** Any recipe's ingredient amounts scale to a different serving count on the fly.
- **Favorite recipes and share them.** Signed-in users can favorite recipes and filter their list down to favorites only. Every recipe has a one-click "copy share link" button.
- **Shopping list.** Add items manually or straight from a recipe (amounts auto-scale and merge with existing items); check items off while you shop without deleting them. Works for signed-in users and anonymous guests alike (a shopping list tied to a browser session, not an account).
- **Meal planner.** Signed-in users build a monthly calendar of planned meals (existing recipes or free-text entries), invite collaborators by email to co-plan a shared calendar, and generate a shopping list for an entire month in one click — ingredient amounts are pulled from the plan's entries (each with its own overridable serving count) and merged into the shopping list automatically.
- **Admin tools.** Admins manage the recipe catalog, categories, the ingredient-name vocabulary used for autocomplete, and other admins; bulk-export the whole recipe collection to JSON or bulk-import a batch of recipes with per-item validation; and see an audit trail of a recipe's last 5 edits.

## Repository layout

```
backend/    .NET 10 Web API (FoodHelper.Api) + FoodHelper.Api.Tests (xUnit)
frontend/   Angular 21 app (standalone components, Angular Material, Vitest)
scripts/    dev.sh — runs both apps together
```

## Prerequisites

- Node.js + npm
- .NET SDK 10 (project-local SDK is available at `./.dotnet/dotnet`)
- Firebase project with Firestore enabled (optional locally — the backend falls back to in-memory storage if unconfigured)
- Google Cloud OAuth client (Web application)

## Frontend setup

1. Configure Google OAuth values in `frontend/src/environments/environment.ts` (local dev) and `frontend/src/environments/environment.prod.ts` (production build — these are placeholders checked into the repo and must be filled in with real values before deploying; see `angular.json`'s `fileReplacements`).
2. Install dependencies:

```bash
cd frontend
npm install
```

3. Start the Angular dev server:

```bash
npm start
```

The app runs on `http://localhost:4200`.

### Regenerating shared API types

`frontend/src/app/api/generated/api-types.ts` is a checked-in TypeScript snapshot of the backend's request/response shapes, generated from its OpenAPI spec via NSwag. Regenerate it after a backend contract change:

```bash
cd frontend
npm run generate:api-types
```

This reads the checked-in `frontend/openapi/food-helper-api.json` snapshot by default. To regenerate from a live backend instead, update `nswag.json`'s `url` to point at a running instance's `/openapi/v1.json` — **only ever do this against a backend running with an empty `Firebase:ProjectId`** (see the warning in `CLAUDE.md`), never against a Development-configured instance pointed at the real project.

## Backend setup

1. Update placeholders in:
- `backend/FoodHelper.Api/appsettings.Development.json`
- `backend/FoodHelper.Api/appsettings.json`

2. Set Firestore credentials:
- Use `Firebase:GoogleApplicationCredentialsPath` in appsettings, or
- set `GOOGLE_APPLICATION_CREDENTIALS` environment variable.

3. Run backend:

```bash
./.dotnet/dotnet run --project backend/FoodHelper.Api
```

The API runs on `http://localhost:5000`.

Swagger UI is available in development at:

- `http://localhost:5000/swagger`

## Start both apps with one command

From the repository root, run:

```bash
./scripts/dev.sh
```

This starts:

- frontend on `http://localhost:4200`
- backend on `http://localhost:5000`

You can also run the VS Code task `dev: start all`.

## Testing

```bash
# Backend: xUnit — in-memory store contract tests + WebApplicationFactory controller tests
./.dotnet/dotnet test backend/FoodHelper.slnx

# Frontend: Vitest via the Angular CLI
cd frontend && npm test
```

## Implemented API endpoints

### Recipes

- `GET /api/recipes` — combined text/ingredient/category/diet-type/max-time filtering, sorting, and cursor-based pagination via query parameters
- `GET /api/recipes/{id}`
- `GET /api/recipes/{id}/similar` — shared-category/overlapping-ingredient recommendations
- `GET /api/recipes/{id}/history` — last 5 edit revisions (admin only)
- `GET /api/recipes/export` / `POST /api/recipes/import` — bulk JSON export/import (admin only)
- `POST /api/recipes` / `PUT /api/recipes/{id}` / `DELETE /api/recipes/{id}` (admin only)
- `POST /api/recipes/{id}/image` — appends an image to the recipe's gallery (admin only)
- `DELETE /api/recipes/{id}/images` / `PUT /api/recipes/{id}/images/order` — remove/reorder gallery images (admin only)

### Favorites (signed-in users only)

- `GET /api/favorites`
- `POST /api/favorites/{recipeId}` / `DELETE /api/favorites/{recipeId}`

### Shopping list

- `GET /api/shopping-list/items`
- `POST /api/shopping-list/items`
- `PUT /api/shopping-list/items/{itemId}` (including the `checked` / mark-as-purchased flag)
- `DELETE /api/shopping-list/items/{itemId}`
- `DELETE /api/shopping-list/items`

Anonymous requests to shopping list endpoints must include `X-Session-Id`.

### Meal plans (signed-in users only)

- `GET /api/meal-plans` / `GET /api/meal-plans/{id}`
- `POST /api/meal-plans` / `PUT /api/meal-plans/{id}` / `DELETE /api/meal-plans/{id}`
- `GET /api/meal-plans/{id}/entries` / `POST .../entries` / `DELETE .../entries/{entryId}`
- `POST /api/meal-plans/{id}/shopping-list` — generates merged shopping-list items from the plan's recipe entries over a date range
- `POST /api/meal-plans/{id}/collaborators` / `DELETE .../collaborators/{email}`

### Admin, categories, ingredient words

- `GET/POST/DELETE /api/admin` — admin management (admin only for writes)
- `GET/POST/PUT/DELETE /api/categories` (admin only for writes)
- `GET/POST/DELETE /api/ingredient-words` (admin only for writes)

### Health

- `GET /health` — reports `{ status, storageMode, firestoreReachable }`

## Authentication

- Frontend uses Google Identity Services sign-in; the ID token is sent as a bearer JWT.
- Backend validates bearer JWT tokens from Google.
- Write endpoints are rate-limited per authenticated user / session to guard against runaway clients.

## Notes

- If Firestore cannot initialize, the backend falls back to in-memory storage for **every** store (not per-store) — check `GET /health`'s `storageMode` field to see which mode is active.
- Recipe images are soft-deleted (moved to a `trash/` prefix in Storage, purged after 30 days) when replaced or when a recipe is deleted, rather than removed immediately.
- Recipe creation/edit/delete UI is shown only to authenticated admins.
- Shopping list is persisted per browser session for anonymous users and per user (by Google `sub`) when signed in.
- `appsettings.Development.json` points at the real Firebase project — see the warning in `CLAUDE.md` before running the backend locally against it.
