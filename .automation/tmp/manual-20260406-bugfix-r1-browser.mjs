import { chromium } from 'playwright';

const baseUrl = process.env.BASE_URL || 'http://localhost:5119';
const keyword = 'manual 20260406 bugfix r1';

function decodeUrl(value) {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

async function collectCards(page) {
  return page.evaluate(() => {
    const cards = Array.from(document.querySelectorAll('.archive-card'));
    return cards.map(card => {
      const title = card.querySelector('h3')?.textContent?.trim() || '';
      const rawTitle = card.querySelector('.archive-raw-title')?.textContent?.trim() || '';
      const meta = Array.from(card.querySelectorAll('.archive-meta-row span')).map(node => node.textContent?.trim() || '');
      return { title, rawTitle, meta };
    });
  });
}

async function main() {
  const browser = await chromium.launch({ channel: 'msedge', headless: true }).catch(() => chromium.launch({ headless: true }));
  const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
  const page = await context.newPage();

  const consoleMessages = [];
  const pageErrors = [];
  const processResponses = [];
  const processRequestStarts = [];
  const buttonStates = [];

  page.on('console', msg => {
    if (msg.type() === 'error' || msg.type() === 'warning') {
      consoleMessages.push({ type: msg.type(), text: msg.text() });
    }
  });

  page.on('pageerror', error => {
    pageErrors.push(error.message);
  });

  page.on('request', request => {
    if (request.method() === 'POST' && request.url().includes('/api/news/archive/process-pending')) {
      processRequestStarts.push({ url: request.url(), startedAt: new Date().toISOString() });
    }
  });

  page.on('response', async response => {
    if (response.request().method() === 'POST' && response.url().includes('/api/news/archive/process-pending')) {
      let body = '';
      try {
        body = await response.text();
      } catch (error) {
        body = `__READ_FAILED__:${error.message}`;
      }

      processResponses.push({
        url: response.url(),
        status: response.status(),
        body,
        receivedAt: new Date().toISOString()
      });
    }
  });

  await page.goto(baseUrl, { waitUntil: 'networkidle' });

  const archiveEntry = page.locator('button, a').filter({ hasText: '全量资讯库' }).first();
  await archiveEntry.click();
  await page.waitForSelector('.archive-shell', { timeout: 15000 });

  const keywordInput = page.getByPlaceholder('标题 / 译题 / 代码 / 板块 / 靶点');
  await keywordInput.fill(keyword);

  await Promise.all([
    page.waitForResponse(response => {
      return response.request().method() === 'GET'
        && response.url().includes('/api/news/archive?')
        && decodeUrl(response.url()).includes(keyword);
    }, { timeout: 15000 }),
    page.getByRole('button', { name: '检索' }).click()
  ]);

  await page.waitForLoadState('networkidle');

  const beforeCards = await collectCards(page);
  await page.screenshot({ path: 'screenshots/manual-20260406-bugfix-r1-before.png', fullPage: true });

  const processButton = page.locator('button').filter({ hasText: '批量清洗待处理' }).first();
  buttonStates.push(await processButton.textContent());
  await processButton.click();

  const start = Date.now();
  while (Date.now() - start < 30000) {
    const text = (await processButton.textContent())?.trim() || '';
    const last = buttonStates[buttonStates.length - 1];
    if (text && text !== last) {
      buttonStates.push(text);
    }

    if (text === '🧹 批量清洗待处理') {
      break;
    }

    await page.waitForTimeout(250);
  }

  await page.waitForTimeout(1200);

  const toastMessages = await page.evaluate(() => {
    return Array.from(document.querySelectorAll('.toast-message')).map(node => node.textContent?.trim() || '');
  });

  const afterCards = await collectCards(page);
  await page.screenshot({ path: 'screenshots/manual-20260406-bugfix-r1-after.png', fullPage: true });

  const result = {
    baseUrl,
    keyword,
    beforeCards,
    afterCards,
    buttonStates,
    toastMessages,
    processRequestStarts,
    processResponses,
    consoleMessages,
    pageErrors
  };

  console.log(JSON.stringify(result, null, 2));
  await browser.close();
}

main().catch(error => {
  console.error(error);
  process.exit(1);
});