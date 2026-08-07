# Epic F7: Meal Planner Integration — Generate Shopping List from a Meal Plan

## Goal

Let a user generate shopping list entries in bulk from their meal plan's scheduled recipes over a date range, instead of adding one recipe's ingredients at a time from the recipe detail page.

## Maps to IMPROVEMENTS.md

- "Generate shopping list from a meal plan" (functional #15)
- "Collaborator invitations" (functional #16) — **dropped; see resolved decisions.**

## Resolved decisions

- **Collaborator invitations: dropped entirely.** No outbound email, no invitation flow, no "shared with me" indicator work. `MealPlansController.AddCollaborator` keeps its current silent-grant behavior — out of scope for this epic (and not scheduled elsewhere).
- **`MealEntry` gains a serving count.** Defaults to the recipe's own `Servings` value when a recipe-type entry is added, but the user must be able to override it per entry at add-time (e.g. "planning to make this for 6 people this Tuesday, even though the recipe's base servings is 4").
- **Ingredient merge matching: exact match only**, matching today's `ShoppingListService.addItem` behavior (same name, case-insensitive, and same unit). No fuzzy matching (e.g. "tomato" vs. "tomatoes" stay separate entries) — this is an accepted limitation, not a gap to solve here.

## Current state

- [MealPlansController](../backend/FoodHelper.Api/Controllers/MealPlansController.cs) manages plans and `MealEntry` records (date, `type: "recipe" | "custom"`, optional `recipeId`/`recipeName`/`recipeImage`, optional `customText`) but has no endpoint that touches the shopping list at all, and no serving-count field yet.
- The only existing "add to shopping list" path is [RecipeDetailComponent.addToShoppingList](../frontend/src/app/components/recipe-detail/recipe-detail.ts), which adds one recipe's (serving-scaled) ingredients at a time via [ShoppingListService.addIngredientsFromRecipe](../frontend/src/app/services/shopping-list.ts). `ShoppingListService.addItem` already merges on `(name, unit)` match by summing amounts — the bulk endpoint should replicate this exact-match merge behavior server-side.
- [AddMealEntryRequest](../backend/FoodHelper.Api/Contracts/AddMealEntryRequest.cs) has `Date`, `Type`, `RecipeId`, `RecipeName`, `RecipeImage`, `CustomText` — no `Servings` field.

## Proposed changes

### Backend
- Add `Servings: int?` to `MealEntry` ([MealEntry.cs](../backend/FoodHelper.Api/Models/MealEntry.cs)) and `AddMealEntryRequest`. `MealPlansController.AddEntry` defaults it to the referenced recipe's `Servings` when a recipe-type entry is added and no explicit value is supplied, but accepts an explicit override.
- New endpoint `POST /api/meal-plans/{id}/shopping-list`, accepting a date range (`from`, `to`), which: loads the plan's `MealEntry` records in range, resolves each `recipe`-type entry's ingredients scaled by `entry.Servings / recipe.Servings` (reusing the same scaling math as `RecipeService.scaleIngredients` on the frontend, ported server-side), aggregates/merges quantities across entries by exact `(name, unit)` match (mirroring `ShoppingListService.addItem`'s existing merge behavior), and upserts the merged results into the caller's shopping list via `IShoppingListStore`. `custom`-type entries are skipped (no ingredients to add).
- This endpoint requires `[Authorize]` + the same ownership/collaborator access check already used elsewhere in `MealPlansController` (`CanAccess`), consolidated via NF4 if that's landed by this point.

### Frontend
- Add a serving-count input to the "add meal" flow (likely [AddMealDialogComponent](../frontend/src/app/components/add-meal-dialog/add-meal-dialog.html), not yet inspected in depth but implied by the routes/components list), pre-filled with the selected recipe's own `servings` and editable.
- Add a "Generate Shopping List" action on the meal plan detail view, scoped to a date range (e.g. "this week" or a custom range picker), calling the new bulk endpoint and confirming via a snackbar (per NF5's standardized pattern) how many items were added/merged.

## Task breakdown

1. Add `Servings` to `MealEntry`/`AddMealEntryRequest`; default-and-override logic in `MealPlansController.AddEntry`.
2. Add the serving-count input to the add-meal UI.
3. Port the ingredient-scaling and exact-match merge logic server-side (or extract it into a shared method both the new endpoint and existing logic can call, if a sensible shared location exists).
4. Add the bulk shopping-list-generation endpoint, including the date-range query and `CanAccess` ownership check.
5. Add the "Generate Shopping List" UI action with date-range selection and result confirmation.

## Dependencies

- Depends on NF4 (consolidated identity/claims handling) since this touches the same `MealPlansController` ownership code NF4 refactors.
- Benefits from NF5's standardized confirm/snackbar pattern for the new UI action.
- Benefits from F6 having landed if aisle-adjacent display is ever revisited later, though F6 itself no longer includes aisle grouping, so there's no hard coupling.

## Rough sizing

**M** — substantially smaller than originally scoped now that the collaborator-invitation half (which required introducing an entirely new outbound-email capability) is dropped. What remains is a well-scoped bulk-aggregation endpoint plus a small `MealEntry` model addition.
