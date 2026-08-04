import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

// Applied to every spec's TestBed automatically. Most components in this app use
// RouterLink and/or inject HttpClient-backed services, so without these, specs fail
// with NG0201 (no provider found) rather than testing anything meaningful.
export default [provideRouter([]), provideHttpClient(), provideHttpClientTesting()];
