# Epic F5: Admin Bulk Import/Export

## Goal

Let an admin export the full recipe collection to a JSON file and re-import recipes in bulk, instead of only being able to create/edit one recipe at a time through the form.

## Maps to IMPROVEMENTS.md

- "Bulk import/export for admins" (functional #10)

## Resolved decisions

- **File format: JSON only.** No CSV support — matches the nested recipe structure (ingredients, instructions, tips) naturally without a flattening scheme.
- **Conflict resolution: reject on name collision.** An imported recipe whose name matches an existing recipe's name is treated as a conflict and rejected with an error for that item — mirroring the existing conflict-handling convention already used elsewhere in the codebase (e.g. [CategoriesController.Add](../backend/FoodHelper.Api/Controllers/CategoriesController.cs) and [IngredientWordsController.Add](../backend/FoodHelper.Api/Controllers/IngredientWordsController.cs) both return `409 Conflict` for a duplicate name). Import proceeds per-item with per-item success/failure reporting — a name collision on one item doesn't need to abort the whole batch, consistent with the batch already needing partial-failure handling for other validation errors too.
- **Image handling: none.** Bulk import does not handle images at all — imported recipes come in without images (or, if the JSON schema includes an `images` field referencing already-hosted URLs, that's fine to accept, but no upload/zip/file handling is built). Images for imported recipes are added afterward, manually, through the existing per-recipe upload flow.

## Current state

- The only way to create or edit recipes is one at a time through [RecipeFormComponent](../frontend/src/app/components/recipe-form/recipe-form.ts) hitting `POST`/`PUT /api/recipes` ([RecipesController](../backend/FoodHelper.Api/Controllers/RecipesController.cs)).
- [UpsertRecipeRequest](../backend/FoodHelper.Api/Contracts/UpsertRecipeRequest.cs) is the starting point for the import/export schema, though it will reflect F3/F4's model changes (`Images: List<string>` instead of `Image`) by the time this epic is built.
- There's no admin UI location for this today; [AdminComponent](../frontend/src/app/components/admin/admin.ts) currently only manages admins and categories.

## Proposed changes

### Backend
- `GET /api/recipes/export` (`[Authorize(Policy = "AdminOnly")]`) returning the full recipe collection as a JSON array matching the `UpsertRecipeRequest`-shaped fields (plus `id` for reference, though import always creates new records rather than matching by id).
- `POST /api/recipes/import` (`[Authorize(Policy = "AdminOnly")]`) accepting a JSON array of recipes in the export shape. For each item: validate with the existing `UpsertRecipeRequest` validation rules, check for a name collision against existing recipes (case-insensitive, matching the convention used by categories/ingredient words), and either create the recipe or record a per-item rejection reason (`"validation failed"` / `"name already exists"`). Response reports per-item results — not all-or-nothing.
- No image-related logic in the import path — the `images` field (if present in the import JSON) is stored as-is (URLs only, no upload).

### Frontend
- Add an "Import / Export" section to `AdminComponent`: an export button (downloads the JSON via a simple anchor-download of the response), and an import flow (file picker → submit → per-item result table showing which recipes were created vs. rejected and why).

## Task breakdown

1. Add the export endpoint.
2. Add the import endpoint with per-item validation, name-collision rejection, and partial-success response shape.
3. Add the admin UI for both directions, including the per-item result display for import.
4. If F4 (edit history) has landed by this point, decide whether bulk-imported recipes should also write an initial `RecipeRevision` entry — likely yes for consistency, but not required since these are newly-created recipes with no prior state to snapshot.

## Dependencies

- Should come after F3 and F4 settle the final `Recipe` shape (`Images`, etc.), so the import/export schema matches the real model instead of needing a second migration.
- Depends on NF4's rate-limiting work exempting or raising limits for admin-authenticated bulk endpoints, since importing many recipes at once is a legitimate high-volume write pattern that shouldn't be throttled like abusive traffic.
- Benefits from NF6 (shared API contracts) for the import/export DTO shape.

## Rough sizing

**S–M** — smaller than originally scoped now that image handling and update-in-place conflict resolution are both off the table; this is close to straightforward batch-CRUD work with per-item error reporting.
