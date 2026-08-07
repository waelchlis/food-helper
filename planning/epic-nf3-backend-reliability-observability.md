# Epic NF3: Backend Reliability & Observability

## Goal

Replace silent failure modes with visible, logged, and (where appropriate) alertable ones: the Firestore-vs-in-memory fallback, the `/health` endpoint, and orphaned image cleanup in Firebase Storage.

## Maps to IMPROVEMENTS.md

- "Startup fallback to in-memory storage is all-or-nothing and silent" (non-functional #5)
- "`/health` doesn't reflect real dependency health" (non-functional #6)
- "Orphaned images in Firebase Storage" (non-functional #7)
- "Console-based logging instead of `ILogger`" (non-functional #9)

## Resolved decisions

- **`/health` shape: one enriched response, no liveness/readiness split.** Nothing consumes this endpoint today (no uptime monitor, no orchestrator probes), so the added complexity of splitting `/health/live` vs. `/health/ready` isn't justified. A single `GET /health` returning `{ status, storageMode, firestoreReachable }` is sufficient.
- **Image deletion: soft-delete.** Replaced/deleted recipe images move to a `trash/` prefix in the bucket rather than being hard-deleted immediately, guarding against accidental overwrite/rollback. A specific retention window wasn't specified — this plan assumes **30 days** as a reasonable default grace period before a cleanup job purges `trash/` objects; adjust if a different window is wanted.

## Current state

- [Program.cs](../backend/FoodHelper.Api/Program.cs): if `FirestoreDb.Create(firebase.ProjectId)` throws, the `catch` block registers in-memory stores for *every* domain and reports the failure via `Console.WriteLine` only — no `ILogger`, no startup failure, no way for an operator to notice short of reading raw stdout.
- `/health` ([Program.cs](../backend/FoodHelper.Api/Program.cs)) is `app.MapGet("/health", () => Results.Ok(new { status = "ok" }))` — a static response, independent of whether Firestore/Storage are actually reachable.
- [FirebaseImageStore.UploadAsync](../backend/FoodHelper.Api/Services/FirebaseImageStore.cs) uploads a new object under `recipe-images/{recipeId}/{token}.{ext}` on every call but never deletes the object it's replacing. [RecipesController.Delete](../backend/FoodHelper.Api/Controllers/RecipesController.cs) removes only the Firestore document, not the associated Storage object(s).
- Ingredient seeding failures ([Program.cs](../backend/FoodHelper.Api/Program.cs)) also use `Console.WriteLine`.

## Proposed changes

### Backend
- Replace `Console.WriteLine` calls in `Program.cs` with `ILogger` (obtainable via `app.Services.GetRequiredService<ILogger<Program>>()` or a dedicated startup logger category), at `LogError`/`LogWarning` severity as appropriate.
- Add a `StorageMode` (or similar) singleton set at startup (`Firestore` vs. `InMemory`) so downstream code — notably `/health` — can report which mode is active.
- Extend `/health` into a single enriched response: `{ status, storageMode, firestoreReachable }`, pinging Firestore with a cheap read (e.g. a sentinel doc `GetSnapshotAsync`) when `storageMode == Firestore`.
- `IImageStore` gets a `DeleteAsync(string imageUrl, CancellationToken)` method. In `FirebaseImageStore`, this **moves** the object to a `trash/{original-object-path}` prefix (copy + delete original, or `StorageClient.CopyObjectAsync` + `DeleteObjectAsync`) rather than deleting it outright. `InMemoryImageStore` treats this as a no-op.
- Add a scheduled cleanup path for `trash/` objects older than the retention window (30 days by default) — implemented as a `BackgroundService`/`IHostedService` that runs periodically (e.g. daily) and deletes trash objects past their grace period, using each object's stored creation timestamp (Google Cloud Storage objects already carry `TimeCreated` metadata, so no extra bookkeeping is needed).
- `RecipesController.UploadImage` calls `imageStore.DeleteAsync` (i.e. soft-delete/move-to-trash) on the recipe's previous `Image` value (if any) after a successful new upload.
- `RecipesController.Delete` calls `imageStore.DeleteAsync` on the recipe's `Image` before/after deleting the Firestore document.

### Frontend
- None required.

## Task breakdown

1. Introduce `ILogger`-based logging in `Program.cs`, replacing all `Console.WriteLine` call sites.
2. Add a `StorageMode` marker registered at startup based on which branch of the Firestore try/catch executed.
3. Rework `/health` into the single enriched readiness response described above.
4. Add `IImageStore.DeleteAsync` (soft-delete/trash semantics) + implementations.
5. Wire soft-deletion into `RecipesController.UploadImage` (replace) and `RecipesController.Delete` (remove).
6. Add the background cleanup job that purges `trash/` objects older than the retention window.
7. Manually verify against a real Firebase Storage bucket that objects move to `trash/` on replace/delete and are purged by the cleanup job after the window (contract/unit tests for this land under NF1 once feasible — see NF1's note on extending contract-test coverage).

## Dependencies

- Best done after NF1's store test project exists, so `IImageStore.DeleteAsync` and the `/health` logic get unit-tested rather than only manually verified.

## Rough sizing

**M** — the logging and `/health` changes are small; the soft-delete-with-cleanup-job flow is the larger piece, though simpler than the originally-considered liveness/readiness split.
