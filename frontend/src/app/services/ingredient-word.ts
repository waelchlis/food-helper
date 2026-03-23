import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface IngredientWord {
  id: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class IngredientWordService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/ingredient-words`;
  private readonly words = signal<IngredientWord[]>([]);

  readonly allWords = this.words.asReadonly();

  loadAll(): void {
    this.http.get<IngredientWord[]>(this.apiUrl).pipe(
      tap(items => this.words.set(items)),
      catchError(() => {
        this.words.set([]);
        return of([]);
      })
    ).subscribe();
  }

  add(name: string): void {
    this.http.post<IngredientWord>(this.apiUrl, { name }).pipe(
      tap(word => {
        const updated = [...this.words(), word].sort((a, b) => a.name.localeCompare(b.name));
        this.words.set(updated);
      })
    ).subscribe();
  }

  delete(id: string): void {
    this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.words.set(this.words().filter(w => w.id !== id)))
    ).subscribe();
  }
}
