export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  auth: {
    authority: 'https://login.microsoftonline.com/{{tenantId}}',
    clientId: '{{clientId}}',
    redirectUri: 'http://localhost:4200/auth/callback',
    scopes: ['https://graph.microsoft.com/.default'],
    knownAuthorities: ['login.microsoftonline.com'],
  },
  features: {
    enableAnalytics: true,
    enableFileAttachments: true,
  },
};
