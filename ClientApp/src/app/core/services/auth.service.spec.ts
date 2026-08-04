import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const TOKEN_KEY = 'webshop_token';

function makeToken(payload: Record<string, unknown>): string {
  const base64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64url({ alg: 'none', typ: 'JWT' })}.${base64url(payload)}.`;
}

function secondsFromNow(offset: number): number {
  return Math.floor(Date.now() / 1000) + offset;
}

describe('AuthService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('isLoggedIn() is false with no stored token', () => {
    const service = TestBed.inject(AuthService);
    expect(service.isLoggedIn()).toBe(false);
  });

  it('isLoggedIn() is true for a valid, non-expired token', () => {
    localStorage.setItem(TOKEN_KEY, makeToken({ email: 'a@b.com', exp: secondsFromNow(3600) }));
    const service = TestBed.inject(AuthService);
    expect(service.isLoggedIn()).toBe(true);
  });

  it('isLoggedIn() is false for an expired token', () => {
    localStorage.setItem(TOKEN_KEY, makeToken({ email: 'a@b.com', exp: secondsFromNow(-3600) }));
    const service = TestBed.inject(AuthService);
    expect(service.isLoggedIn()).toBe(false);
  });

  it('currentEmail reads the email from a valid stored token', () => {
    localStorage.setItem(TOKEN_KEY, makeToken({ email: 'a@b.com', exp: secondsFromNow(3600) }));
    const service = TestBed.inject(AuthService);
    expect(service.currentEmail()).toBe('a@b.com');
  });

  it('currentEmail is null when the stored token is expired', () => {
    localStorage.setItem(TOKEN_KEY, makeToken({ email: 'a@b.com', exp: secondsFromNow(-3600) }));
    const service = TestBed.inject(AuthService);
    expect(service.currentEmail()).toBeNull();
  });

  it('isAdmin is true for a valid token carrying the Admin role claim', () => {
    localStorage.setItem(
      TOKEN_KEY,
      makeToken({ email: 'a@b.com', exp: secondsFromNow(3600), [ROLE_CLAIM]: 'Admin' })
    );
    const service = TestBed.inject(AuthService);
    expect(service.isAdmin()).toBe(true);
  });

  it('isAdmin is false when an Admin-role token has expired', () => {
    localStorage.setItem(
      TOKEN_KEY,
      makeToken({ email: 'a@b.com', exp: secondsFromNow(-3600), [ROLE_CLAIM]: 'Admin' })
    );
    const service = TestBed.inject(AuthService);
    expect(service.isAdmin()).toBe(false);
  });

  it('logout() clears the token and resets currentEmail/isAdmin', () => {
    localStorage.setItem(
      TOKEN_KEY,
      makeToken({ email: 'a@b.com', exp: secondsFromNow(3600), [ROLE_CLAIM]: 'Admin' })
    );
    const service = TestBed.inject(AuthService);

    service.logout();

    expect(service.getToken()).toBeNull();
    expect(service.currentEmail()).toBeNull();
    expect(service.isAdmin()).toBe(false);
  });

  it('handleAuthResponse() stores the token and updates currentEmail/isAdmin', () => {
    const service = TestBed.inject(AuthService);
    const token = makeToken({ email: 'new@b.com', exp: secondsFromNow(3600), [ROLE_CLAIM]: 'Admin' });

    service.handleAuthResponse({ token, email: 'new@b.com' });

    expect(service.getToken()).toBe(token);
    expect(service.currentEmail()).toBe('new@b.com');
    expect(service.isAdmin()).toBe(true);
  });
});
