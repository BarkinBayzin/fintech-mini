import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from '../../environments/environment';

type RealmAccess = {
  roles?: string[];
};

type JwtPayload = {
  realm_access?: RealmAccess;
};

type TokenResponse = {
  access_token: string;
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'access_token';
  private readonly tokenSubject: BehaviorSubject<string | null>;
  private readonly rolesSubject: BehaviorSubject<string[]>;

  readonly token$: Observable<string | null>;
  readonly roles$: Observable<string[]>;
  readonly isOps$: Observable<boolean>;

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
    const token = this.readToken();
    this.tokenSubject = new BehaviorSubject<string | null>(token);
    this.rolesSubject = new BehaviorSubject<string[]>(this.readRoles(token));

    this.token$ = this.tokenSubject.asObservable();
    this.roles$ = this.rolesSubject.asObservable();
    this.isOps$ = this.roles$.pipe(map((roles) => roles.includes('ops')));
  }

  login(username: string, password: string): Observable<void> {
    const tokenUrl = this.getTokenUrl();
    const body = new URLSearchParams();
    body.set('grant_type', 'password');
    body.set('client_id', environment.keycloakClientId);
    body.set('scope', 'openid');
    body.set('username', username);
    body.set('password', password);

    return this.http
      .post<TokenResponse>(tokenUrl, body.toString(), {
        headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' })
      })
      .pipe(
        tap((response) => {
          this.applyToken(response.access_token);
        }),
        map(() => undefined)
      );
  }

  logout(): void {
    this.clearToken();
  }

  getToken(): string | null {
    return this.tokenSubject.value;
  }

  getRoles(): string[] {
    return this.rolesSubject.value;
  }

  isInRole(role: string): boolean {
    return this.getRoles().includes(role);
  }

  setToken(token: string): void {
    const trimmed = token.trim();
    if (!trimmed) {
      this.clearToken();
      return;
    }

    this.applyToken(trimmed);
  }

  clearToken(): void {
    this.applyToken(null);
  }

  private applyToken(token: string | null): void {
    if (this.isBrowser()) {
      if (token) {
        localStorage.setItem(this.tokenKey, token);
      } else {
        localStorage.removeItem(this.tokenKey);
      }
    }

    this.tokenSubject.next(token);
    this.rolesSubject.next(this.readRoles(token));
  }

  private getTokenUrl(): string {
    return `${environment.keycloakBaseUrl}/realms/${environment.keycloakRealm}/protocol/openid-connect/token`;
  }

  private readToken(): string | null {
    if (!this.isBrowser()) {
      return null;
    }

    return localStorage.getItem(this.tokenKey);
  }

  private readRoles(token: string | null): string[] {
    if (!token) {
      return [];
    }

    const payload = this.decodePayload(token);
    const roles = payload?.realm_access?.roles;
    if (!Array.isArray(roles)) {
      return [];
    }

    return roles.filter((role) => typeof role === 'string');
  }

  private decodePayload(token: string): JwtPayload | null {
    if (!this.isBrowser() || typeof atob !== 'function') {
      return null;
    }

    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    try {
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '==='.slice((base64.length + 3) % 4);
      const json = atob(padded);
      return JSON.parse(json) as JwtPayload;
    } catch {
      return null;
    }
  }

  private isBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }
}
