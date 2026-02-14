import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';

const baseUrl = process.env.UI_BASE_URL || 'http://localhost:5119';
const userDataDir = path.resolve('..', '.automation', 'edge-profile');
const evidenceDir = path.resolve('..', '.automation', 'reports', 'edge-mcp-20260214-153325');
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
  await page.waitForTimeout(2000);

  const bodyText = await page.locator('body').innerText();
  if (!bodyText || bodyText.trim().length < 10) {
    throw new Error('UI body text is unexpectedly empty.');
  }

  const clickables = page.locator('button, [role="button"], a');
  const clickableCount = await clickables.count();
  if (clickableCount > 0) {
    await clickables.first().click({ timeout: 5000 }).catch(() => {});
  }

  await page.screenshot({ path: path.join(evidenceDir, 'ui-home.png'), fullPage: true });
  await context.tracing.stop({ path: path.join(evidenceDir, 'trace.zip') });

  const summary = {
    baseUrl,
    clickableCount,
    title: await page.title(),
    success: true
  };
  fs.writeFileSync(path.join(evidenceDir, 'summary.json'), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify(summary));
} finally {
  await context.close();
}
