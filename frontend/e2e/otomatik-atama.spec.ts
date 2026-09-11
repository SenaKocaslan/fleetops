import { expect, test } from '@playwright/test';
import {
  API,
  agvSecilebilirOlanaKadarBekle,
  agvSerbestBirak,
  girisYap,
  yetkiliBaslik,
} from './yardimcilar';

const TOHUM_ARACLAR = [
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333',
];

test.describe('Otomatik atama', () => {
  test('havuzdan ata dugmesi bekleyen gorevi bir araca verir', async ({ page }) => {
    await girisYap(page);

    // Uc tohum aracin da acik gorevi kapatiliyor. Onceki surum bu yardimciyi
    // arac kimligi OLMADAN cagiriyordu; Playwright tip denetimi yapmadigi
    // icin fark edilmedi ve hazirlik hic calismadi.
    for (const agvId of TOHUM_ARACLAR) {
      await agvSerbestBirak(page.request, agvId);
    }

    // Havuzda en az bir bekleyen gorev olsun. Bu gorevin kendisinin
    // atanmasi BEKLENMIYOR: onceki kosulardan kalan daha oncelikli gorevler
    // olabilir. Eskiden bunu 1000 oncelikle "siranin onune gecerek"
    // cozuyorduk; oncelik artik 1-10 araliginda.
    const baslik = await yetkiliBaslik(page.request, 'supervisor');
    const lokasyonlar = await page.request
      .get(`${API}/locations`, { headers: baslik })
      .then((y) => y.json());
    const olustur = await page.request.post(`${API}/tasks`, {
      headers: baslik,
      data: {
        fromLocationId: lokasyonlar[0].id,
        toLocationId: lokasyonlar[1].id,
        materialCode: `OTO-${Date.now()}`,
        quantity: 1,
        priority: 5,
      },
    });
    expect(olustur.status()).toBe(201);

    await page.goto('/');
    await agvSecilebilirOlanaKadarBekle(page);
    await page.getByTestId('auto-assign').click();

    const sonuc = page.getByTestId('dispatch-result');
    await expect(sonuc).toContainText('gorev atandi');

    // Sonuc metnindeki ilk atama listede gercekten "Assigned" olmali.
    // Hazirlikta tum acik gorevler kapatildigi icin su an atanmis olan her
    // gorev bu tiklamadan geliyor.
    const eslesme = /([^\s,:]+) -> (AGV-\d+)/.exec((await sonuc.textContent()) ?? '');
    expect(eslesme).not.toBeNull();
    const [, malzeme, agvKodu] = eslesme!;

    await page.getByTestId('task-status-filter').selectOption('Assigned');
    await page.getByTestId('task-search').fill(malzeme);
    await expect(
      page.getByTestId('task-row').filter({ hasText: malzeme }).filter({ hasText: agvKodu }),
    ).toHaveCount(1);
  });

  test('operator havuzdan ata dugmesini gormez', async ({ page }) => {
    await girisYap(page, 'operator');
    await page.goto('/');

    await expect(page.getByTestId('task-search')).toBeVisible();
    await expect(page.getByTestId('auto-assign')).toHaveCount(0);
  });
});
