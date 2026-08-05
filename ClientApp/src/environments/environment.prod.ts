export const environment = {
  production: true,
  // Served from the same origin as the API (see Dockerfile), so this stays relative.
  apiUrl: '/api',
  // Test key - safe to expose, swap for the live publishable key before accepting real payments.
  stripePublishableKey: 'pk_test_51TzEJ7BBM98jA9Z8mYHNaXirNL4AVJmn9n1HdEGRgVXxjzweJTMuGq1qK0A0vcIDrvY7xxPLdkdTaGjvs4r4NkmI00Iq3Ncaf5',
};