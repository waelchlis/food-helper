# Epic NF2: CI/CD Pipeline

> **Status: deferred.** Per the resolved decisions below, this epic is a later goal, not near-term work. Kept here so the plan exists when it's picked back up.

## Goal

Add automated build/lint/test checks that run on every push and pull request, so regressions are caught before merge instead of at manual-test or deploy time.

## Maps to IMPROVEMENTS.md

- "No CI pipeline" (non-functional #2)

## Resolved decisions

- **Timing: deferred.** There's no CI today and setting it up is explicitly "a goal for later," not part of the current implementation wave. This epic file stays as a ready-to-execute plan for whenever it's prioritized.
- **CI platform: not yet confirmed.** Since this isn't being built now, the GitHub Actions assumption is left unconfirmed rather than locked in — revisit when the epic is actually picked up.
- **Branch protection: not applicable while there's no CI.** No required checks for now.
- **Linting in CI: out of scope while there's no CI.** No lint/format-check script is being added as part of this (deferred) epic.

## Current state

- There is no `.github/workflows` directory and no other CI configuration in the repo (confirmed by directory search). The only automation present is [scripts/dev.sh](../scripts/dev.sh) (local dev convenience) and [docker-compose.yml](../docker-compose.yml) (deployment, not CI).
- Backend build command already exists and works locally: `./.dotnet/dotnet build backend/FoodHelper.slnx` (also wired as the "backend: build" VS Code task in [.vscode/tasks.json](../.vscode/tasks.json)).
- Frontend has `npm run build` and `npm test` ([frontend/package.json](../frontend/package.json)) via Angular CLI + Vitest.

## Proposed changes (for when this is picked up)

### Backend
- No source changes required; CI just needs to invoke the existing build (and, once NF1's Firestore-emulator coverage lands, `dotnet test`).

### Frontend
- No source changes required beyond possibly adding an `npm run lint` script if linting is introduced at that time.

### CI configuration (new, once prioritized)
- `.github/workflows/backend.yml`: restore + build `backend/FoodHelper.slnx` on push/PR touching `backend/**`; add a `dotnet test` step (NF1's in-memory contract tests already exist and cost nothing to run in CI from day one).
- `.github/workflows/frontend.yml`: `npm ci`, `npm run build`, `npm test -- --watch=false` on push/PR touching `frontend/**`.
- Optionally a status badge in [README.md](../README.md) once workflows are green.

## Task breakdown (for when this is picked up)

1. Confirm CI platform (GitHub Actions assumed given the repo's host, but re-confirm at that time).
2. Add a backend workflow that restores, builds, and runs `dotnet test` (NF1's suite already exists by then).
3. Add a frontend workflow that installs, builds, and runs the Vitest suite headlessly.
4. Scope both workflows with `paths:` filters so an unrelated frontend-only change doesn't trigger a backend build and vice versa.
5. Decide on branch protection at that time.
6. Revisit whether to add a Firestore-emulator test job now that a CI runner exists (see [NF1](epic-nf1-backend-testing-foundation.md)'s deferred emulator coverage).

## Dependencies

- Depends on NF1 existing so the backend workflow's test step is meaningful from day one.

## Rough sizing

**S** — mostly YAML; no application code changes needed for the build-only version. Not currently scheduled.
