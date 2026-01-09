import { Injectable } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';

type RealmAccess = {
  roles?: string[];
};

type JwtPayload = {
  realm_access?: RealmAccess;
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'access_token';
  private readonly tokenSubject = new BehaviorSubject<string | null>(this.readToken());
  private readonly rolesSubject = new BehaviorSubject<string[]>(this.readRoles(this.tokenSubject.value));

  readonly token$ = this.tokenSubject.asObservable();
  readonly roles$ = this.rolesSubject.asObservable();
  readonly isOps$ = this.roles$.pipe(map((roles) => roles.includes('ops')));

  getToken(): string | null {
    return this.tokenSubject.value;
  }

  setToken(token: string): void {
    const trimmed = token.trim();
    if (!trimmed) {
      this.clearToken();
      return;
    }

    if (this.canAccessStorage()) {
      localStorage.setItem(this.tokenKey, trimmed);
    }

    this.tokenSubject.next(trimmed);
    this.rolesSubject.next(this.readRoles(trimmed));
  }

  clearToken(): void {
    if (this.canAccessStorage()) {
      localStorage.removeItem(this.tokenKey);
    }

    this.tokenSubject.next(null);
    this.rolesSubject.next([]);
  }

  private readToken(): string | null {
    if (!this.canAccessStorage()) {
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

  private canAccessStorage(): boolean {
    return typeof localStorage !== 'undefined';
  }
}
