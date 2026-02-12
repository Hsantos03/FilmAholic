/**
 * Ambiente de produção (Azure).
 * URL vazio = mesmo origem (quando o front e a API estão no mesmo App Service).
 * Se a API estiver noutro URL (ex: https://filmaholic-api.azurewebsites.net),
 * altere apiBaseUrl para esse URL.
 */
export const environment = {
  production: true,
  apiBaseUrl: ''
};
