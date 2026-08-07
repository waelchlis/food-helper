# Epic NF5: Frontend UX & Code Consistency

## Goal

Standardize on one confirmation/error-notification pattern across the app, remove a duplicated constant, and wire up a real production environment configuration.

## Maps to IMPROVEMENTS.md

- "Inconsistent confirmation/error UX" (non-functional #10)
- "Duplicated `timeOptions` constant" (non-functional #11)
- "No environment-specific build config" (non-functional #12)

## Resolved decisions

- **Production environment values: will be provided at deploy time.** The real Google OAuth client id and OIDC audience/issuer values exist and are available, but aren't being handed over as part of planning — implementation should scaffold `environment.prod.ts` with clearly-marked placeholder values (following the same shape as `environment.ts`) and the `fileReplacements` wiring, ready for the real values to be dropped in during deployment prep. Do not fabricate or guess actual client IDs.

## Current state

- Confirmation/notification patterns are split:
  - Material-based: [ShoppingListComponent.clearAll](../frontend/src/app/components/shopping-list/shopping-list.ts) uses `MatDialog` with a purpose-built `ConfirmClearDialogComponent`; [MealPlannerComponent](../frontend/src/app/components/meal-planner/meal-planner.ts) uses `MatSnackBar` for success/error feedback.
  - Native-browser-based: [RecipeFormComponent.saveRecipe](../frontend/src/app/components/recipe-form/recipe-form.ts) uses `alert()` for validation errors and `onImageFileSelected`; [RecipeListComponent.deleteRecipe](../frontend/src/app/components/recipe-list/recipe-list.ts) and [MealPlannerComponent.deletePlan](../frontend/src/app/components/meal-planner/meal-planner.ts) use `confirm()` for delete confirmation (note: `MealPlannerComponent` already imports `MatSnackBar` for other feedback, so it's mixing both patterns in one component).
- `timeOptions` (an identical 5-entry array: 15/30/45/60/90 minutes) is defined separately in [recipe-list.ts](../frontend/src/app/components/recipe-list/recipe-list.ts) and [wheel-of-fortune.ts](../frontend/src/app/components/wheel-of-fortune/wheel-of-fortune.ts).
- [frontend/src/environments/](../frontend/src/environments) contains only `environment.ts`; [angular.json](../frontend/angular.json)'s `production` build configuration has no `fileReplacements` entry, so a production build ships the same file — including the (apparently dev/sample) Google OAuth client ID — as local development.

## Proposed changes

### Frontend
- Introduce a small shared UI service (e.g. `services/confirm-dialog.ts` wrapping `MatDialog` with a generic confirm dialog component, parameterized by title/message/confirm-label) and a convention of using `MatSnackBar` for all success/error toasts.
- Replace every `alert()`/`confirm()` call site (`recipe-form.ts`, `recipe-list.ts`, `meal-planner.ts`) with the shared dialog/snackbar pattern.
- Extract `timeOptions` into a shared constant (e.g. `shared/time-options.ts` or a small constants module) imported by both `recipe-list.ts` and `wheel-of-fortune.ts`.
- Add `frontend/src/environments/environment.prod.ts` with placeholder production values (`googleClientId: 'REPLACE_AT_DEPLOY'`, etc.) and wire `fileReplacements` into `angular.json`'s `production` build configuration so `ng build` (which defaults to the production configuration per `angular.json`) picks it up. Document in [README.md](../README.md) that the real values must be filled in before a production build/deploy.

## Task breakdown

1. Build the shared confirm-dialog service/component, migrate `ShoppingListComponent.clearAll`'s existing `ConfirmClearDialogComponent` to use it (or generalize that component directly) so there's one implementation, not two.
2. Replace `alert()` calls in `recipe-form.ts` with snackbar-based validation feedback.
3. Replace `confirm()` calls in `recipe-list.ts` and `meal-planner.ts` with the shared confirm dialog.
4. Extract and dedupe `timeOptions`.
5. Create `environment.prod.ts` with placeholder values and add `fileReplacements` to `angular.json`; confirm `npm run build` picks up the production file and `npm start` still uses the dev one; add a README note that deploy-time values must replace the placeholders.

## Dependencies

- Land before F2, F4, F6, F7 where practical — those epics add new delete/confirm flows (favorites removal, shopping list clear variants) that should use the shared pattern from day one rather than adding more one-off `confirm()` calls.

## Rough sizing

**S** — straightforward refactors with low risk; the production environment values are a deploy-time handoff, not a blocker to building the scaffolding now.
