# Food Helper — Implementation Planning

This folder breaks every item in [IMPROVEMENTS.md](../IMPROVEMENTS.md) down into epics. Each epic file describes the goal, the current state it starts from, the proposed backend/frontend changes, a task breakdown, and its dependencies on other epics. All open questions have been answered — see [open-questions.md](open-questions.md) for the full Q&A, and each epic's own "Resolved decisions" section for how the answer shaped that epic's scope. Nothing in this folder has been implemented yet.

## Scope changes from the original plan

Answering the open questions narrowed several epics and deferred one entirely:

- **NF2 (CI/CD Pipeline) is deferred** — explicitly "a goal for later," not part of the current wave of work. Its file is kept as a ready-to-execute plan for whenever it's picked up.
- **F2 (Personalization) drops ratings entirely** and reframes "notes" as a public, creator-authored field (like the existing `Tips` list) rather than a private per-user annotation — no new per-user data store needed for notes.
- **F3 (Recipe Content) drops nutrition info entirely** and caps unit conversion at same-dimension conversions only (no ingredient-density data).
- **F4 (Recipe Lifecycle) narrows edit history to an admin-only audit trail** capped at the last 5 revisions (no diffs, no revert), and drops PDF export in favor of print CSS + a shareable link.
- **F5 (Bulk Import/Export) drops image handling entirely** and uses JSON only, with import rejecting name collisions per-item rather than updating in place.
- **F6 (Shopping List Enhancements) drops aisle/category grouping entirely** — scoped down to "mark as purchased" only.
- **F7 (Meal Planner Integration) drops collaborator invitations entirely** (no outbound email capability being built) — scoped down to shopping-list generation from a meal plan, plus a per-entry serving-count field.

## Epics

### Non-functional (code quality, structure, internal wiring)

| # | Epic | Covers IMPROVEMENTS.md items |
|---|------|-------------------------------|
| [NF1](epic-nf1-backend-testing-foundation.md) | Backend Automated Testing Foundation | No backend test project; no store contract tests *(in-memory only for now — Firestore emulator coverage deferred alongside NF2)* |
| [NF2](epic-nf2-ci-cd-pipeline.md) | CI/CD Pipeline — **deferred** | No CI pipeline |
| [NF3](epic-nf3-backend-reliability-observability.md) | Backend Reliability & Observability | Silent all-or-nothing storage fallback; fake `/health`; orphaned Storage images *(soft-delete)*; console-based logging |
| [NF4](epic-nf4-auth-consolidation-rate-limiting.md) | Auth Consolidation & API Hardening | Duplicated claims-resolution logic; no rate limiting *(per-session keying)* |
| [NF5](epic-nf5-frontend-ux-consistency.md) | Frontend UX & Code Consistency | `alert()`/`confirm()` vs Material dialogs; duplicated `timeOptions`; no prod environment config |
| [NF6](epic-nf6-shared-api-contracts.md) | Shared API Contracts / Type Generation | Hand-duplicated DTO types between backend and frontend *(NSwag, checked-in output)* |

### Functional (features for end users)

| # | Epic | Covers IMPROVEMENTS.md items |
|---|------|-------------------------------|
| [F1](epic-f1-recipe-discovery.md) | Recipe Discovery — Search, Filter, Sort, Pagination, Recommendations | Server-side search/filtering *(combined, AND)*; pagination *(cursor-based)*; sorting; similar-recipe recommendations *(simple heuristic)* |
| [F2](epic-f2-personalization.md) | Personalization — Favorites & Recipe Notes | Favorites/bookmarks *(sign-in required)*; public creator notes *(ratings dropped)* |
| [F3](epic-f3-recipe-content.md) | Recipe Content — Image Gallery & Unit Conversion | Multiple images per recipe; unit conversion *(same-dimension only)*; ~~nutrition info (dropped)~~ |
| [F4](epic-f4-recipe-lifecycle.md) | Recipe Lifecycle — Edit History & Print/Share | Recipe edit history *(admin-only audit trail, last 5 versions)*; print/export view *(print CSS + share link, no PDF)* |
| [F5](epic-f5-admin-bulk-import-export.md) | Admin Bulk Import/Export | Bulk import/export for admins *(JSON only, reject-on-collision, no image handling)* |
| [F6](epic-f6-shopping-list-enhancements.md) | Shopping List Enhancements — Mark Purchased | Mark items as purchased; ~~group by category/aisle (dropped)~~ |
| [F7](epic-f7-meal-planner-integration.md) | Meal Planner Integration — Shopping List Generation | Generate shopping list from a meal plan *(with per-entry serving counts)*; ~~collaborator invitations (dropped)~~ |

## Suggested sequencing

This is a dependency-informed order, not a strict schedule:

1. **NF1 (testing foundation)** first — every subsequent backend change is safer with a test project and in-memory store contract tests in place. (Firestore-emulator coverage is deferred until NF2 exists.)
2. **NF4 (auth consolidation)** early — several functional epics (F2, F5, F7) add new owner/claims-aware endpoints; consolidating the claims-resolution helper first avoids adding a fourth copy of the same logic.
3. **NF5 (frontend consistency)** early-ish — several functional epics (F2, F4, F6, F7) add new confirmation/error flows; standardizing the pattern first means new features don't need to be retrofitted.
4. **NF3 (reliability/observability)** can proceed independently at any point; recommended before production traffic grows given it addresses a silent-data-loss risk.
5. **NF6 (shared API contracts)** is a larger structural investment — best done before F1/F3/F4/F5/F7 add a wave of new DTOs, but can also be deferred and retrofitted.
6. **F1 (discovery)** and **F6 (shopping list — mark purchased)** are largely independent and can be done any time after NF1.
7. **F7 (meal planner integration)** touches the same `MealPlansController` code NF4 consolidates; sequence after NF4.
8. **F3 (recipe content)** and **F4 (lifecycle)** both extend the `Recipe` model — coordinate their data-model changes together to avoid two separate migrations.
9. **F5 (bulk import/export)** should come after F3/F4 settle the final `Recipe` shape, since the import/export schema needs to match it.
10. **F2 (personalization)** depends on NF4's consolidated identity handling and can land any time after that; it's small enough now (notes reuse the existing recipe-edit flow) that it could also be pulled forward as an early quick win.
11. **NF2 (CI/CD)** — deferred; revisit and re-sequence when prioritized.

## Open questions

All resolved — see [open-questions.md](open-questions.md) for the full record of questions and answers.
