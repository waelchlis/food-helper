import { HttpClient, HttpHeaders } from '@angular/common/http';
import { effect, Injectable, inject, signal } from '@angular/core';
import { catchError, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth';

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
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/shopping-list/items`;
  private readonly sessionIdStorageKey = 'food-helper:session-id';
  private readonly sessionId = this.ensureSessionId();
  private items = signal<ShoppingListItem[]>([]);
  public readonly allItems = this.items.asReadonly();

  constructor(private authService: AuthService) {
    effect(() => {
      this.authService.isAuthenticated();
      this.loadItems();
    });
  }

  addItem(name: string, amount: number, unit: string, notes: string = ''): void {
    const existing = this.items().find(
      i => i.name.toLowerCase() === name.toLowerCase() && i.unit.toLowerCase() === unit.toLowerCase()
    );

    if (existing) {
      this.updateItem(existing.id, { amount: existing.amount + amount, notes: notes || existing.notes });
    } else {
      const payload = {
        name,
        amount,
        unit,
        notes,
      };

      this.http.post<ShoppingListItem>(this.apiUrl, payload, { headers: this.buildHeaders() })
        .pipe(tap(item => this.items.set([...this.items(), item])))
        .subscribe();
    }
  }

  addIngredientsFromRecipe(ingredients: { name: string; amount: number; unit: string }[]): void {
    for (const ing of ingredients) {
      this.addItem(ing.name, ing.amount, ing.unit);
    }
  }

  removeItem(id: string): void {
    this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.buildHeaders() })
      .pipe(tap(() => this.items.set(this.items().filter(i => i.id !== id))))
      .subscribe();
  }

  updateItem(id: string, updates: Partial<Omit<ShoppingListItem, 'id'>>): void {
    const current = this.items().find(item => item.id === id);
    if (!current) {
      return;
    }

    const payload = {
      name: (updates.name ?? current.name).trim(),
      amount: updates.amount ?? current.amount,
      unit: (updates.unit ?? current.unit).trim(),
      notes: (updates.notes ?? current.notes).trim(),
    };

    this.http.put<ShoppingListItem>(`${this.apiUrl}/${id}`, payload, { headers: this.buildHeaders() })
      .pipe(
        tap(item => {
          this.items.set(this.items().map(existing => (existing.id === id ? item : existing)));
        })
      )
      .subscribe();
  }

  clearAll(): void {
    this.http.delete<void>(this.apiUrl, { headers: this.buildHeaders() })
      .pipe(tap(() => this.items.set([])))
      .subscribe();
  }

  private loadItems(): void {
    this.http.get<ShoppingListItem[]>(this.apiUrl, { headers: this.buildHeaders() })
      .pipe(
        tap(items => this.items.set(items)),
        catchError(() => {
          this.items.set([]);
          return of([]);
        })
      )
      .subscribe();
  }

  private buildHeaders(): HttpHeaders {
    return new HttpHeaders({
      'X-Session-Id': this.sessionId,
    });
  }

  private ensureSessionId(): string {
    const existing = sessionStorage.getItem(this.sessionIdStorageKey);
    if (existing) {
      return existing;
    }

    const value = crypto.randomUUID();
    sessionStorage.setItem(this.sessionIdStorageKey, value);
    return value;
  }
}
