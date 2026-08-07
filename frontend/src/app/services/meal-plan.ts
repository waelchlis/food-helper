import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ShoppingListItem } from './shopping-list';

export interface MealEntry {
  id: string;
  date: string; // YYYY-MM-DD
  type: 'recipe' | 'custom';
  recipeId?: string;
  recipeName?: string;
  recipeImage?: string;
  /** Planned serving count for this entry; defaults to the recipe's own Servings when added. */
  servings?: number;
  customText?: string;
  createdAt: string;
}

export interface MealPlan {
  id: string;
  ownerKey: string;
  ownerEmail: string;
  name: string;
  description: string;
  collaboratorEmails: string[];
  createdAt: string;
  updatedAt: string;
}

export interface AddMealEntryPayload {
  date: string;
  type: 'recipe' | 'custom';
  recipeId?: string;
  recipeName?: string;
  recipeImage?: string;
  servings?: number;
  customText?: string;
}

@Injectable({
  providedIn: 'root',
})
export class MealPlanService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/meal-plans`;

  private mealPlans = signal<MealPlan[]>([]);
  public readonly allMealPlans = this.mealPlans.asReadonly();

  loadMealPlans(): Observable<MealPlan[]> {
    return this.http.get<MealPlan[]>(this.apiUrl).pipe(
      tap(plans => this.mealPlans.set(plans)),
      catchError(() => of(this.mealPlans()))
    );
  }

  getMealPlan(id: string): Observable<MealPlan | undefined> {
    const existing = this.mealPlans().find(p => p.id === id);
    if (existing) {
      return of(existing);
    }
    return this.http.get<MealPlan>(`${this.apiUrl}/${id}`).pipe(
      tap(plan => {
        const next = this.upsertInMemory(this.mealPlans(), plan);
        this.mealPlans.set(next);
      }),
      catchError(() => of(undefined))
    );
  }

  reloadMealPlan(id: string): Observable<MealPlan | undefined> {
    return this.http.get<MealPlan>(`${this.apiUrl}/${id}`).pipe(
      tap(plan => {
        const next = this.upsertInMemory(this.mealPlans(), plan);
        this.mealPlans.set(next);
      }),
      catchError(() => of(undefined))
    );
  }

  createMealPlan(name: string, description: string): Observable<MealPlan> {
    return this.http.post<MealPlan>(this.apiUrl, { name, description }).pipe(
      tap(plan => this.mealPlans.set([plan, ...this.mealPlans()]))
    );
  }

  updateMealPlan(id: string, name: string, description: string): Observable<MealPlan> {
    return this.http.put<MealPlan>(`${this.apiUrl}/${id}`, { name, description }).pipe(
      tap(plan => this.mealPlans.set(this.upsertInMemory(this.mealPlans(), plan)))
    );
  }

  deleteMealPlan(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.mealPlans.set(this.mealPlans().filter(p => p.id !== id)))
    );
  }

  getEntries(planId: string): Observable<MealEntry[]> {
    return this.http.get<MealEntry[]>(`${this.apiUrl}/${planId}/entries`);
  }

  addEntry(planId: string, payload: AddMealEntryPayload): Observable<MealEntry> {
    return this.http.post<MealEntry>(`${this.apiUrl}/${planId}/entries`, payload);
  }

  deleteEntry(planId: string, entryId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${planId}/entries/${entryId}`);
  }

  addCollaborator(planId: string, email: string): Observable<MealPlan> {
    return this.http.post<MealPlan>(`${this.apiUrl}/${planId}/collaborators`, { email }).pipe(
      tap(plan => this.mealPlans.set(this.upsertInMemory(this.mealPlans(), plan)))
    );
  }

  generateShoppingList(planId: string, from: string, to: string): Observable<ShoppingListItem[]> {
    return this.http.post<ShoppingListItem[]>(`${this.apiUrl}/${planId}/shopping-list`, { from, to });
  }

  removeCollaborator(planId: string, email: string): Observable<MealPlan> {
    return this.http
      .delete<MealPlan>(`${this.apiUrl}/${planId}/collaborators/${encodeURIComponent(email)}`)
      .pipe(tap(plan => this.mealPlans.set(this.upsertInMemory(this.mealPlans(), plan))));
  }

  private upsertInMemory(list: MealPlan[], plan: MealPlan): MealPlan[] {
    const idx = list.findIndex(p => p.id === plan.id);
    if (idx === -1) return [plan, ...list];
    const next = [...list];
    next[idx] = plan;
    return next;
  }
}
