# Epic F1: Recipe Discovery — Search, Filter, Sort, Pagination, Recommendations

## Goal

Move recipe search/filter/sort from "fetch everything, filter in the browser" to a server-side query, add pagination, and surface "similar recipes" on the recipe detail page.

## Maps to IMPROVEMENTS.md

- "Server-side search/filtering" (functional #1)
- "Pagination" (functional #2)
- "Sorting options" (functional #3)
- "'Similar recipes' / recommendations" (functional #12)

## Resolved decisions

- **Filters combine (AND).** Text, ingredient, category, diet type, and max-total-time filters should all be applicable simultaneously, not mutually exclusive as today.
- **Pagination: cursor-based**, matching how Firestore paginates natively (`StartAfter`), not offset-based `page`/`pageSize`.
- **Firestore composite indexes: scripted and documented**, not manually provisioned per environment. A `firestore.indexes.json` gets checked into the repo and deployed via the Firebase CLI (`firebase deploy --only firestore:indexes`) as part of the documented deployment process — not a manual console click-through.
- **"Similar recipes": the simple heuristic is enough** (shared category and/or overlapping ingredients) — no recommendation model needed.

## Current state

- [RecipesController.GetAll](../backend/FoodHelper.Api/Controllers/RecipesController.cs) takes no query parameters; it always returns every recipe.
- [RecipeService](../frontend/src/app/services/recipe.ts) loads the full collection into a signal once (`refreshRecipes`) and does all filtering client-side: `searchRecipes` (substring match on name/description), `filterByIngredient`, `filterByCategory`, `filterByMaxTotalTime` (invoked inline in `RecipeListComponent.filteredRecipes` and `WheelOfFortuneComponent.filteredRecipes`).
- [RecipeListComponent](../frontend/src/app/components/recipe-list/recipe-list.ts) currently applies only *one* of text/ingredient/category at a time (`if query ... else if ingredient ... else if categoryId`) — this changes to combined AND filtering per the resolved decision above.
- There is no sort control in the UI at all; results come back in whatever order `IRecipeStore.GetAllAsync` returns (`updatedAt` descending, per [InMemoryRecipeStore](../backend/FoodHelper.Api/Services/InMemoryRecipeStore.cs) / [FirestoreRecipeStore](../backend/FoodHelper.Api/Services/FirestoreRecipeStore.cs)).
- [RecipeDetailComponent](../frontend/src/app/components/recipe-detail/recipe-detail.ts) has no "similar recipes" section.

## Proposed changes

### Backend
- Extend `RecipesController.GetAll` to accept query parameters: `q` (text search), `ingredient`, `categoryId`, `dietType`, `maxTotalTime`, `sort` (e.g. `newest`, `name`, `totalTime`), `cursor` (opaque, encodes the last-seen document's sort-field value(s) + id for `StartAfter`), `pageSize`.
- Push combined filtering/sorting/paging down into `IRecipeStore` (both implementations): `InMemoryRecipeStore` applies all filters together in LINQ; `FirestoreRecipeStore` composes `Where` clauses for each active filter plus `OrderBy`/`StartAfter`/`Limit` for sort+cursor paging.
- Author `firestore.indexes.json` covering the filter+sort field combinations the UI actually exposes (e.g. `categoryId + dietType + updatedAt`, `categoryId + prepTime+cookTime`, etc.) and document the `firebase deploy --only firestore:indexes` step in [README.md](../README.md)'s deployment instructions.
- Add `GET /api/recipes/{id}/similar`: shared-category and/or overlapping-ingredient heuristic, capped at a small N (e.g. 4–6) results, excluding the recipe itself.

### Frontend
- `RecipeService.refreshRecipes` becomes parameterized (`refreshRecipes(query: RecipeQuery)`) and hits the new query-string endpoint instead of fetching everything; the old client-side `searchRecipes`/`filterByIngredient`/`filterByCategory`/`filterByMaxTotalTime` methods are removed.
- `RecipeListComponent` and `WheelOfFortuneComponent` get updated to debounce search input and call the server with combined filters. The wheel needs the *full filtered set* to spin over (not one page) — it should request a large `pageSize` (or a dedicated "no paging" query mode) rather than iterating cursor pages.
- Add cursor-based pagination UI to `RecipeListComponent` (e.g. "Load more" button advancing the cursor, which is simpler to implement correctly with cursor pagination than a numbered page control).
- Add a sort dropdown to `RecipeListComponent`.
- Add a "Similar recipes" section to `RecipeDetailComponent`, calling the new endpoint.

## Task breakdown

1. Design the combined-filter query parameter contract and the cursor encoding scheme.
2. Implement combined query support in `InMemoryRecipeStore` first (simpler, unblocks frontend work).
3. Implement the same query support in `FirestoreRecipeStore`; author and check in `firestore.indexes.json` for the required composite indexes; document the deploy step.
4. Update `RecipesController.GetAll` to accept and validate the new parameters.
5. Update `RecipeService` and `RecipeListComponent` to use server-side combined filtering + cursor pagination + sorting.
6. Update `WheelOfFortuneComponent` to keep working with its "all matching recipes, unpaginated" requirement.
7. Add the `/similar` endpoint (shared-category/overlapping-ingredient heuristic) and wire it into `RecipeDetailComponent`.

## Dependencies

- Benefits from NF1 (test project) existing so the new store query logic is contract-tested, given this is exactly the kind of behavior that's easy to get subtly wrong in one store and not the other.
- Benefits from NF6 (shared API contracts) landing first so the new query/response DTOs are generated rather than hand-written.

## Rough sizing

**L** — the Firestore combined-filter query composition (and its index requirements) is the biggest piece of work; cursor pagination and the frontend changes are more mechanical once the contract is settled.
