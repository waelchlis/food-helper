import { Injectable, signal } from '@angular/core';

export interface ShoppingListItem {
  id: string;
  name: string;
  amount: number;
  unit: string;
  notes: string;
}

@Injectable({
  providedIn: 'root',
})
export class ShoppingListService {
  private items = signal<ShoppingListItem[]>([]);
  public readonly allItems = this.items.asReadonly();

  addItem(name: string, amount: number, unit: string, notes: string = ''): void {
    const existing = this.items().find(
      i => i.name.toLowerCase() === name.toLowerCase() && i.unit.toLowerCase() === unit.toLowerCase()
    );

    if (existing) {
      this.items.set(
        this.items().map(i =>
          i.id === existing.id ? { ...i, amount: i.amount + amount } : i
        )
      );
    } else {
      const newItem: ShoppingListItem = {
        id: crypto.randomUUID(),
        name,
        amount,
        unit,
        notes,
      };
      this.items.set([...this.items(), newItem]);
    }
  }

  addIngredientsFromRecipe(ingredients: { name: string; amount: number; unit: string }[]): void {
    for (const ing of ingredients) {
      this.addItem(ing.name, ing.amount, ing.unit);
    }
  }

  removeItem(id: string): void {
    this.items.set(this.items().filter(i => i.id !== id));
  }

  updateItem(id: string, updates: Partial<Omit<ShoppingListItem, 'id'>>): void {
    this.items.set(
      this.items().map(i => (i.id === id ? { ...i, ...updates } : i))
    );
  }

  clearAll(): void {
    this.items.set([]);
  }
}
