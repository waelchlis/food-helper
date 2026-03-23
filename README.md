# Food Helper Monorepo

This repository is split into two apps:

- `frontend/`: Angular web app
- `backend/`: .NET 10 Web API

## Prerequisites

- Node.js + npm
- .NET SDK 10 (project-local SDK is available at `./.dotnet/dotnet`)
- Firebase project with Firestore enabled
- Google Cloud OAuth client (Web application)

## Frontend setup

1. Configure Google OAuth values in `frontend/src/environments/environment.ts`.
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

## Implemented API endpoints

### Recipes

- `GET /api/recipes`
- `GET /api/recipes/{id}`
- `POST /api/recipes` (authenticated)
- `PUT /api/recipes/{id}` (authenticated)
- `DELETE /api/recipes/{id}` (authenticated)

### Shopping list

- `GET /api/shopping-list/items`
- `POST /api/shopping-list/items`
- `PUT /api/shopping-list/items/{itemId}`
- `DELETE /api/shopping-list/items/{itemId}`
- `DELETE /api/shopping-list/items`

Anonymous requests to shopping list endpoints must include `X-Session-Id`.

## Authentication

- Frontend uses Google OIDC Authorization Code flow with PKCE.
- Backend validates bearer JWT tokens from Google.

## Notes

- If Firestore cannot initialize, backend falls back to in-memory storage.
- Recipe creation/edit/delete UI is shown only to authenticated users.
- Shopping list is persisted per browser session for anonymous users and per user (local storage key scoped to Google `sub`) when signed in.
