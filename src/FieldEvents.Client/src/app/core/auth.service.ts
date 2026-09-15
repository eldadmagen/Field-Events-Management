import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginResponse {
  token: string;
  userName: string;
  role: 'Dispatcher' | 'Technician';
}

const STORAGE_KEY = 'fieldevents.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<LoginResponse | null>(this.loadStoredSession());

  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly role = computed(() => this.session()?.role ?? null);
  readonly userName = computed(() => this.session()?.userName ?? null);

  constructor(private readonly http: HttpClient) {}

  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, { userName, password })
      .pipe(tap((res) => this.setSession(res)));
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  private setSession(res: LoginResponse): void {
    this.session.set(res);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(res));
  }

  private loadStoredSession(): LoginResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as LoginResponse;
    } catch {
      return null;
    }
  }
}
