import { test as setup } from '@playwright/test';
import path from 'path';

/**
 * auth.setup.ts
 *
 * Setup global do Playwright: autentica o utilizador de teste e guarda o
 * estado da sessão (cookies ASP.NET Identity + localStorage Angular) em
 * .auth/user.json para ser reutilizado por todos os testes.
 *
 * Este ficheiro chama o endpoint exclusivo de testes:
 *   POST https://localhost:7277/api/e2e/login
 *
 * O endpoint só existe quando:
 *   - ASPNETCORE_ENVIRONMENT = Development
 *   - E2ETesting:Enabled = true (appsettings.Development.json)
 *
 * Em Production o endpoint devolve 404 e este setup falhará imediatamente,
 * impedindo que os testes E2E corram contra um ambiente real.
 */

const authFile = path.join(__dirname, '..', '.auth', 'user.json');
const apiBase = 'https://localhost:7277';
const appBase = 'https://localhost:50905';

setup('authenticate via test endpoint', async ({ page }) => {
  // 1. Navegar primeiro para o domínio do Angular para estabelecer contexto
  //    (necessário para poder definir o localStorage mais tarde)
  await page.goto(`${appBase}/login`, { waitUntil: 'domcontentloaded' });

  // 2. Chamar o endpoint E2E de login através do contexto do browser.
  //    O cookie de autenticação do ASP.NET Identity fica registado no
  //    contexto do Playwright (SameSite=None; Secure).
  const response = await page.request.post(`${apiBase}/api/e2e/login`, {
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok()) {
    throw new Error(
      `[E2E Setup] O endpoint de login de testes falhou com status ${response.status()}. ` +
      `Confirma que o backend está a correr em modo Development com E2ETesting:Enabled=true.`
    );
  }

  const user = await response.json() as {
    nome: string;
    sobrenome: string;
    userName: string;
    id: string;
    email: string;
  };

  // 3. Definir o localStorage do Angular com os dados do utilizador,
  //    tal como o componente de login faz após autenticação bem-sucedida.
  await page.evaluate((u) => {
    localStorage.setItem('user_nome', u.nome);
    localStorage.setItem('userName', u.userName);
    localStorage.setItem('user_id', u.id);
  }, user);

  // 4. Guardar o estado completo da sessão (cookies + localStorage)
  //    para ser reutilizado por todos os testes que dependem deste setup.
  await page.context().storageState({ path: authFile });

  console.log(`[E2E Setup] Sessão guardada em ${authFile} para o utilizador ${user.email}`);
});
