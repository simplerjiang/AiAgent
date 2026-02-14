import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';

const baseUrl = process.env.UI_BASE_URL || 'http://localhost:5119';
const userDataDir = path.resolve('..', '.automation', 'edge-profile');
const evidenceDir = path.resolve('..', '.automation', 'reports', 'edge-mcp-20260214-154734');
fs.mkdirSync(evidenceDir, { recursive: true });

const context = await chromium.launchPersistentContext(userDataDir, {
  channel: 'msedge',
  headless: true,
  viewport: { width: 1440, height: 900 }
});

try {
  await context.tracing.start({ screenshots: true, snapshots: true });
  const page = context.pages()[0] ?? await context.newPage();
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 45000 });
  await page.waitForTimeout(1200);

  if ((await page.locator('text=股票信息').count()) > 0) {
    const firstHistoryRow = page.locator('.history-table tbody tr').first();
    if (await firstHistoryRow.count()) {
      await firstHistoryRow.click({ timeout: 5000 }).catch(() => {});
      await page.waitForTimeout(1500);
    } else {
      const input = page.locator('input[placeholder="输入股票代码/名称/拼音缩写"]').first();
      if (await input.count()) {
        await input.fill('sz000021').catch(() => {});
        await page.getByRole('button', { name: '查询' }).first().click({ timeout: 5000 }).catch(() => {});
        await page.waitForTimeout(2500);
      }
    }
  }

  const hasKlineHeader = (await page.locator('text=K 线图').count()) > 0;
  const hasMinuteHeader = (await page.locator('text=分时图').count()) > 0;
  const tabCount = await page.locator('.chart-tabs .tab').count();

  if (tabCount > 0) {
    await page.locator('.chart-tabs .tab').nth(1).click({ timeout: 5000 }).catch(() => {});
  }

  const canvasCount = await page.locator('canvas').count();

  await page.screenshot({ path: path.join(evidenceDir, 'ui-goal005.png'), fullPage: true });
  await context.tracing.stop({ path: path.join(evidenceDir, 'trace.zip') });

  const summary = {
    baseUrl,
    hasKlineHeader,
    hasMinuteHeader,
    tabCount,
    canvasCount,
    success: hasKlineHeader && hasMinuteHeader
  };
  fs.writeFileSync(path.join(evidenceDir, 'summary.json'), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify(summary));
} finally {
  await context.close();
}
