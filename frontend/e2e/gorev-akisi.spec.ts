import { expect, test } from '@playwright/test';

// Walking skeleton'in kanitı: tarayici -> Angular -> HTTP -> API ->
// PostgreSQL -> geri. Zincirin tamami calisiyor mu?
test.describe('Gorev havuzu', () => {
  test('sayfa acilir ve gorev havuzu gorunur', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'FleetOps' })).toBeVisible();
    await expect(page.getByTestId('task-form')).toBeVisible();

    // Yuklemenin BASARIYLA bittigini bekle. Yalnizca "hata yok" demek
    // yaris kosulu yaratir: istek daha donmeden test gecebilir.
    await expect(
      page.getByTestId('task-table').or(page.getByTestId('empty')),
    ).toBeVisible();
    await expect(page.getByTestId('error')).toHaveCount(0);
  });

  test('olusturulan gorev listede gorunur', async ({ page }) => {
    const malzeme = `MLZ-${Date.now()}`;

    await page.goto('/');
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('quantity').fill('4');
    await page.getByTestId('priority').fill('3');
    await page.getByTestId('submit').click();

    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });
    await expect(satir).toBeVisible();
    await expect(satir).toContainText('Pending');
  });

  test('sunucu tarafi hatasi ekranda gosterilir', async ({ page }) => {
    // Sadece bosluk iceren kod, HTML required dogrulamasini gecer ama
    // sunucu Trim() sonrasi reddeder. Yani hata gercekten API'den doner.
    await page.goto('/');
    await page.getByTestId('material-code').fill('   ');
    await page.getByTestId('submit').click();

    await expect(page.getByTestId('error')).toContainText('Malzeme kodu bos olamaz');
  });

  test('istemci gecersiz miktarda gonderime izin vermez', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('material-code').fill('MLZ-TEST');
    await page.getByTestId('quantity').fill('0');

    await expect(page.getByTestId('submit')).toBeDisabled();
  });
});
