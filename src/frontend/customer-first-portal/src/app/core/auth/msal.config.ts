import { MsalInterceptorConfiguration } from '@azure/msal-angular';
import { InteractionType, LogLevel } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

const isIE =
  window.navigator.userAgent.indexOf('MSIE ') > -1 ||
  window.navigator.userAgent.indexOf('Trident/') > -1;

export const msalConfig = {
  auth: {
    clientId: '452ec6c1-c8ee-4f34-b990-0dbfcdda46b7',
    authority:
      'https://login.microsoftonline.com/7d0059b6-4454-4fb6-a191-dcaaf13760ca',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: isIE,
  },
  system: {
    loggerOptions: {
      loggerCallback: () => {},
      piiLoggingEnabled: false,
      logLevel: LogLevel.Warning,
    },
  },
};

export const msalInterceptorConfig = {
  interactionType: InteractionType.Redirect,
  protectedResourceMap: new Map<string, string[]>([
    [
      environment.apiUrl,
      ['api://82236300-90e7-45c4-a414-1974b4b36e7b/access_as_user'],
    ],
  ]),
} as MsalInterceptorConfiguration;
