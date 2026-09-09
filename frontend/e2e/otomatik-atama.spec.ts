import { expect, test } from '@playwright/test';
import { API, agvSerbestBirak, girisYap, yetkiliBaslik } from './yardimcilar';

test.describe('Otomatik atama', () => {
  test('havuzdan ata dugmesi bekleyen gorevi bir araca verir', async ({ page }) => {
    await girisYap(page);
    await agvSerbestBirak(page.request);

    // Kendi gorevimizi ureten kod: havuzda baska testlerden kalan gorevler
    // olabilir, o yuzden "atandi mi" sorusunu kendi kodumuz uzerinden
    // soruyoruz, toplam sayilar uzerinden degil.
    const malzeme = `OTO-${Date.now()}`;
    const baslik = await yetkiliBaslik(page.request, 'supervisor');
    const lokasyonlar = await page.request
      .get(`${API}/locations`, { headers: baslik })
      .then((y) => y.json());

    // Havuzda onceki kosulardan kalan 99 oncelikli gorevler var ve onlar
    // daha eski oldugu icin sirada once geliyorlar. Dagitim yalnizca musait
    // arac sayisi kadar gorev alabildiginden, bizim gorevimizin bu turda
    // atanmasi icin havuzun EN ONCELIKLISI olmasi gerekiyor.
    const olustur = await page.request.post(`${API}/tasks`, {
      headers: baslik,
      data: {
        fromLocationId: lokasyonlar[0].id,
        toLocationId: lokasyonlar[1].id,
        materialCode: malzeme,
        quantity: 1,
        priority: 1000,
      },
    });
    expect(olustur.status()).toBe(201);

    await page.goto('/');
    await page.getByTestId('auto-assign').click();

    await expect(page.getByTestId('dispatch-result')).toContainText(malzeme);

    // Listede de gercekten atanmis gorunmeli.
    await page.getByTestId('task-search').fill(malzeme);
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });
    await expect(satir).toContainText('Assigned');
  });

  test('operator havuzdan ata dugmesini gormez', async ({ page }) => {
    await girisYap(page, 'operator');
    await page.goto('/');

    await expect(page.getByTestId('task-search')).toBeVisible();
    await expect(page.getByTestId('auto-assign')).toHaveCount(0);
  });
});
