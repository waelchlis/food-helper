import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth';
import { Recipe } from './recipe';

/**
 * Favorites require sign-in (unlike the shopping list's session-based model) — see
 * planning/epic-f2-personalization.md for why an anonymous/session-based favorites model
 * was rejected (favorites tied to an expiring session id would just vanish).
 */
@Injectable({ providedIn: 'root' })
export class FavoriteService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly apiUrl = `${environment.apiBaseUrl}/favorites`;

  private readonly favoriteRecipes = signal<Recipe[]>([]);
  readonly allFavorites = this.favoriteRecipes.asReadonly();
  readonly favoriteIds = computed(() => new Set(this.favoriteRecipes().map(r => r.id)));

  constructor() {
    effect(() => {
      if (this.authService.isAuthenticated()) {
        this.refresh();
      } else {
        this.favoriteRecipes.set([]);
      }
    });
  }

  refresh(): void {
    this.http.get<Recipe[]>(this.apiUrl).pipe(
      tap(recipes => this.favoriteRecipes.set(recipes)),
      catchError(() => {
        this.favoriteRecipes.set([]);
        return of([]);
      })
    ).subscribe();
  }

  isFavorite(recipeId: string): boolean {
    return this.favoriteIds().has(recipeId);
  }

  toggle(recipe: Recipe): void {
    if (this.isFavorite(recipe.id)) {
      this.http.delete<void>(`${this.apiUrl}/${recipe.id}`).pipe(
        tap(() => this.favoriteRecipes.set(this.favoriteRecipes().filter(r => r.id !== recipe.id)))
      ).subscribe();
    } else {
      this.http.post<void>(`${this.apiUrl}/${recipe.id}`, {}).pipe(
        tap(() => this.favoriteRecipes.set([...this.favoriteRecipes(), recipe]))
      ).subscribe();
    }
  }
}
