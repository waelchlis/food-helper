# Food Helper — Improvement List

Analysis of the current codebase (Angular 21 frontend + .NET 10 backend, Firestore/in-memory dual storage). Items are grounded in specific files and observed gaps, not generic advice. Nothing here has been implemented — this is a planning document only.

---

## Functional improvements (features for end users)

### Recipes
1. **Server-side search/filtering.** [RecipesController](backend/FoodHelper.Api/Controllers/RecipesController.cs) has no query parameters — `GET /api/recipes` always returns the full collection, and [RecipeService](frontend/src/app/services/recipe.ts) does all searching/filtering client-side over the full in-memory array. This won't scale past a small recipe count and prevents combining filters efficiently (e.g. text + ingredient + category in one pass server-side).
2. **Pagination.** No paging anywhere in the recipe list — the whole collection is fetched and rendered at once.
3. **Sorting options.** [RecipeListComponent](frontend/src/app/components/recipe-list/recipe-list.ts) has no sort control (newest, alphabetical, prep time, etc.) — results come back in whatever order the store returns (`updatedAt` descending only).
4. **Favorites / bookmarks.** No way for a signed-in user to save/star recipes for quick access.
5. **Ratings or personal notes.** No rating, review, or "made this" tracking on a recipe.
6. **Print/export view.** No print-friendly layout, PDF export, or shareable read-only link for a recipe — awkward for actual kitchen use.
7. **Multiple images per recipe.** `Recipe.Image` ([Recipe.cs](backend/FoodHelper.Api/Models/Recipe.cs)) is a single field; no gallery/step photos.
8. **Unit conversion.** [RecipeService.scaleIngredients](frontend/src/app/services/recipe.ts) scales the *amount* by serving ratio but never converts *units* (e.g. g ↔ oz, ml ↔ cup) — scaling only works within the recipe's original unit system.
9. **Recipe edit history.** Updates overwrite the document in place ([RecipesController.Update](backend/FoodHelper.Api/Controllers/RecipesController.cs)) with no revision history or "last edited by" trail beyond a single `CreatorName`/`UpdatedAt`.
10. **Bulk import/export for admins.** No CSV/JSON import or export path for migrating or backing up the recipe collection — every recipe must be entered by hand through the form.
11. **Nutrition info.** No calories/macros field on recipes.
12. **"Similar recipes" / recommendations.** Recipe detail page has no related-recipe suggestions.

### Shopping list
13. **Mark items as purchased.** [ShoppingListItem](backend/FoodHelper.Api/Models/ShoppingListItem.cs) has no `checked`/`purchased` flag — items can only be added, edited, or deleted, so there's no way to tick things off while actually shopping without removing them.
14. **Group by category/aisle.** The app already maintains a controlled ingredient vocabulary ([IngredientWordsController](backend/FoodHelper.Api/Controllers/IngredientWordsController.cs)) but shopping list items aren't grouped by category/aisle for a more usable in-store view.

### Meal planner
15. **Generate shopping list from a meal plan.** There's no bulk "add all ingredients for this week's planned recipes to the shopping list" action — ingredients can currently only be added one recipe at a time from the recipe detail page ([RecipeDetailComponent.addToShoppingList](frontend/src/app/components/recipe-detail/recipe-detail.ts)), with no bridge from [MealPlansController](backend/FoodHelper.Api/Controllers/MealPlansController.cs) entries.
16. **Collaborator invitations.** [MealPlansController.AddCollaborator](backend/FoodHelper.Api/Controllers/MealPlansController.cs) grants access by email silently — no invite email, no notification, and no "shared with me" indicator surfaced to the invited user until they happen to open the plan.

---

## Non-functional improvements (code quality, structure, internal wiring)

### Testing & CI
1. **No backend test project.** Only the Angular side has `*.spec.ts` files; there is no `.csproj` test project for the API at all, so ownership resolution (`OwnerKeyResolver`), the admin authorization handler, and every controller are completely untested.
2. **No CI pipeline.** There is no `.github/workflows` directory (or any other CI config) — nothing builds, lints, or runs tests automatically on push/PR.

