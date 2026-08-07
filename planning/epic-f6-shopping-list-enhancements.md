# Epic F6: Shopping List Enhancements — Mark Purchased

## Goal

Let users check items off the shopping list while actually shopping, without deleting them.

## Maps to IMPROVEMENTS.md

- "Mark items as purchased" (functional #13)
- "Group by category/aisle" (functional #14) — **dropped; see resolved decisions.**

## Resolved decisions

- **Aisle/category grouping: dropped entirely.** No aisle taxonomy, no admin UI for assigning aisles to ingredient words, no grouped display. This epic is scoped down to "mark as purchased" only.
- **Checked items: separate "done" section.** Checked items move to a distinct section (e.g. "Checked off" below the active list) rather than staying in place with strikethrough styling or sorting to the bottom of a mixed list.

## Current state

- [ShoppingListItem](../backend/FoodHelper.Api/Models/ShoppingListItem.cs) has `Id`, `Name`, `Amount`, `Unit`, `Notes`, `UpdatedAt` — no boolean "purchased"/"checked" field.
- [ShoppingListComponent](../frontend/src/app/components/shopping-list/shopping-list.ts) supports add/edit-amount/edit-notes/remove/clear-all only.

## Proposed changes

### Backend
- `ShoppingListItem` gains a `Checked: bool` (default `false`) field. `UpsertShoppingListItemRequest` gains the same. `ShoppingListController.UpdateItem` already accepts a full replace via `PUT`, so toggling fits there without a new endpoint — a lighter `PATCH /api/shopping-list/items/{itemId}/checked` is a nice-to-have to avoid resending the full item just to toggle one boolean, but not required for a correct implementation.

### Frontend
- `ShoppingListComponent`: add a checkbox per item.
- Split the template into two sections: active (unchecked) items and a "Checked off" section below for checked items, matching the resolved decision.
- `ShoppingListService.updateItem` already supports partial updates (`Partial<Omit<ShoppingListItem, 'id'>>`), so wiring `checked` through it is a small addition, not a new code path.

## Task breakdown

1. Add `Checked` to `ShoppingListItem`/`UpsertShoppingListItemRequest`; update both store implementations (trivial — they already persist the whole item).
2. Add checkbox UI and toggle interaction in `ShoppingListComponent`.
3. Split the template/service state into active vs. checked sections.

## Dependencies

- Independent of other epics; can proceed any time after NF1.

## Rough sizing

**S** — small and self-contained now that aisle grouping (the larger part of the original epic) is out of scope.
