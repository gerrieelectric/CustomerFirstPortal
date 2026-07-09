import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthenticationResult, SilentRequest } from '@azure/msal-browser';
import { MSAL_INSTANCE } from '@azure/msal-angular';
import { BehaviorSubject, Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly msalInstance = inject(MSAL_INSTANCE);
  private readonly http = inject(HttpClient);
  private readonly authorizationApiUrl = `${environment.apiUrl}/authorization`;
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private readonly isAdminSubject = new BehaviorSubject<boolean | null>(null);
  private readonly initialized = this.msalInstance.initialize();
  private readonly ready: Promise<void>;
  private isReady = false;

  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  public isAdmin$ = this.isAdminSubject.asObservable();

  constructor() {
    this.ready = this.initialized
      .then(() => this.msalInstance.handleRedirectPromise())
      .then((result: AuthenticationResult | null) => {
        if (result?.account) {
          this.msalInstance.setActiveAccount(result.account);
        }

        if (!this.msalInstance.getActiveAccount()) {
          const firstAccount = this.msalInstance.getAllAccounts()[0];
          if (firstAccount) {
            this.msalInstance.setActiveAccount(firstAccount);
          }
        }

        this.checkAuthStatus();
        this.isReady = true;
      })
      .catch((error) => {
        console.error('MSAL initialization failed:', error);
      });
  }

  private checkAuthStatus(): void {
    const account = this.msalInstance.getActiveAccount();
    this.isAuthenticatedSubject.next(!!account);
  }

  login(redirectStartPage?: string): void {
    this.ready
      .then(() => {
        return this.msalInstance.loginRedirect({
          scopes: ['api://82236300-90e7-45c4-a414-1974b4b36e7b/access_as_user'],
          redirectStartPage,
        });
      })
      .catch((error) => {
        console.error('Login failed:', error);
      });
  }

  logout(): void {
    this.initialized
      .then(() => this.msalInstance.logoutPopup())
      .finally(() => {
        this.isAuthenticatedSubject.next(false);
        this.isAdminSubject.next(null);
      });
  }

  isAuthenticated(): boolean {
    if (!this.isReady) {
      return false;
    }

    return !!this.msalInstance.getActiveAccount();
  }

  getAccount() {
    if (!this.isReady) {
      return null;
    }

    return this.msalInstance.getActiveAccount();
  }

  async isAuthenticatedAsync(): Promise<boolean> {
    await this.ready;

    let account = this.msalInstance.getActiveAccount();
    if (!account) {
      const firstAccount = this.msalInstance.getAllAccounts()[0];
      if (firstAccount) {
        this.msalInstance.setActiveAccount(firstAccount);
        account = firstAccount;
      }
    }

    return !!account;
  }

  async getAccessToken(): Promise<string | null> {
    await this.ready;

    let account = this.msalInstance.getActiveAccount();
    if (!account) {
      const firstAccount = this.msalInstance.getAllAccounts()[0];
      if (firstAccount) {
        this.msalInstance.setActiveAccount(firstAccount);
        account = firstAccount;
      }
    }

    if (!account) {
      return null;
    }

    const tokenRequest: SilentRequest = {
      account,
      scopes: ['api://82236300-90e7-45c4-a414-1974b4b36e7b/access_as_user'],
    };

    try {
      const response = await this.msalInstance.acquireTokenSilent(tokenRequest);
      return response.accessToken;
    } catch (error) {
      console.error('Silent token acquisition failed:', error);
      return null;
    }
  }

  async isAdminAsync(forceRefresh = false): Promise<boolean> {
    const isAuthenticated = await this.isAuthenticatedAsync();

    if (!isAuthenticated) {
      this.isAdminSubject.next(false);
      return false;
    }

    if (!forceRefresh && this.isAdminSubject.value !== null) {
      return this.isAdminSubject.value;
    }

    try {
      const token = await this.getAccessToken();

      if (!token) {
        console.warn('[AuthService] No token available');
        this.isAdminSubject.next(false);
        return false;
      }

      const isAdmin = await firstValueFrom(
        this.http.get<boolean>(`${this.authorizationApiUrl}/me/is-admin`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }),
      );

      this.isAdminSubject.next(isAdmin);
      return isAdmin;
    } catch (error) {
      console.error('[AuthService] HTTP request failed:', error);
      this.isAdminSubject.next(false);
      return false;
    }
  }
}
