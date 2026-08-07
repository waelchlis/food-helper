# Open Questions

Decisions needed before implementation starts, grouped by epic. Answering these will materially change scope/sizing for several epics — most notably F2, F3, F4, F5, and F7.

> **Status: all answered.** Resolutions have been folded into each epic's "Resolved decisions" section — see [README.md](README.md) for a summary of what changed. This file remains as the historical record of the Q&A.

## NF1 — Backend Automated Testing Foundation

1. **Test framework:** xUnit is the idiomatic default for new .NET projects and pairs well with `WebApplicationFactory` for controller tests — any objection to that, or a house preference for NUnit/MSTest?

#### Answer: use xUnit

2. **Firestore contract tests:** should the shared store-contract tests run against a real Firestore emulator (`gcloud emulators firestore start`) in CI, or should Firestore-backed tests be skipped/mocked for now and only the in-memory implementations get full contract coverage until an emulator is set up? The emulator gives real confidence but adds CI setup complexity.
#### Answer: there is no CI currently so skip them for now

## NF2 — CI/CD Pipeline

3. **CI platform:** GitHub Actions is assumed since the repo is hosted on GitHub — please confirm, or name a different platform (Azure DevOps, GitLab CI, etc.) if that's actually the target.
#### Answer: no CI currently setup, thats a goal for later
4. **Branch protection:** should passing CI become a required check before merging to `main`, or is CI advisory-only for now?
#### Answer: no CI setup so no
5. **Linting in CI:** there's a `.prettierrc` but no enforcing lint/format-check script today. Should format/lint checking be added as part of this epic, or is that explicitly out of scope?
#### Answer: no CI, ignore

## NF3 — Backend Reliability & Observability

6. **`/health` consumers:** is this endpoint hit by an uptime monitor, a container orchestrator's liveness/readiness probes, both, or neither today? That determines whether a liveness/readiness split is worth the complexity or whether one enriched `/health` response is enough.
#### Answer: currently neither, one enriched response is enough
7. **Image deletion timing:** should replaced/deleted recipe images be removed from Firebase Storage immediately, or soft-deleted with a grace period (e.g. moved to a `trash/` prefix, purged after N days) in case of accidental overwrite?
#### Answer: soft-delete

## NF4 — Auth Consolidation & API Hardening

8. **Rate-limit thresholds and key:** what request volume is actually expected/acceptable for write endpoints, and should limits key on per-IP, per-authenticated-user (`sub`), or per-`X-Session-Id`? The app has three different identity concepts already in play (JWT subject, session id, IP) and the right one likely differs by endpoint (e.g. shopping-list writes are session-keyed, admin writes are user-keyed).
#### Answer: the app is only used by a few (under 50) users, so not a lot of volume is expected. Limits should be per session
9. **Claims fallback verification:** the consolidation work reconciles several slightly different claim-lookup fallback chains into one — should this be validated against a real captured Google ID token payload before rollout, to make sure the "correct" merged chain doesn't accidentally drop a claim some existing user relies on?
#### Answer: no claims are currently relied on so no verification necessary

## NF5 — Frontend UX & Code Consistency

10. **Production environment values:** actual production Google OAuth client ID and OIDC audience/issuer values are needed to populate `environment.prod.ts` — these are deployment configuration only the user can supply, not something to infer from the existing dev values in `environment.ts`.
#### Answer: the values are available and will be provided when deploying the app

## NF6 — Shared API Contracts / Type Generation

11. **Generator tool:** which OpenAPI-to-TypeScript generator is preferred — `openapi-typescript` (lightweight, types-only), NSwag (can also generate an HTTP client, more opinionated), or `openapi-generator-cli`? This affects how much of the existing hand-written `RecipeService`-style wrapper logic stays vs. gets replaced.
#### Answer: use nswag
12. **Checked-in vs. generated-at-build:** should generated types be committed to the repo (simpler review diffs, risk of staleness) or regenerated on every build/CI run (always fresh, adds a build step and a backend-must-be-running-or-spec-must-be-checked-in dependency)?
#### Answer: checked-in

## F1 — Recipe Discovery

13. **Combined vs. single-select filtering:** today's UI applies only one of text/ingredient/category at a time ([RecipeListComponent.filteredRecipes](../frontend/src/app/components/recipe-list/recipe-list.ts)). Should the redesign allow combining filters (text AND ingredient AND category simultaneously), which is a more standard search-UI expectation but changes both the query contract and the UI significantly?
#### Answer: yes

14. **Pagination style:** offset-based (`page`/`pageSize`, simple but can skip/duplicate rows if data changes between pages) or cursor-based (`startAfter`, matches how Firestore itself paginates natively, more correct but a bigger API change)?
#### Answer: cursor-based
15. **Firestore composite indexes:** combining filter fields with sort fields in a Firestore query often requires manually provisioning composite indexes (done via `firestore.indexes.json`/Firebase console, not app code). Who owns provisioning these in each environment — is that something you'll handle, or does it need to be scripted/documented as part of this epic?
#### Answer: scripted and documented, not manually handled
16. **"Similar recipes" heuristic:** is a simple heuristic (shared category and/or overlapping ingredients) sufficient, or is there an expectation of something more sophisticated (e.g. a proper recommendation model)? The epic assumes the simple heuristic.
#### Answer: simple heuristic is enough

## F2 — Personalization

