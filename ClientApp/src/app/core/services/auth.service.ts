import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse } from '../../shared/models/auth.model';

const TOKEN_KEY = 'webshop_token';

interface DecodedToken {
  sub: string;
  email: string;
  role?: string | string[];
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/auth`;

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
    return !!this.getToken();
  }

  private checkIsAdmin(token: string): boolean {
    try {
      const decoded = jwtDecode<DecodedToken>(token);
      if (!decoded.role) return false;
      return Array.isArray(decoded.role) ? decoded.role.includes('Admin') : decoded.role === 'Admin';
    } catch {
      return false;
    }
  }

  private getStoredEmail(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      return jwtDecode<DecodedToken>(token).email;
    } catch {
      return null;
    }
  }

  private getStoredIsAdmin(): boolean {
    const token = this.getToken();
    if (!token) return false;
    return this.checkIsAdmin(token);
  }
}