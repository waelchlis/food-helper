# Epic F3: Recipe Content — Image Gallery & Unit Conversion

## Goal

Let a recipe carry more than one photo, and let ingredient amounts convert between compatible units (not just scale by serving count).

## Maps to IMPROVEMENTS.md

- "Multiple images per recipe" (functional #7)
- "Unit conversion" (functional #8)
- "Nutrition info" (functional #11) — **dropped; see resolved decisions.**

## Resolved decisions

- **Nutrition info: dropped entirely.** Not being implemented as part of this (or any) epic for now — remove from scope.
- **Image migration: yes, automatic, no rollback plan.** When `Recipe.Image` becomes `Recipe.Images`, existing recipes' single image becomes the first (and only) entry in the new list via a one-time migration script. No rollback path is being built for this migration — if it needs to be redone, that's handled manually at the time.
- **Unit conversion scope: same-dimension conversions only** (e.g. g↔oz↔kg↔lb for weight, ml↔l↔tsp↔tbsp↔cup for volume). No ingredient-specific density lookups (e.g. "cups of flour to grams") — that's explicitly out of scope as a much bigger data problem than what's being built here.

## Current state

- [Recipe.Image](../backend/FoodHelper.Api/Models/Recipe.cs) is a single nullable string; [FirebaseImageStore.UploadAsync](../backend/FoodHelper.Api/Services/FirebaseImageStore.cs) and `RecipesController.UploadImage` both assume one image per recipe (the upload endpoint is `POST /api/recipes/{id}/image`, singular, replacing the existing one).
- [RecipeService.scaleIngredients](../frontend/src/app/services/recipe.ts) multiplies `Ingredient.amount` by `desiredServings / recipe.servings` — it never touches `unit`, so a recipe written in grams stays in grams.

## Proposed changes

### Backend
- `Recipe.Image` (singular) becomes `Recipe.Images: List<string>`. `UpsertRecipeRequest` gains the corresponding field.
- A one-time migration script (run manually against Firestore, e.g. a small console utility or an admin-only maintenance endpoint invoked once) sets `Images = [Image]` (or `[]` if `Image` was null) for every existing recipe document, then the `Image` field is retired.
- `RecipesController.UploadImage` becomes additive (append to `Images`) rather than replace; add `DELETE /api/recipes/{id}/images/{index-or-token}` to remove one image, and a way to reorder/select a cover image (e.g. `PUT /api/recipes/{id}/images/order` accepting the new ordered list of URLs, with index 0 always treated as the cover).
- Image removal from the gallery reuses NF3's `IImageStore.DeleteAsync` (soft-delete/trash) so gallery cleanup behaves the same as single-image replacement did.
- Unit conversion: this is a display-only concern (the underlying recipe data doesn't change) — implement the conversion table/utility in the **frontend**, not the API. No backend change needed here beyond what F1/elsewhere already does.

### Frontend
- `RecipeFormComponent` gains a multi-image upload/manage UI (thumbnail grid, remove, set-as-cover, reorder) replacing the current single-image picker.
- `RecipeDetailComponent` gains an image gallery/carousel instead of a single `<img>`.
- Add a small unit-conversion utility covering weight (g/kg/oz/lb) and volume (ml/l/tsp/tbsp/cup) conversions, keyed by unit string matching what's already used in `Ingredient.unit`.
- `RecipeDetailComponent`'s serving-scale control gains a unit-system toggle (e.g. "Metric" / "US Customary") that runs scaled amounts through the conversion utility before display for units within the same dimension — ingredients whose unit isn't a recognized weight/volume unit (e.g. "piece", "clove") are left as-is.

## Task breakdown

1. Write and run the one-time `Image` → `Images` migration script against the real data; retire the `Image` field from the model.
2. Update backend model/contracts/both stores to support `Images: List<string>`; update upload/delete/reorder endpoints.
3. Coordinate image removal with NF3's `IImageStore.DeleteAsync` (soft-delete) path.
4. Update `RecipeFormComponent` and `RecipeDetailComponent` for the image gallery.
5. Build the same-dimension unit-conversion utility (weight and volume tables).
6. Wire the unit-system toggle into `RecipeDetailComponent`'s serving-scale UI.

## Dependencies

- Coordinate its `Recipe.Images` model change with F4 (which also extends the recipe model, for edit history) so there's one migration, not two.
- Depends on NF3's `IImageStore.DeleteAsync` for gallery image removal cleanup.
- F5 (bulk import/export) should wait for this epic to settle the final `Recipe` shape — though per F5's resolved decisions, import doesn't handle images at all, which reduces this coupling somewhat.

## Rough sizing

**M** — smaller than originally scoped now that nutrition is dropped and unit conversion is capped at same-dimension-only. The image gallery migration is the main remaining source of care needed (one-time data migration with no rollback).
