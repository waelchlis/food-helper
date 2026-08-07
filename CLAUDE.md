# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

Monorepo with two apps:

- `frontend/` — Angular 21 app (standalone components, Angular Material, Vitest for tests)
- `backend/` — .NET 10 Web API (`FoodHelper.Api`)

## Common commands

Backend uses a project-local SDK at `./.dotnet/dotnet` (see `global.json`) — prefer this over a system-wide `dotnet` to guarantee the right SDK version.

```bash
# Run backend (http://localhost:5000, Swagger at /swagger in Development)
./.dotnet/dotnet run --project backend/FoodHelper.Api

# Build backend
./.dotnet/dotnet build backend/FoodHelper.slnx

# Frontend install / dev server (http://localhost:4200)
cd frontend && npm install && npm start

# Frontend build
cd frontend && npm run build

# Frontend tests (Vitest via Angular CLI)
cd frontend && npm test

# Run both frontend and backend together
./scripts/dev.sh
# or the VS Code task "dev: start all"
```

There is currently no backend test project — only the frontend has `*.spec.ts` tests.

To run a single frontend spec file, pass it through the Angular test builder, e.g.:
```bash
cd frontend && npx ng test --include='**/recipe.spec.ts'
```

## Architecture

### Backend: dual storage with automatic fallback

Every domain store has an `I*Store` interface with two implementations: a `Firestore*Store` (real persistence) and an `InMemory*Store` (fallback). Wiring happens once in [Program.cs](backend/FoodHelper.Api/Program.cs): if `Firebase:ProjectId` is configured and `FirestoreDb.Create` succeeds, all Firestore-backed stores are registered; if it's missing or throws, the app registers the in-memory equivalents for *every* store instead (not a per-store fallback — it's all-or-nothing at startup). Keep this pattern when adding a new domain: define the interface in `Services/`, add both implementations, and register both branches in `Program.cs`.

Firestore models (`Models/*.cs`) are annotated with `[FirestoreData]` / `[FirestoreProperty]`.

### Backend: identity and ownership

- Auth is Google OIDC bearer JWTs validated by `AddJwtBearer` (no local user table/password auth).
- `OwnerKeyResolver` (`Services/OwnerKeyResolver.cs`) computes a per-request owner key: `user:{sub}` for authenticated requests, or `session:{X-Session-Id header}` for anonymous requests (used by the shopping list so guests get a persistent cart without an account). Any new anonymous-friendly, per-user resource should reuse this resolver rather than inventing another identity scheme.
- Admin is a separate authorization layer on top of authentication: `AdminRequirement` + `AdminRequirementHandler` (`Authorization/`) check the JWT subject against `IAdminStore`, exposed as the `"AdminOnly"` policy. Recipe writes (create/update/delete/image upload) require this policy; reads are `[AllowAnonymous]`.
- `AuthController` exposes `/api/auth/me` which the frontend polls after login to learn `isAdmin`.

### Backend: controllers

Thin controllers under `Controllers/`, one per resource, constructor-injected with their store interface(s). Pattern to follow: anonymous GETs, `[Authorize(Policy = "AdminOnly")]` on mutating recipe/category/admin endpoints, `OwnerKeyResolver` + `[AllowAnonymous]` + `X-Session-Id` header for per-user-or-session resources (shopping list, meal plans). Request/response shapes live in `Contracts/` as separate `Upsert*Request` records, not the Firestore models directly.

### Frontend: standalone Angular, signals-based

No NgModules — `app.config.ts` wires `provideHttpClient`, `provideRouter`, and the `authInterceptor` directly. State in services (e.g. `AuthService`) is exposed via Angular `signal`/`computed`, not RxJS subjects, except where async event streams are genuinely needed (e.g. `ReplaySubject` for one-shot "admin status resolved").

- Routes (`app.routes.ts`) are guarded with `authGuard` (must be signed in) and `adminGuard` (must be admin) from `guards/`.
- `AuthService` (`services/auth.ts`) implements Google Identity Services sign-in itself (loads the GSI script, decodes the ID token JWT client-side, stores it in `sessionStorage`) — there is no backend session/cookie. `authInterceptor` (`interceptors/auth.interceptor.ts`) attaches the stored ID token as a Bearer header to requests whose URL starts with `environment.apiBaseUrl`, and only those.
- `environment.apiBaseUrl` is `/api`; in dev, `proxy.conf.json` forwards `/api` to `http://localhost:5000`. In production, Caddy (`Caddyfile`) fronts the frontend and proxies to the backend container — see `docker-compose.yml`.
- Feature UI lives under `components/<feature>/` as one directory per component (`.ts` + `.html` + `.scss`, plus `.spec.ts` where tested).

### Anonymous vs. authenticated data ownership

Recipes are global/shared content (admin-curated). Shopping lists and meal plans are per-owner using the `user:`/`session:` key scheme above — this is why shopping-list endpoints are `[AllowAnonymous]` but still resolve a scoped owner key rather than being truly public.

## Configuration

- Backend settings: `appsettings.json` + environment-specific overlay (`appsettings.Development.json` locally, `appsettings.Production.json` via Docker). Key sections: `Firebase` (project id, credentials path, storage bucket), `GoogleOidc` (authority/audience for JWT validation), `Cors:AllowedOrigins`.
- Firestore credentials: `Firebase:GoogleApplicationCredentialsPath` in appsettings, or the `GOOGLE_APPLICATION_CREDENTIALS` env var.
- Frontend Google OAuth client id / OIDC issuer: `frontend/src/environments/environment.ts`.
