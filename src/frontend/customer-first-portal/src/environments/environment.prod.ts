export const environment = {
  production: true,
  apiUrl: '/api',
  auth: {
    authority: 'https://login.microsoftonline.com/{{tenantId}}',
    clientId: '{{clientId}}',
    redirectUri: 'https://app.customerfirstportal.com/auth/callback',
    scopes: ['https://graph.microsoft.com/.default'],
    knownAuthorities: ['login.microsoftonline.com'],
  },
  features: {
    enableAnalytics: true,
    enableFileAttachments: true,
  },
};
