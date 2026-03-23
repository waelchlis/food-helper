import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom, ReplaySubject } from 'rxjs';
import { environment } from '../../environments/environment';

type GoogleIdConfiguration = {
  client_id: string;
  callback: (response: GoogleCredentialResponse) => void;
  auto_select?: boolean;
  cancel_on_tap_outside?: boolean;
  context?: 'signin' | 'signup' | 'use';
  itp_support?: boolean;
  use_fedcm_for_button?: boolean;
};

type GoogleButtonConfiguration = {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'large' | 'medium' | 'small';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: string;
};

type GoogleCredentialResponse = {
  credential: string;
  select_by?: string;
  state?: string;
};

type GoogleAccountsId = {
  initialize(config: GoogleIdConfiguration): void;
  prompt(): void;
  renderButton(parent: HTMLElement, options: GoogleButtonConfiguration): void;
  cancel(): void;
  disableAutoSelect(): void;
};

type GoogleIdentityWindow = Window & {
  google?: {
    accounts?: {
      id?: GoogleAccountsId;
    };
  };
};

type GoogleIdentityClaims = {
  sub?: string;
  exp?: number;
  name?: string;
  given_name?: string;
  email?: string;
};

const tokenStorageKey = 'food-helper.google-id-token';
const adminStorageKey = 'food-helper.is-admin';
let googleIdentityScriptPromise: Promise<GoogleAccountsId> | null = null;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly initialized = signal(false);
  private readonly authenticated = signal(false);
  private readonly admin = signal(false);
  private readonly idToken = signal('');
  private readonly identityClaims = signal<GoogleIdentityClaims | null>(null);
  private readonly adminStatusResolved$ = new ReplaySubject<void>(1);
  private googleAccountsId: GoogleAccountsId | null = null;
  private readonly hasGoogleClientId =
    environment.googleClientId.trim().length > 0 &&
    !environment.googleClientId.includes('YOUR_GOOGLE_CLIENT_ID');

  readonly isReady = this.initialized.asReadonly();
  readonly isAuthenticated = computed(() => this.authenticated());
  readonly isAdmin = computed(() => this.admin());
  readonly isConfigured = computed(() => this.hasGoogleClientId);
  readonly userDisplayName = computed(() => {
    const claims = this.identityClaims();
    return claims?.given_name?.trim() || claims?.name?.trim() || claims?.email?.trim() || 'there';
  });
  readonly userEmail = computed(() => this.identityClaims()?.email ?? null);

  constructor() {
    this.restoreStoredSession();

    if (!this.hasGoogleClientId) {
      this.initialized.set(true);
      return;
    }

    void this.loadGoogleIdentityScript()
      .then((googleAccountsId) => {
        this.googleAccountsId = googleAccountsId;
        this.googleAccountsId.initialize({
          client_id: environment.googleClientId,
          callback: ({ credential }) => {
            this.persistSession(credential);
          },
          auto_select: false,
          cancel_on_tap_outside: true,
          context: 'signin',
          itp_support: true,
          use_fedcm_for_button: true,
        });
      })
      .catch(() => {
        this.clearSession();
      })
      .finally(() => {
        this.initialized.set(true);
      });
  }

  login(): void {
    if (!this.googleAccountsId) {
      return;
    }

    this.googleAccountsId.prompt();
  }

  logout(): void {
    this.googleAccountsId?.cancel();
    this.googleAccountsId?.disableAutoSelect();
    this.clearSession();
    this.admin.set(false);
  }

  renderButton(hostElement: HTMLElement): void {
    if (!this.googleAccountsId || this.authenticated()) {
      return;
    }

    hostElement.replaceChildren();
    this.googleAccountsId.renderButton(hostElement, {
      type: 'standard',
      theme: 'outline',
      size: 'medium',
      text: 'signin_with',
      shape: 'rectangular',
      logo_alignment: 'left',
      width: '110',
    });
  }

  getIdToken(): string {
    return this.idToken();
  }

  getUserSubject(): string | null {
    return this.identityClaims()?.sub ?? null;
  }

  private restoreStoredSession(): void {
    const storedToken = sessionStorage.getItem(tokenStorageKey) ?? '';
    if (!storedToken) {
      return;
    }

    const claims = this.parseJwtClaims(storedToken);
    if (!claims || this.isExpired(claims)) {
      sessionStorage.removeItem(tokenStorageKey);
      return;
    }

    this.idToken.set(storedToken);
    this.identityClaims.set(claims);
    this.authenticated.set(true);
    this.admin.set(sessionStorage.getItem(adminStorageKey) === 'true');
    queueMicrotask(() => this.fetchAdminStatus());
  }

  private persistSession(token: string): void {
    const claims = this.parseJwtClaims(token);
    if (!claims || this.isExpired(claims)) {
      this.clearSession();
      return;
    }

    sessionStorage.setItem(tokenStorageKey, token);
    this.idToken.set(token);
    this.identityClaims.set(claims);
    this.authenticated.set(true);
    this.fetchAdminStatus();
  }

  waitForAdminStatus(): Promise<void> {
    if (!this.authenticated()) {
      return Promise.resolve();
    }
    return firstValueFrom(this.adminStatusResolved$).then(() => {});
  }

  private fetchAdminStatus(): void {
    this.http.get<{ isAdmin: boolean }>(`${environment.apiBaseUrl}/auth/me`).subscribe({
      next: (res) => {
        this.admin.set(res.isAdmin);
        sessionStorage.setItem(adminStorageKey, String(res.isAdmin));
        this.adminStatusResolved$.next();
      },
      error: () => {
        this.admin.set(false);
        sessionStorage.removeItem(adminStorageKey);
        this.adminStatusResolved$.next();
      },
    });
  }

  private clearSession(): void {
    sessionStorage.removeItem(tokenStorageKey);
    sessionStorage.removeItem(adminStorageKey);
    this.idToken.set('');
    this.identityClaims.set(null);
    this.authenticated.set(false);
    this.admin.set(false);
  }

  private parseJwtClaims(token: string): GoogleIdentityClaims | null {
    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    try {
      const normalizedPayload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const paddedPayload = normalizedPayload.padEnd(Math.ceil(normalizedPayload.length / 4) * 4, '=');
      const payload = atob(paddedPayload);
      return JSON.parse(payload) as GoogleIdentityClaims;
    } catch {
      return null;
    }
  }

  private isExpired(claims: GoogleIdentityClaims): boolean {
    return !claims.exp || claims.exp * 1000 <= Date.now();
  }

  private loadGoogleIdentityScript(): Promise<GoogleAccountsId> {
    if (this.googleAccountsId) {
      return Promise.resolve(this.googleAccountsId);
    }

    const googleWindow = window as GoogleIdentityWindow;
    const existingGoogleAccountsId = googleWindow.google?.accounts?.id;
    if (existingGoogleAccountsId) {
      return Promise.resolve(existingGoogleAccountsId);
    }

    if (googleIdentityScriptPromise) {
      return googleIdentityScriptPromise;
    }

    googleIdentityScriptPromise = new Promise<GoogleAccountsId>((resolve, reject) => {
      const existingScript = document.querySelector<HTMLScriptElement>('script[src="https://accounts.google.com/gsi/client"]');
      if (existingScript) {
        existingScript.addEventListener(
          'load',
          () => {
            const loadedGoogleAccountsId = googleWindow.google?.accounts?.id;
            if (loadedGoogleAccountsId) {
              resolve(loadedGoogleAccountsId);
              return;
            }

            reject(new Error('Google Identity Services did not initialize.'));
          },
          { once: true }
        );
        existingScript.addEventListener(
          'error',
          () => {
            reject(new Error('Failed to load Google Identity Services.'));
          },
          { once: true }
        );
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => {
        const loadedGoogleAccountsId = googleWindow.google?.accounts?.id;
        if (loadedGoogleAccountsId) {
          resolve(loadedGoogleAccountsId);
          return;
        }

        reject(new Error('Google Identity Services did not initialize.'));
      };
      script.onerror = () => {
        reject(new Error('Failed to load Google Identity Services.'));
      };
      document.head.appendChild(script);
    });

    return googleIdentityScriptPromise;
  }
}
