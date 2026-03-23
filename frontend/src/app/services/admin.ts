import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Admin {
  id: string;
  email: string;
  isLinked: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/admin`;
  private admins = signal<Admin[]>([]);

  public readonly allAdmins = this.admins.asReadonly();

  loadAdmins(): Observable<Admin[]> {
    return this.http.get<Admin[]>(this.apiUrl).pipe(
      tap(items => this.admins.set(items)),
      catchError(() => of(this.admins())),
    );
  }

  addAdmin(email: string): Observable<Admin> {
    return this.http.post<Admin>(this.apiUrl, { email }).pipe(
      tap(admin => this.admins.set([...this.admins(), admin])),
    );
  }

  removeAdmin(id: string): Observable<boolean> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      map(() => true),
      tap(() => this.admins.set(this.admins().filter(a => a.id !== id))),
      catchError(() => of(false)),
    );
  }
}
