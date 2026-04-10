import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Category {
  id: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/categories`;
  private readonly _categories = signal<Category[]>([]);

  readonly allCategories = this._categories.asReadonly();

  loadAll(): void {
    this.http.get<Category[]>(this.apiUrl).pipe(
      tap(items => this._categories.set(items)),
      catchError(() => {
        this._categories.set([]);
        return of([]);
      })
    ).subscribe();
  }

  add(name: string): Observable<Category> {
    return this.http.post<Category>(this.apiUrl, { name }).pipe(
      tap(category => {
        const updated = [...this._categories(), category].sort((a, b) => a.name.localeCompare(b.name));
        this._categories.set(updated);
      })
    );
  }

  rename(id: string, newName: string): Observable<Category> {
    return this.http.put<Category>(`${this.apiUrl}/${id}`, { name: newName }).pipe(
      tap(updated => {
        const next = this._categories()
          .map(c => (c.id === id ? updated : c))
          .sort((a, b) => a.name.localeCompare(b.name));
        this._categories.set(next);
      })
    );
  }

  delete(id: string): Observable<boolean> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      map(() => true),
      tap(() => this._categories.set(this._categories().filter(c => c.id !== id))),
      catchError(() => of(false))
    );
  }
}
