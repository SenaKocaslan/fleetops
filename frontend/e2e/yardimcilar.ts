import { APIRequestContext, Page, expect } from '@playwright/test';

export const API = 'http://localhost:5199/api';

export const KULLANICILAR = {
  supervisor: { userName: 'supervisor', password: 'Supervisor123!' },
  operator: { userName: 'operator', password: 'Operator123!' },
};

// Sistem eventually consistent: atama/tamamlama AGV durumunu ancak outbox
// dagitim turundan SONRA degistirir. "Hemen olur" varsayan test flaky olur.
export async function agvSecilebilirOlanaKadarBekle(page: Page, kod?: string, malzeme?: string) {
  await expect(async () => {
    // AGV listesi bilesenin kurulusunda cekiliyor, refresh butonu onu
    // tazelemiyor; bu yuzden tam reload gerekiyor.
    await page.reload();

    // Reload arama kutusunu sifirliyor. Aranan gorev oncelige gore sirali
    // havuzda ilk sayfada olmayabilir, filtre yeniden uygulanmali.
    if (malzeme) {
      await page.getByTestId('task-search').fill(malzeme);
      await expect(page.getByTestId('task-row').filter({ hasText: malzeme })).toBeVisible();
    }

    const secenekler = page.getByTestId('agv-select').first().locator('option');
    const sayi = kod
      ? await secenekler.filter({ hasText: kod }).count()
      : (await secenekler.count()) - 1; // ilki "AGV secin" yer tutucusu

    expect(sayi).toBeGreaterThan(0);
  }).toPass({ timeout: 30000, intervals: [500, 1000, 2000] });
}

export async function gorevTamamla(page: Page, malzeme: string) {
  // Sayfalama geldikten sonra gorev ilk sayfada olmayabilir; once filtrele.
  const arama = page.getByTestId('task-search');
  if ((await arama.inputValue()) !== malzeme) {
    await arama.fill(malzeme);
  }

  const satir = () => page.getByTestId('task-row').filter({ hasText: malzeme });
  await expect(satir()).toBeVisible();

  await satir().getByTestId('start').click();
  await satir().getByTestId('complete').click();
  await expect(satir()).toContainText('Completed');
}

// Her senaryo gercek giris akisindan geciyor; token elle uretilmiyor ki
// login bozulursa e2e de kirilsin.
export async function girisYap(page: Page, kim: keyof typeof KULLANICILAR = 'supervisor') {
  const kullanici = KULLANICILAR[kim];

  await page.goto('/giris');
  await page.getByTestId('login-user').fill(kullanici.userName);
  await page.getByTestId('login-password').fill(kullanici.password);
  await page.getByTestId('login-submit').click();

  await expect(page.getByTestId('oturum-bilgisi')).toBeVisible();
}

// page.request tarayicidan bagimsiz; interceptor devrede degil, token elle
// eklenmeli.
export async function yetkiliBaslik(
  request: APIRequestContext,
  kim: keyof typeof KULLANICILAR = 'supervisor',
): Promise<Record<string, string>> {
  const yanit = await request.post(`${API}/auth/login`, { data: KULLANICILAR[kim] });
  const govde = await yanit.json();

  return { Authorization: `Bearer ${govde.token}` };
}

// Onceki kosulardan kalan atanmis gorevler AGV'yi kalici olarak Busy birakiyor
// ve simulator Busy araci bosalttigi icin batarya da dusuyor. Test kendi
// on kosulunu kurmali; "veritabani temiz" varsaymak flaky test demek.
export async function agvSerbestBirak(request: APIRequestContext, agvId: string) {
  const baslik = await yetkiliBaslik(request);

  for (let sayfa = 1; ; sayfa++) {
    const yanit = await request.get(`${API}/tasks?page=${sayfa}&pageSize=100`, {
      headers: baslik,
    });
    const govde = await yanit.json();

    const takilanlar = govde.items.filter(
      (g: { assignedAgvId: string | null; status: string }) =>
        g.assignedAgvId === agvId && (g.status === 'Assigned' || g.status === 'InProgress'),
    );

    for (const gorev of takilanlar) {
      if (gorev.status === 'Assigned') {
        await request.post(`${API}/tasks/${gorev.id}/start`, { headers: baslik });
      }
      await request.post(`${API}/tasks/${gorev.id}/complete`, { headers: baslik });
    }

    if (!govde.hasNext) {
      break;
    }
  }

  // Batarya esigin altina dusmusse arac yine gorev alamaz.
  await request.post(`${API}/agvs/${agvId}/telemetry`, {
    headers: await yetkiliBaslik(request, 'operator'),
    data: { batteryLevel: 90 },
  });
}
