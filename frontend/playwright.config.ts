import { defineConfig, devices } from '@playwright/test';

// E2E, tam sistemi ayaga kaldirir: Angular + API + gercek PostgreSQL.
// Veritabani "docker compose up -d" ile calisir olmalidir; migration'lar
// ayri bir adimdir (uygulama acilista migrate etmez).
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,

  // Makine bellek sinirli; paralel worker sayisi dusuk tutuluyor.
  workers: 1,
  reporter: process.env['CI'] ? 'github' : 'list',

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  webServer: [
    {
      command:
        'dotnet run --project ../backend/src/FleetOps.Api --urls http://localhost:5199',
      url: 'http://localhost:5199/health',
      timeout: 180_000,
      reuseExistingServer: !process.env['CI'],
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
    },
    {
      command: 'npm start',
      url: 'http://localhost:4200',
      timeout: 180_000,
      reuseExistingServer: !process.env['CI'],
    },
  ],
});
