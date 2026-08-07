# Epic NF1: Backend Automated Testing Foundation

## Goal

Give the .NET API an automated test suite, starting with a test project and a set of "contract" tests that run identically against both the Firestore-backed and in-memory implementation of every store interface, so the two never silently drift apart.

## Maps to IMPROVEMENTS.md

- "No backend test project" (non-functional #1)
- "Firestore/in-memory store pairs have no shared contract tests" (non-functional #4)

## Resolved decisions

- **Test framework: xUnit.**
- **Firestore emulator tests: skipped for now.** There's no CI yet ([NF2](epic-nf2-ci-cd-pipeline.md) is deferred), so setting up a Firestore emulator has no runner to execute it regularly and isn't worth the setup cost right now. Contract tests run only against the in-memory implementations for the time being; the same test base should be written so it's trivial to point at a real `FirestoreDb` later (e.g. against an emulator) once CI exists.

## Current state

- [backend/FoodHelper.Api/FoodHelper.Api.csproj](../backend/FoodHelper.Api/FoodHelper.Api.csproj) has no sibling test project; `find` across the repo confirms there is no `*.Tests.csproj` anywhere.
- Every domain has two implementations of its store interface (e.g. `FirestoreRecipeStore` / `InMemoryRecipeStore` for `IRecipeStore`, and the same pattern for shopping list, admin, ingredient words, categories, meal plans) — see [Program.cs](../backend/FoodHelper.Api/Program.cs) for the full wiring. Nothing asserts they behave the same way for a given interface.
- Authorization-sensitive logic (`OwnerKeyResolver`, `AdminRequirementHandler`, `MealPlansController.CanAccess`/`IsOwner`) has zero coverage despite being where a bug would be a security bug.

## Proposed changes

### Backend
- Add `backend/FoodHelper.Api.Tests/FoodHelper.Api.Tests.csproj` (xUnit, referencing the API project) and register it in [food-helper.sln](../food-helper.sln)/[FoodHelper.slnx](../backend/FoodHelper.slnx).
- Unit tests for pure logic with no external dependency: `OwnerKeyResolver.TryResolve` (authenticated vs. anonymous vs. invalid session id), `AdminRequirementHandler`, `MealPlansController`'s `IsOwner`/`CanAccess` helpers.
- A shared abstract test base (e.g. `RecipeStoreContractTests<TStore>`) that exercises `IRecipeStore` behavior (create, get, update, delete, ordering) and is run against `InMemoryRecipeStore` only for now — written generically enough (constructing the store under test via an abstract factory method) that a `FirestoreRecipeStoreContractTests : RecipeStoreContractTests<FirestoreRecipeStore>` subclass can be added later with no changes to the shared base. Repeat the pattern for `IShoppingListStore`, `IMealPlanStore`, `IAdminStore`, `ICategoryStore`, `IIngredientWordStore`.
- Controller-level integration tests using `WebApplicationFactory<Program>` with the in-memory stores wired in, covering the authorization boundaries already present (`[Authorize(Policy = "AdminOnly")]` on recipe/category/ingredient-word writes, `[Authorize]` + ownership checks on meal plans, `X-Session-Id` handling on the shopping list).

### Frontend
- None — this epic is backend-only. (Frontend testing gaps are not called out in IMPROVEMENTS.md beyond what already exists via `*.spec.ts`.)

## Task breakdown

1. Create the test project, wire it into the solution, confirm `./.dotnet/dotnet test` runs (even with zero tests).
2. Write unit tests for `OwnerKeyResolver` and `AdminRequirementHandler`.
3. Design the shared store-contract test base class as a generic abstract class over the store interface; apply it first to `IRecipeStore` against `InMemoryRecipeStore` only.
4. Roll the contract-test pattern out to the remaining five store interfaces, in-memory implementations only.
5. Add `WebApplicationFactory`-based controller tests for the three distinct authorization patterns in the codebase (admin-only, authenticated+ownership, anonymous+session-key).
6. Document how to run tests locally in [CLAUDE.md](../CLAUDE.md) once the project exists (update the "no backend test project" note).
7. Leave a short code comment on the contract-test base pointing at this file, so whoever revisits Firestore emulator coverage later (once [NF2](epic-nf2-ci-cd-pipeline.md) exists) knows the extension point is already there.

## Dependencies

- None — this is the foundation other epics build on. Should land before or alongside NF4, and ideally before any functional epic that adds new controllers/endpoints (F1, F5, F7).

## Rough sizing

**S–M** — narrower than originally scoped now that Firestore-emulator coverage is deferred; mostly mechanical in-memory contract tests plus the auth unit/integration tests.
