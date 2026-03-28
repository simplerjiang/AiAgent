const { chromium } = require('playwright');
const path = require('path');
const ssDir = 'C:/Users/kong/AiAgent/.automation/screenshots';

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: false });
  const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
  const page = await context.newPage();
  const consoleErrors = [];
  page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });

  console.log('=== STEP 1: Opening page ===');
  await page.goto('http://localhost:5000', { waitUntil: 'networkidle' });
  console.log('Title:', await page.title(), 'URL:', page.url());
  await page.screenshot({ path: ssDir + '/step1-home.png', fullPage: true });

  console.log('\n=== STEP 2: Explore page structure ===');
  const pageText = await page.textContent('body');
  const lines = pageText.split('\n').map(l=>l.trim()).filter(l=>l.length>0);
  console.log('Page text lines (first 60):', lines.slice(0,60).join(' | '));

  // Find all clickable tab-like elements
  const elements = await page.evaluate(() => {
    const result = [];
    document.querySelectorAll('[role="tab"], .tab, .el-tabs__item, button, a, [class*="tab"], [class*="nav"]').forEach(e => {
      const text = e.textContent.trim();
      if (text.length > 0 && text.length < 80) result.push({ tag: e.tagName, text, cls: e.className.substring(0,80), id: e.id });
    });
    return result;
  });
  console.log('\nClickable elements:', JSON.stringify(elements.slice(0, 40), null, 0));

  // Check for stock copilot or research related elements
  const copilotLink = await page.locator('text=/Copilot|研究|智能研究|AI研究/i').first().elementHandle().catch(()=>null);
  if (copilotLink) {
    console.log('\nFound copilot link, clicking...');
    await copilotLink.click();
    await page.waitForTimeout(3000);
    await page.screenshot({ path: ssDir + '/step2-copilot.png', fullPage: true });
    console.log('Copilot page screenshot saved');
  }

  // Check URL query params for tab navigation
  console.log('\nCurrent URL:', page.url());

  console.log('\n=== CONSOLE ERRORS ===');
  console.log(consoleErrors.length > 0 ? JSON.stringify(consoleErrors) : 'None');

  await browser.close();
  console.log('\nDONE');
})().catch(err => { console.error('FATAL:', err.message); process.exit(1); });