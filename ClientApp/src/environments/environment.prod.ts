export const environment = {
  production: true,
  apiUrl: 'https://your-deployed-api-url.com/api', // Placeholder, replace with the deployed API URL
  // stripePublishableKey is missing here entirely - checkout-page.ts calls
  // loadStripe(environment.stripePublishableKey), which fails without it.
};