import { defineConfig, devices } from '@playwright/test';
import path from 'path';

/**
 * Configuração do Playwright para testes E2E do FilmAholic.
 *
 * URL base: Angular dev-server em https://localhost:50905
 * API backend: https://localhost:7277
 *
 * Para correr os testes:
 *   npm run test:e2e
 *
 * Pré-requisito: o backend (.NET) e o frontend (Angular) devem estar a correr.
 */
export default defineConfig({
  testDir: './e2e',
  /* Tempo máximo por teste */
  timeout: 30_000,
  /* Tempo máximo para o expect */
  expect: { timeout: 10_000 },
  /* Correr testes em paralelo */
  fullyParallel: true,
  /* Falhar o build em caso de test.only no CI */
  forbidOnly: !!process.env['CI'],
  /* Número de tentativas em caso de falha */
  retries: process.env['CI'] ? 2 : 0,
  /* Workers em paralelo */
  workers: process.env['CI'] ? 1 : undefined,
  /* Reporter */
  reporter: [['html', { open: 'never' }], ['list']],

  use: {
    /* URL base da aplicação Angular */
    baseURL: 'https://localhost:50905',
    /* Aceitar certificados self-signed do servidor de desenvolvimento */
    ignoreHTTPSErrors: true,
    /* Recolher traces em falhas para debugging */
    trace: 'on-first-retry',
  },

  projects: [
    /* 1 — Setup global: autenticar e guardar estado de sessão */
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },

    /* 2 — Testes Chromium: reutilizam a sessão autenticada */
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        /* Ficheiro de estado de sessão gerado pelo setup */
        storageState: path.join(__dirname, '.auth', 'user.json'),
      },
      dependencies: ['setup'],
    },
  ],
});
