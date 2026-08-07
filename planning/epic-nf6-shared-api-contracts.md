# Epic NF6: Shared API Contracts / Type Generation

## Goal

Stop hand-maintaining frontend TypeScript interfaces as a parallel, manually-synced copy of the backend's C# `Models`/`Contracts` — generate them from the OpenAPI spec the backend already produces.

## Maps to IMPROVEMENTS.md

- "Hand-duplicated DTO types" (non-functional #13)

## Resolved decisions

- **Generator: NSwag.** Chosen over `openapi-typescript` and `openapi-generator-cli`. NSwag can also generate an HTTP client, not just types — the migration in this epic will need to decide, per service, how much of that generated client replaces the existing hand-written `HttpClient` wrapper logic in files like [recipe.ts](../frontend/src/app/services/recipe.ts) vs. how much stays (e.g. the `RecipeDto`-to-`Recipe` date-parsing adaptation, the `Partial<>` update-payload shaping) — see the updated task breakdown below.
- **Generated output: checked into the repo.** Not regenerated on every build. A `npm run generate:api-types` script regenerates on demand (e.g. after a backend contract change), and the generated file(s) are committed like any other source file.

## Current state

- The backend already exposes an OpenAPI document in Development via `builder.Services.AddOpenApi()` / `app.MapOpenApi()` and Swagger UI at `/swagger` ([Program.cs](../backend/FoodHelper.Api/Program.cs)).
- The frontend independently declares matching shapes by hand, e.g. `Recipe`/`Ingredient` in [recipe.ts](../frontend/src/app/services/recipe.ts), `MealPlan` in [meal-plan.ts](../frontend/src/app/services/meal-plan.ts), `ShoppingListItem` in [shopping-list.ts](../frontend/src/app/services/shopping-list.ts) — each maintained separately from the C# `Recipe`, `MealPlan`, `ShoppingListItem` models and their `Upsert*Request` contracts.
- There's already at least one manual DTO-adaptation layer per service (e.g. `RecipeDto` in `recipe.ts` converting `createdAt`/`updatedAt` strings to `Date`), which needs to be preserved even after generation since generated types will still express dates as `string`.

## Proposed changes

### Backend
- No behavior changes; ensure the OpenAPI document is complete and accurate (mostly a verification pass, since `Microsoft.AspNetCore.OpenApi` infers the spec from controller action signatures).
- `MapOpenApi()` stays gated to `IsDevelopment()` — the checked-in-output decision means the spec only needs to be available from a local dev instance at generation time, not in production.

### Frontend
- Add NSwag tooling (`nswag`/`NSwag.MSBuild` or the `nswag` npm CLI — pick whichever integrates more simply with the existing npm-script-driven workflow) and an `nswag.json` config pointing at the local dev backend's OpenAPI JSON.
- Generate **types only** (not a full generated HTTP client) to start, keeping the existing hand-written service methods but backing their signatures with generated interfaces instead of hand-written ones — this is the lower-risk migration path and preserves all the existing adaptation logic unchanged. Revisit generating a full client later if the types-only approach proves the tooling out.
- Add `npm run generate:api-types` to `frontend/package.json`, and commit the generated output (e.g. `frontend/src/app/api/generated/`) to the repo.
- Replace hand-written interfaces in `services/recipe.ts`, `services/meal-plan.ts`, `services/shopping-list.ts`, `services/category.ts`, `services/ingredient-word.ts`, `services/admin.ts` with imports from the generated types.

## Task breakdown

1. Verify the current OpenAPI output is accurate for all controllers (spot-check via `/swagger` locally).
2. Set up NSwag with a types-only generation config; add the npm script; commit the first generated output.
3. Migrate one service (`recipe.ts`, since it's the most-used) to the generated types as a proof of concept, keeping its existing `RecipeDto`-to-`Recipe` date-parsing wrapper.
4. Migrate the remaining services.
5. Document the regeneration workflow in [CLAUDE.md](../CLAUDE.md) (when to re-run `generate:api-types`, and that output is checked in so a stale generation is a normal PR diff, not a build failure).

## Dependencies

- Best done before F1/F3/F4/F5/F7 add new request/response DTOs, so those epics can build directly on generated types instead of adding more hand-written ones that would need retrofitting later. If timing doesn't allow going first, it can be retrofitted afterward at the cost of one extra migration pass.

## Rough sizing

**M–L** — narrower than a full-client migration since types-only was chosen, but still touches every frontend service file and requires care to preserve existing runtime adaptation logic during migration.