17. **Favorites identity model:** should favorites require sign-in (simpler, matches how recipe creation already works), or should anonymous/session-based users be able to favorite too (matches the shopping list's more permissive model, but favorites tied to an expiring session id is a confusing UX — they'd vanish)?
#### Answer: sign in required
18. **Ratings scope:** a simple star/numeric value, or written reviews too? If written reviews, that's closer to a moderated public-content feature (spam/abuse concerns) than a private personalization feature.
#### Answer: skip ratings for now, dont implement this feature
19. **Aggregate rating computation:** denormalized onto the `Recipe` document and updated on every rating write (fast reads, more write complexity/potential race conditions), or computed on read by querying all ratings for a recipe (simple, but doesn't scale as well)?
#### Answer: skip ratings for now
20. **Notes vs. ratings:** is "personal notes" meant to be a private per-user field on a recipe (e.g. "I substituted X for Y and it worked"), distinct from a public rating? The current improvement item bundles them ("ratings or personal notes") ambiguously.
#### Answer: skip ratings for now, notes are meant for the creator of a recipe to inform all users that see it (not private)

## F3 — Recipe Content

21. **Existing single-image migration:** when `Recipe.Image` becomes `Recipe.Images`, should existing recipes' single image become the first/only entry automatically (a one-time migration script), and is there a rollback plan if the migration needs to be re-run?
#### Answer: yes the current image should become the first one, no rollback planned
22. **Nutrition granularity:** per-serving values or per-recipe totals? Manually entered by the recipe author/admin, or is integration with a nutrition database/API (e.g. USDA FoodData Central, Edamam) actually wanted despite the added complexity and potential cost?
#### Answer: skip nutrition implementation for now
23. **Unit conversion scope:** same-dimension conversions only (e.g. g↔oz, ml↔cup, which don't need per-ingredient data), or ingredient-aware density conversions (e.g. "2 cups of flour" → grams, which requires an ingredient-specific density lookup table)? The former is a much smaller, self-contained feature; the latter is a meaningfully bigger data problem.
#### Answer: the former

## F4 — Recipe Lifecycle

24. **Edit history audience:** an admin-only audit trail (who changed what, when — mainly for accountability), or a user-facing feature with visible diffs and a revert button? These are very different amounts of work.
#### Answer: audit trail only
25. **History retention:** should revisions be kept forever, or pruned after N versions / N days, given unbounded Firestore document growth has a real storage cost?
#### Answer: pruned after 5 versions
26. **Export scope:** does "print/export" mean (a) a browser print stylesheet only, (b) true PDF generation (client-side library or server-rendered), or (c) a shareable public read-only link? Each is a substantially different amount of work, and the epic currently assumes (a) as the baseline with (b)/(c) as optional extensions.
#### Answer: (a) and (c)

## F5 — Admin Bulk Import/Export

27. **File format:** JSON (matches the nested recipe structure naturally) or CSV (more familiar to non-technical admins, but ingredients/instructions/tips don't flatten cleanly)? Both is also an option but doubles the work.
#### Answer: json
28. **Import conflict resolution:** should importing a recipe with a name matching an existing one update it in place, or should import always create new recipes (append-only, admin de-dupes manually afterward)?
#### Answer: importing with an already existing name should abort with an error
29. **Image handling on import:** are imported recipes expected to reference already-hosted image URLs only, or does bulk import need to also handle bundled image files (e.g. a zip with images + a JSON/CSV manifest)? The latter is a significantly larger feature.
#### Answer: no handling of images for imports necessary

## F6 — Shopping List Enhancements

30. **Checked-item placement:** should checked items move to a separate "done" section, get inline strikethrough styling and stay in place, or sort to the bottom of their group? This is a UX call with no clearly "correct" answer.
#### Answer: seperate done section
31. **Aisle taxonomy:** a fixed enum of aisles (e.g. Produce/Dairy/Meat/Bakery/Frozen/Pantry/Other) that's consistent across all users, or free-text per-admin categories? A fixed enum is simpler to build grouped UI for; free-text is more flexible but harder to group meaningfully.
#### Answer: no aisles needed for now, skip
32. **Unmatched items:** if a shopping-list item's name doesn't match any known `IngredientWord`, should it just land in an "Other"/ungrouped bucket, or should adding a shopping-list item require selecting from the known vocabulary (which would be a bigger UX change to the current free-text add flow)?
#### Answer: skip

## F7 — Meal Planner Integration

33. **Email provider:** the app has zero outbound-email capability today. Is there an existing transactional-email provider account (SendGrid, Mailgun, AWS SES, plain SMTP relay) to integrate with, or does one need to be selected and provisioned as part of this work? This blocks the collaborator-invitation half of the epic entirely until answered.
#### Answer: skip the collaborator invitation feature
34. **Meal-entry serving counts:** `MealEntry` has no serving-count field today, so generating a shopping list from planned meals has no way to know "how many servings of this recipe are planned for this day" — should this field be added (and if so, does it default to the recipe's own `Servings`, or must it be set per-entry when adding a meal)?
#### Answer: yes add it, default to recipes own servings but make sure the user can set it per entry when adding a meal
35. **Ingredient merge matching:** when aggregating ingredients across multiple recipes for shopping-list generation, should merging require an exact name+unit match (matches today's `ShoppingListService.addItem` behavior, simple but can produce duplicate near-identical entries like "tomato" vs. "tomatoes"), or is some fuzzier matching expected?
#### Answer: exact matching is enough