// Production values are supplied at deploy time — do not commit real secrets here.
// See README.md "Deployment" section for how these get filled in before a production build.
export const environment = {
  production: true,
  apiBaseUrl: '/api',
  googleClientId: 'REPLACE_AT_DEPLOY_TIME',
  oidcIssuer: 'https://accounts.google.com',
};
