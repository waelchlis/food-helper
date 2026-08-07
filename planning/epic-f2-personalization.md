# Epic F2: Personalization — Favorites & Recipe Notes

## Goal

Let signed-in users bookmark recipes they like, and let a recipe's creator/admin attach a public note that's visible to everyone who views the recipe.

## Maps to IMPROVEMENTS.md

- "Favorites / bookmarks" (functional #4)
- "Ratings or personal notes" (functional #5) — **narrowed to notes only; see resolved decisions.**

## Resolved decisions

- **Ratings: dropped entirely.** No star rating, no numeric rating, no aggregate computation. This is out of scope, not deferred within this epic — remove all rating-related work from the plan.
- **Favorites require sign-in.** No anonymous/session-based favoriting — matches how recipe creation already works, and avoids the confusing "favorites vanish when the session expires" UX of the session-based model.
- **Notes are public, not private, and authored by the recipe's creator/admin — not per-viewer.** "Personal notes" turned out to mean the recipe author informing *all* viewers of something (e.g. a substitution tip), not a private per-user annotation. This is functionally close to the existing `Recipe.Tips` field ([Recipe.cs](../backend/FoodHelper.Api/Models/Recipe.cs)) — implementation should add this as a straightforward addition to the existing admin-only recipe edit flow rather than building a new per-user data model/store.

## Current state

- There is no per-user "favorited" data attached to a recipe anywhere in the schema. [Recipe](../backend/FoodHelper.Api/Models/Recipe.cs) is a single shared document with no concept of "this user's relationship to this recipe."
- The only existing per-user-or-session data pattern in the codebase is the shopping list, keyed by `OwnerKeyResolver`'s `user:{sub}` / `session:{sessionId}` scheme ([OwnerKeyResolver.cs](../backend/FoodHelper.Api/Services/OwnerKeyResolver.cs)) — favorites should follow the `user:{sub}`-only half of that pattern (no session-key branch, since sign-in is required).
- [RecipeListComponent](../frontend/src/app/components/recipe-list/recipe-list.ts) and [RecipeDetailComponent](../frontend/src/app/components/recipe-detail/recipe-detail.ts) have no favorite UI at all.
- `Recipe.Tips` already exists as a list of free-text strings shown on the recipe detail page — the new "note" field is conceptually a sibling of this, authored the same way (through the existing `[Authorize(Policy = "AdminOnly")]`-gated `RecipesController.Update` flow).

## Proposed changes

### Backend
- New `Favorite` model (`ownerKey`, `recipeId`, `createdAt`) + `IFavoriteStore`/`FirestoreFavoriteStore`/`InMemoryFavoriteStore` trio, following the existing store pattern (see [Program.cs](../backend/FoodHelper.Api/Program.cs) for how new stores get wired into both branches). Keyed by `(ownerKey, recipeId)`, `ownerKey` always `user:{sub}` (authenticated only — reject anonymous requests outright rather than resolving a session key).
- New `FavoritesController`: `GET /api/favorites` (current user's favorited recipe ids/summaries, `[Authorize]`), `POST /api/favorites/{recipeId}`, `DELETE /api/favorites/{recipeId}` (both `[Authorize]`).
- Notes: add a `Note: string?` field directly to `Recipe` (and `UpsertRecipeRequest`) — no new store, no new endpoint. Displayed on `RecipeDetailComponent` alongside `Tips` when present, editable only through the existing admin recipe-edit form.

### Frontend
- New `FavoriteService` with favorite state (a `Set<string>` of favorited recipe ids, loaded once per session for the signed-in user via `GET /api/favorites`).
- Add a favorite toggle (heart/star icon) to `RecipeCardComponent` and `RecipeDetailComponent`, visible/interactive only when `AuthService.isAuthenticated()`.
- Add a "My Favorites" view — either a new route or a filter toggle on `RecipeListComponent` (reusing F1's combined-filter query mechanism if F1 has landed, or a simple client-side filter over already-loaded favorite ids otherwise).
- Add a note textarea to `RecipeFormComponent` (alongside the existing tips UI) and display it prominently on `RecipeDetailComponent`.

## Task breakdown

1. Add the `Note` field to `Recipe`/`UpsertRecipeRequest`/both stores; add the form field and detail-page display. (Small, no new store — do this first as a quick win.)
2. Add `Favorite` model/store trio + `FavoritesController`; wire into `Program.cs`.
3. Add favorite toggle UI on card and detail views.
4. Add a "My Favorites" list view.

## Dependencies

- Depends on NF4 (consolidated claims/identity accessor) so `FavoritesController` uses the canonical authenticated-subject pattern from the start.
- Benefits from NF1 existing for store contract tests on the new `Favorite` store trio.
- If F1's pagination/query changes land first, the "My Favorites" view should reuse that query mechanism rather than building a separate one.

## Rough sizing

**S–M** — significantly smaller than originally scoped now that ratings are dropped and notes reuse the existing admin recipe-edit flow instead of a new per-user data model. Favorites alone (mirroring the shopping-list ownership pattern, minus the session-key branch) is the only real new moving part.
