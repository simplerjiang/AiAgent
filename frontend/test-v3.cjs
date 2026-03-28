const { chromium } = require('playwright');
const fs = require('fs');
const ssDir = 'C:/Users/kong/AiAgent/.automation/screenshots';
const results = [];
function log(msg) { results.push(msg); console.log(msg); }

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
  const page = await ctx.newPage();
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });

  await page.goto('http://localhost:5000', { waitUntil: 'networkidle', timeout: 15000 });
  log('PAGE_LOADED: ' + page.url());

  // Search stock
  await page.fill('input[placeholder*="输入"]', '浦发银行');
  await page.click('button:has-text("查询")');
  await page.waitForTimeout(3000);
  await page.screenshot({ path: ssDir + '/s2-searched.png', fullPage: false });
  log('SEARCH_DONE: ' + page.url());

  // Check workbench tabs
  const tabs = await page.locator('.wb-tab').allTextContents();
  log('TABS: ' + JSON.stringify(tabs));

  // Click history tab
  await page.click('.wb-tab:has-text("历史记录")');
  await page.waitForTimeout(1500);
  const histPanel = await page.locator('.wb-content, .wb-panel').first().innerText().catch(() => 'EMPTY');
  log('HISTORY_CONTENT: ' + histPanel.substring(0, 300));
  await page.screenshot({ path: ssDir + '/s3-history.png', fullPage: false });

  // Click progress tab
  await page.click('.wb-tab:has-text("团队进度")');
  await page.waitForTimeout(1500);
  const progPanel = await page.locator('.wb-content, .wb-panel').first().innerText().catch(() => 'EMPTY');
  log('PROGRESS_CONTENT: ' + progPanel.substring(0, 300));
  const rerunCount = await page.locator('button:has-text("重跑"), button:has-text("🔄 重跑")').count();
  log('RERUN_BUTTONS: ' + rerunCount);
  await page.screenshot({ path: ssDir + '/s4-progress.png', fullPage: false });

  // Click discussion feed tab
  await page.click('.wb-tab:has-text("讨论动态")');
  await page.waitForTimeout(1500);
  const feedPanel = await page.locator('.wb-content, .wb-panel').first().innerText().catch(() => 'EMPTY');
  log('FEED_CONTENT: ' + feedPanel.substring(0, 300));
  
  // Check lifecycle message styling
  const smallLifecycle = await page.locator('.feed-lifecycle, .lifecycle-compact, [class*="lifecycle"]').count();
  log('LIFECYCLE_STYLED: ' + smallLifecycle);
  await page.screenshot({ path: ssDir + '/s5-feed.png', fullPage: false });

  // Click report tab
  await page.click('.wb-tab:has-text("研究报告")');
  await page.waitForTimeout(1500);
  const repPanel = await page.locator('.wb-content, .wb-panel').first().innerText().catch(() => 'EMPTY');
  log('REPORT_CONTENT: ' + repPanel.substring(0, 300));
  await page.screenshot({ path: ssDir + '/s5b-report.png', fullPage: false });

  // Unicode check
  const bodyText = await page.textContent('body');
  const uniMatches = bodyText.match(/\\u[0-9a-fA-F]{4}/g);
  log('UNICODE_ESCAPES: ' + (uniMatches ? uniMatches.length : 0));

  log('CONSOLE_ERRORS: ' + (errs.length > 0 ? JSON.stringify(errs) : 'NONE'));

  await browser.close();

  // Write results
  fs.writeFileSync('C:/Users/kong/AiAgent/.automation/screenshots/results.txt', results.join('\n'), 'utf8');
  log('ALL_DONE');
})().catch(e => { console.error('FATAL:', e.message); process.exit(1); });