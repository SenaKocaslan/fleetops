import { expect, test } from '@playwright/test';
import { girisYap } from './yardimcilar';

test.describe('Kimlik ve roller', () => {
  test('giris yapmadan korumali sayfa acilmaz', async ({ page }) => {
    await page.goto('/');

    await expect(page).toHaveURL(/\/giris$/);
    await expect(page.getByTestId('login-form')).toBeVisible();
    // Gezinme cubugu da gizli: yetkisiz kullaniciya menü gostermek yaniltici.
    await expect(page.getByTestId('nav-tasks')).toHaveCount(0);
  });

  test('yanlis parola hata mesaji gosterir', async ({ page }) => {
    await page.goto('/giris');
    await page.getByTestId('login-user').fill('supervisor');
    await page.getByTestId('login-password').fill('yanlis');
    await page.getByTestId('login-submit').click();

    await expect(page.getByTestId('login-hata')).toContainText('hatali');
    await expect(page).toHaveURL(/\/giris$/);
  });

  test('supervisor girisi gorev acma formunu gosterir', async ({ page }) => {
    await girisYap(page, 'supervisor');

    await expect(page.getByTestId('oturum-bilgisi')).toContainText('Supervisor');
    await expect(page.getByTestId('task-form')).toBeVisible();
  });

  test('operator gorev acma formunu gormez', async ({ page }) => {
    await girisYap(page, 'operator');

    await expect(page.getByTestId('oturum-bilgisi')).toContainText('Operator');
    await expect(page.getByTestId('task-form')).toHaveCount(0);
    await expect(page.getByTestId('agv-select')).toHaveCount(0);
    // Okuma yetkisi var: liste gorunmeye devam etmeli.
    await expect(page.getByTestId('task-table')).toBeVisible();
  });

  test('cikis oturumu bitirir ve giris ekranina dondurur', async ({ page }) => {
    await girisYap(page);

    await page.getByTestId('cikis').click();

    await expect(page).toHaveURL(/\/giris$/);
    await page.goto('/filo');
    await expect(page).toHaveURL(/\/giris$/);
  });

  test('oturum sayfa yenilendikten sonra korunur', async ({ page }) => {
    await girisYap(page);

    await page.reload();

    await expect(page.getByTestId('oturum-bilgisi')).toBeVisible();
    await expect(page.getByTestId('task-table')).toBeVisible();
  });
});