### Backend architecture
3. **Duplicated claims-resolution logic.** The "get subject/email from the JWT" logic is re-implemented separately in [OwnerKeyResolver](backend/FoodHelper.Api/Services/OwnerKeyResolver.cs), [MealPlansController.TryResolveUser](backend/FoodHelper.Api/Controllers/MealPlansController.cs), [AdminRequirementHandler](backend/FoodHelper.Api/Authorization/AdminRequirementHandler.cs), and [RecipesController.ResolveCreatorName](backend/FoodHelper.Api/Controllers/RecipesController.cs), each with slightly different claim-type fallback order. Worth consolidating into one `ClaimsPrincipal` extension/service to avoid drift.
4. **Firestore/in-memory store pairs have no shared contract tests.** Every store interface (`IRecipeStore`, `IShoppingListStore`, `IMealPlanStore`, etc.) has a Firestore and an in-memory implementation that must behave identically, but nothing verifies that — a behavior change in one is easy to miss in the other.
5. **Startup fallback to in-memory storage is all-or-nothing and silent.** In [Program.cs](backend/FoodHelper.Api/Program.cs), if `FirestoreDb.Create` throws for *any* reason, every store (recipes, shopping lists, admins, meal plans, categories, ingredient words) silently falls back to in-memory, and the only signal is a `Console.WriteLine`. In production this means a Firestore misconfiguration quietly turns into full data loss on every restart, with no structured log (`ILogger`) or startup failure to alert on.
6. **`/health` doesn't reflect real dependency health.** The health endpoint always returns `{ status: "ok" }` regardless of whether Firestore is reachable — not useful for orchestration/monitoring given issue #5.
7. **Orphaned images in Firebase Storage.** [FirebaseImageStore.UploadAsync](backend/FoodHelper.Api/Services/FirebaseImageStore.cs) uploads a new object on every image change but nothing ever deletes the previous image when a recipe's image is replaced, nor when the recipe itself is deleted ([RecipesController.Delete](backend/FoodHelper.Api/Controllers/RecipesController.cs) only removes the Firestore document) — storage cost grows unbounded.
8. **No rate limiting.** Write endpoints (recipe create/update, shopping list add, meal plan create) have no throttling, so a script (or a bug in a client) can spam them freely.
9. **Console-based logging instead of `ILogger`.** Several places (`Program.cs`'s Firestore fallback, ingredient seeding) use `Console.WriteLine` for error reporting instead of the built-in `ILogger` abstraction that's already available via DI.

### Frontend architecture
10. **Inconsistent confirmation/error UX.** Some flows use Angular Material dialogs/snackbars (shopping list clear-all, meal planner create/delete), while others use raw browser `alert()`/`confirm()` ([RecipeFormComponent.saveRecipe](frontend/src/app/components/recipe-form/recipe-form.ts) validation, [RecipeListComponent.deleteRecipe](frontend/src/app/components/recipe-list/recipe-list.ts), [MealPlannerComponent.deletePlan](frontend/src/app/components/meal-planner/meal-planner.ts)). Worth standardizing on one pattern — it's also more testable and themeable than native dialogs.
11. **Duplicated `timeOptions` constant.** The identical 5-entry prep/cook time filter array is defined independently in [recipe-list.ts](frontend/src/app/components/recipe-list/recipe-list.ts) and [wheel-of-fortune.ts](frontend/src/app/components/wheel-of-fortune/wheel-of-fortune.ts) — a shared constant would prevent them drifting apart.
12. **No environment-specific build config.** [angular.json](frontend/angular.json) defines `production`/`development` build configurations but there's no `fileReplacements` entry and only a single `environment.ts` — production builds currently ship the same `environment.ts` (including the dev Google client ID) as local dev rather than swapping in a prod `environment.prod.ts`.
13. **Hand-duplicated DTO types.** Frontend TypeScript interfaces (`Recipe`, `Ingredient`, `MealPlan`, etc. in `services/*.ts`) are maintained by hand in parallel with the backend's `Models`/`Contracts` C# classes, with no shared schema (e.g. generated from the OpenAPI spec the backend already exposes via Swagger) — easy for the two to silently drift.
