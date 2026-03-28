const { chromium } = require('playwright');
const fs = require('fs');
const ssDir = 'C:/Users/kong/AiAgent/.automation/screenshots';
const R = [];
function log(msg) { R.push(msg); console.log(msg); }

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
  const page = await ctx.newPage();
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });

  await page.goto('http://localhost:5000', { waitUntil: 'networkidle', timeout: 15000 });

  // Click 深科技 chip
  await page.click('button.history-chip:has-text("深科技")');
  await page.waitForTimeout(5000);

  // Scroll down to the workbench area
  const wb = page.locator('.trading-workbench, .wb-container, [class*="workbench"]').first();
  
  // Click history tab
  await page.click('.wb-tab:has-text("历史记录")');
  await page.waitForTimeout(3000);
  
  // Take a focused screenshot of just the workbench
  const wbEl = await page.locator('.trading-workbench, .research-workbench, [class*="workbench"]').first().elementHandle();
  if (wbEl) {
    await wbEl.screenshot({ path: ssDir + '/zoom-history.png' });
    log('Zoomed history screenshot saved');
  }
  
  // Get the history tab's actual HTML for analysis
  const histHTML = await page.evaluate(() => {
    const panels = document.querySelectorAll('.wb-content, .wb-panel, [class*="history"]');
    let html = '';
    panels.forEach(p => { html += p.outerHTML.substring(0, 2000) + '\n---\n'; });
    return html;
  });
  log('HISTORY_HTML: ' + histHTML.substring(0, 3000));

  // Click progress tab and take zoomed screenshot
  await page.click('.wb-tab:has-text("团队进度")');
  await page.waitForTimeout(2000);
  if (wbEl) {
    await wbEl.screenshot({ path: ssDir + '/zoom-progress.png' });
    log('Zoomed progress screenshot saved');
  }

  // Click feed tab and take zoomed screenshot  
  await page.click('.wb-tab:has-text("讨论动态")');
  await page.waitForTimeout(2000);
  if (wbEl) {
    await wbEl.screenshot({ path: ssDir + '/zoom-feed.png' });
    log('Zoomed feed screenshot saved');
  }
  
  // Check feed HTML for lifecycle message class names
  const feedHTML = await page.evaluate(() => {
    const el = document.querySelector('.feed-container, .wb-content');
    return el ? el.innerHTML.substring(0, 3000) : 'NOT_FOUND';
  });
  log('FEED_HTML: ' + feedHTML.substring(0, 2000));

  // Click report tab and take zoomed screenshot
  await page.click('.wb-tab:has-text("研究报告")');
  await page.waitForTimeout(2000);
  if (wbEl) {
    await wbEl.screenshot({ path: ssDir + '/zoom-report.png' });
    log('Zoomed report screenshot saved');
  }

  log('CONSOLE_ERRORS: ' + JSON.stringify(errs));

  await browser.close();
  fs.writeFileSync(ssDir + '/results3.txt', R.join('\n'), 'utf8');
  log('DONE');
})().catch(e => { console.error('FATAL:', e.message); process.exit(1); });