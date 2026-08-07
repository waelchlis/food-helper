# Epic F4: Recipe Lifecycle — Edit History & Print/Share

## Goal

Keep a bounded admin-facing audit trail when a recipe is edited, and give users a way to print a recipe or share a public read-only link to it.

## Maps to IMPROVEMENTS.md

- "Recipe edit history" (functional #9)
- "Print/export view" (functional #6)

## Resolved decisions

- **Edit history: admin-only audit trail, not a user-facing feature.** No revert button, no diff UI is required — this is "who changed what, when," visible only to admins, for accountability purposes.
- **Retention: pruned to the last 5 versions.** Older revisions are discarded once a recipe has more than 5 stored revisions, bounding storage growth.
- **Export scope: (a) browser print stylesheet + (c) shareable public read-only link.** PDF generation (option b) is explicitly out of scope.

## Current state

- [RecipesController.Update](../backend/FoodHelper.Api/Controllers/RecipesController.cs) builds a brand-new `Recipe` object and calls `recipes.UpsertAsync`, which in both [FirestoreRecipeStore](../backend/FoodHelper.Api/Services/FirestoreRecipeStore.cs) (`doc.SetAsync`, full overwrite) and [InMemoryRecipeStore](../backend/FoodHelper.Api/Services/InMemoryRecipeStore.cs) (`_recipes[recipe.Id] = recipe`) fully replaces the prior document — there is no history retained anywhere, only the current `CreatorName` and a single `UpdatedAt` timestamp survive.
- There is no print stylesheet, print-specific layout, or share link anywhere in [recipe-detail.html](../frontend/src/app/components/recipe-detail/recipe-detail.html)/[recipe-detail.scss](../frontend/src/app/components/recipe-detail/recipe-detail.scss).
- `GET /api/recipes/{id}` is already `[AllowAnonymous]` ([RecipesController.cs](../backend/FoodHelper.Api/Controllers/RecipesController.cs)) — a shareable link doesn't need new backend read access, just a frontend affordance, since the recipe detail route (`/recipe/:id`) is already reachable without sign-in per [app.routes.ts](../frontend/src/app/app.routes.ts).

## Proposed changes

### Backend
- Add a `RecipeRevision` model (recipe id, full snapshot of the previous state, editor subject/name, timestamp) and a store trio following the existing pattern.
- `RecipesController.Update` writes a `RecipeRevision` snapshot of the recipe's state *before* applying the update, then, if the recipe now has more than 5 stored revisions, deletes the oldest one(s) to enforce the 5-version cap.
- New read endpoint, admin-only: `GET /api/recipes/{id}/history` (list of up to 5 revisions with editor + timestamp + snapshot). No restore/revert endpoint, per the resolved "audit trail only" scope.

### Frontend
- Add a `@media print` stylesheet to `recipe-detail.scss` that hides navigation/buttons and produces a clean, single-column, kitchen-friendly layout; add a "Print" button that calls `window.print()`.
- Add a "Share" button on `RecipeDetailComponent` that copies the recipe's public URL to the clipboard (e.g. via the Clipboard API) — no new backend endpoint needed since the route is already anonymous-accessible.
- Add a "History" view accessible only to admins (gate with the existing `isAdmin` check already used elsewhere, e.g. in [AdminComponent](../frontend/src/app/components/admin/admin.ts)), likely as a tab/section on the recipe edit page, listing the up-to-5 stored revisions with editor and timestamp (read-only display, no diff rendering or restore action).

## Task breakdown

1. Add `RecipeRevision` model/store trio; wire into `Program.cs`.
2. Write revision snapshots (pre-update state) on every `RecipesController.Update`, enforcing the 5-version cap by pruning oldest revisions past that count.
3. Add the admin-only history read endpoint.
4. Add the print stylesheet and print button.
5. Add the share-link button/copy-to-clipboard affordance.
6. Add the admin-only history view UI.

## Dependencies

- Coordinate `Recipe`-adjacent model changes with F3 (also extends the recipe model) to land as one migration where practical (note: `RecipeRevision` is a separate collection/store, not a `Recipe` field change, so the coupling here is looser than with F3's `Image`→`Images` change).
- Benefits from NF1 for store contract tests on the new `RecipeRevision` store trio.

## Rough sizing

**S–M** — meaningfully smaller than originally scoped now that history is audit-trail-only (no diff/revert UI) with a hard 5-version cap, and export drops PDF generation in favor of print CSS + a trivial share-link affordance.
