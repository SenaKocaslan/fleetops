import { Page, expect } from '@playwright/test';

// Integration event'lerden sonra sistem ANLIK TUTARLI DEGIL: atama AGV'yi
// mesgul eder, tamamlama serbest birakir - ama ikisi de outbox dagitim
// turundan SONRA gerceklesir. "Hemen olur" varsayan test flaky olur.
export async function agvSecilebilirOlanaKadarBekle(page: Page, kod?: string) {
  await expect(async () => {
    await page.reload();
    const secenekler = page.getByTestId('agv-select').first().locator('option');
    const sayi = kod
      ? await secenekler.filter({ hasText: kod }).count()
      : (await secenekler.count()) - 1; // ilki "AGV secin" yer tutucusu

    expect(sayi).toBeGreaterThan(0);
  }).toPass({ timeout: 30000, intervals: [500, 1000, 2000] });
}

// Gorevi tamamlar; boylece AGV serbest kalir ve sonraki testler kullanabilir.
export async function gorevTamamla(page: Page, malzeme: string) {
  const satir = () => page.getByTestId('task-row').filter({ hasText: malzeme });

  await satir().getByTestId('start').click();
  await satir().getByTestId('complete').click();
  await expect(satir()).toContainText('Completed');
}
