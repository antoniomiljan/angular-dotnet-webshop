// jsdom (the test DOM environment) doesn't implement matchMedia. Real browsers always
// do, so this is a test-environment polyfill, not application code working around a
// real-world gap - ThemeService.getInitialTheme() calls it unconditionally.
if (typeof window !== 'undefined' && !window.matchMedia) {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false
    }) as MediaQueryList;
}
