import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse } from '../../shared/models/auth.model';

const TOKEN_KEY = 'webshop_token';

// ASP.NET Core Identity remaps the short "role" claim to this long URI by default
// when issuing the JWT; the token has no plain "role" key.
interface DecodedToken {
  sub: string;
  email: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/auth`;

  // Checked against expiry on read (construction, login, isLoggedIn()), but not
  // proactively mid-session; a session that expires while the tab is open is
  // cleared on its next rejected API call instead (see authInterceptor).
  currentEmail = signal<string | null>(this.getStoredEmail());
  isAdmin = signal<boolean>(this.getStoredIsAdmin());

  register(request: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request);
  }

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request);
  }

  handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    this.currentEmail.set(response.email);
    this.isAdmin.set(this.checkIsAdmin(response.token));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.currentEmail.set(null);
    this.isAdmin.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    return !!token && !this.isExpired(token);
  }

  private isExpired(token: string): boolean {
    try {
      return jwtDecode<DecodedToken>(token).exp * 1000 < Date.now();
    } catch {
      return true;
    }
  }

  private checkIsAdmin(token: string): boolean {
    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (!role) return false;
      return Array.isArray(role) ? role.includes('Admin') : role === 'Admin';
    } catch {
      return false;
    }
  }

  private getStoredEmail(): string | null {
    const token = this.getToken();
    if (!token || this.isExpired(token)) return null;
    try {
      return jwtDecode<DecodedToken>(token).email;
    } catch {
      return null;
    }
  }

  private getStoredIsAdmin(): boolean {
    const token = this.getToken();
    if (!token || this.isExpired(token)) return false;
    return this.checkIsAdmin(token);
  }
}