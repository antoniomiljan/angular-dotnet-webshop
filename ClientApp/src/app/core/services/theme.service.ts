import { Injectable, signal, effect } from '@angular/core';

const STORAGE_KEY = 'webshop_theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  isDark = signal<boolean>(this.getInitialTheme());

  constructor() {
    effect(() => {
      const scheme = this.isDark() ? 'dark' : 'light';
      document.body.style.colorScheme = scheme;
      localStorage.setItem(STORAGE_KEY, scheme);
    });
  }

  toggle(): void {
    this.isDark.update(v => !v);
  }

  private getInitialTheme(): boolean {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) return stored === 'dark';
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}