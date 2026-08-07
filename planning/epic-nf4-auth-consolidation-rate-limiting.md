# Epic NF4: Auth Consolidation & API Hardening

## Goal

Consolidate the repeated "resolve subject/email from JWT claims" logic into one place, and add rate limiting to write endpoints so the API isn't trivially spammable.

## Maps to IMPROVEMENTS.md

- "Duplicated claims-resolution logic" (non-functional #3)
- "No rate limiting" (non-functional #8)

## Resolved decisions

- **Rate-limit key: per session.** The app has under 50 users total, so raw volume is low — the goal is basic abuse protection, not capacity management. Limiting per `X-Session-Id` (for anonymous/session-scoped resources) and, for authenticated requests, per the resolved owner key (`user:{sub}`) covers the identity concepts already in play without introducing IP-based limiting.
- **Thresholds: generous, since expected volume is small.** No specific number was mandated — pick a conservative-but-non-restrictive limit (e.g. on the order of tens of write requests per minute per key) that a real user session won't realistically hit during normal use (including bulk actions like adding several shopping-list items in a row), but that blocks a runaway script or bug.
- **Claims fallback verification: not required.** No claims are currently relied upon in a way that verification against a captured token payload would protect against — the consolidation can proceed as a straightforward refactor without an extra validation pass.

## Current state

- The same "read `sub`, falling back to `ClaimTypes.NameIdentifier`, falling back to `nameidentifier`" logic (and a parallel "read `email`" pattern) is implemented independently in:
  - [OwnerKeyResolver.TryResolve](../backend/FoodHelper.Api/Services/OwnerKeyResolver.cs)
  - [MealPlansController.TryResolveUser](../backend/FoodHelper.Api/Controllers/MealPlansController.cs)
  - [AdminRequirementHandler.HandleRequirementAsync](../backend/FoodHelper.Api/Authorization/AdminRequirementHandler.cs) (subject only)
  - [RecipesController.ResolveCreatorName](../backend/FoodHelper.Api/Controllers/RecipesController.cs) (name claim, different fallback chain)
  - [AdminController.Remove](../backend/FoodHelper.Api/Controllers/AdminController.cs) (subject only, its own two-claim fallback)
  Each has a slightly different fallback order, which is exactly the kind of thing that silently diverges over time.
- No rate limiting middleware is registered in [Program.cs](../backend/FoodHelper.Api/Program.cs); write endpoints (recipe create/update/image-upload, shopping list add, meal plan create/entries/collaborators, admin add) have no request throttling.

## Proposed changes

### Backend
- Add a `ClaimsPrincipalExtensions` (or `CurrentUserAccessor` service) in `Services/` exposing `TryGetSubject(out string subject)`, `GetEmail()`, and `GetDisplayName()` with one canonical fallback chain each, replacing all five call sites above.
- `OwnerKeyResolver.TryResolve` becomes a thin wrapper that calls the new subject accessor rather than reimplementing the claim lookup.
- Adopt ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware (available in .NET 10, no extra package needed). Define policies keyed by:
  - The resolved owner key (`user:{sub}` or `session:{sessionId}`, i.e. the exact same identity `OwnerKeyResolver` already computes) for write endpoints — recipe/category/ingredient-word mutations, shopping list writes, meal plan writes, admin writes.
  - No separate policy needed for reads, given the low expected volume.
- Apply policies via `[EnableRateLimiting("writes")]` on the relevant controllers/actions.

### Frontend
- None required, though the frontend should handle `429 Too Many Requests` gracefully wherever it doesn't already handle non-2xx responses (mostly already covered by existing `catchError` usage in services like [recipe.ts](../frontend/src/app/services/recipe.ts) and [shopping-list.ts](../frontend/src/app/services/shopping-list.ts), but worth a pass to confirm user-facing messaging distinguishes "rate limited, try again shortly" from a generic failure).

## Task breakdown

1. Introduce the consolidated claims-accessor abstraction; write unit tests for it (via NF1's test project).
2. Replace the five duplicated call sites one at a time, reconciling their fallback chains into the single canonical one (no separate token-payload verification pass needed per the resolved decision above).
3. Configure a single `"writes"` rate-limiting policy keyed on owner key (session or user), with a generous threshold appropriate for the app's small user base.
4. Apply `[EnableRateLimiting("writes")]` to write endpoints across `RecipesController`, `CategoriesController`, `IngredientWordsController`, `ShoppingListController`, `MealPlansController`, `AdminController`.
5. Add a friendly 429 response body (consistent with the existing `{ error }` shape used elsewhere) and confirm the frontend surfaces it reasonably.

## Dependencies

- Should land early since F2, F5, and F7 all add new authenticated/owned endpoints that should use the consolidated accessor from the start rather than adding a sixth duplicate.
- Rate-limiting benefits from NF3's logging work landing first, so throttled requests are visible in logs.

## Rough sizing

**S–M** — the claims consolidation is a low-risk mechanical refactor with no verification overhead now that no external validation pass is required; a single session/user-keyed rate-limit policy is simpler than originally scoped (no per-IP tier needed).
