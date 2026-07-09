export const environment = {
  production: true,
  apiUrl: 'https://cfpapidev.gerrie.com/api',
  auth: {
    authority: 'https://login.microsoftonline.com/{{tenantId}}',
    clientId: '{{clientId}}',
    redirectUri: 'https://cfpdev.gerrie.com/auth/callback',
    scopes: ['https://graph.microsoft.com/.default'],
    knownAuthorities: ['login.microsoftonline.com'],
  },
  features: {
    enableAnalytics: true,
    enableFileAttachments: true,
  },
};
