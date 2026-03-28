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

  // Click 深科技 chip from history
  log('=== Clicking 深科技 from history ===');
  const chip = page.locator('button.history-chip:has-text("深科技")');
  if (await chip.count() > 0) {
    await chip.first().click();
    await page.waitForTimeout(5000);
    log('Clicked 深科技 chip, waiting for data...');
  } else {
    // Search manually
    await page.fill('input[placeholder*="输入"]', 'sz000021');
    await page.click('button:has-text("查询")');
    await page.waitForTimeout(5000);
    log('Searched sz000021 manually');
  }
  await page.screenshot({ path: ssDir + '/t2-stock-loaded.png', fullPage: false });

  // Check tabs
  const tabs = await page.locator('.wb-tab').allTextContents();
  log('TABS: ' + JSON.stringify(tabs));

  // ---- HISTORY TAB ----
  log('\n=== HISTORY TAB ===');
  await page.click('.wb-tab:has-text("历史记录")');
  await page.waitForTimeout(3000);
  
  // Get all text in the workbench area
  const histArea = await page.locator('.wb-content').first().innerText().catch(() => 'FETCH_FAILED');
  log('HISTORY: ' + histArea.substring(0, 800));
  await page.screenshot({ path: ssDir + '/t3-history.png', fullPage: false });

  // Check if session items are listed
  const sessionItems = await page.locator('.history-session, .session-item, [class*="session"], [class*="history-item"]').count();
  log('SESSION_ITEMS_COUNT: ' + sessionItems);
  
  // Try broader check
  const histHTML = await page.locator('.wb-content').first().innerHTML().catch(() => '');
  const hasSessionList = histHTML.includes('session') || histHTML.includes('Session') || histHTML.includes('历史') || histHTML.includes('Completed') || histHTML.includes('Failed');
  log('HAS_SESSION_DATA: ' + hasSessionList);

  // ---- PROGRESS TAB ----
  log('\n=== PROGRESS TAB ===');
  await page.click('.wb-tab:has-text("团队进度")');
  await page.waitForTimeout(3000);
  
  const progArea = await page.locator('.wb-content').first().innerText().catch(() => 'FETCH_FAILED');
  log('PROGRESS: ' + progArea.substring(0, 800));
  await page.screenshot({ path: ssDir + '/t4-progress.png', fullPage: false });
  
  // Check for re-run buttons
  const rerunBtns = await page.locator('button:has-text("重跑")').count();
  const rerunEmojiBtns = await page.locator('button:has-text("🔄")').count();
  log('RERUN_TEXT_BTNS: ' + rerunBtns);
  log('RERUN_EMOJI_BTNS: ' + rerunEmojiBtns);
  
  // Check progress HTML for stage items
  const progHTML = await page.locator('.wb-content').first().innerHTML().catch(() => '');
  log('PROGRESS_HTML_SNIPPET: ' + progHTML.substring(0, 1000));

  // ---- FEED TAB ----
  log('\n=== FEED TAB ===');
  await page.click('.wb-tab:has-text("讨论动态")');
  await page.waitForTimeout(3000);
  
  const feedArea = await page.locator('.wb-content').first().innerText().catch(() => 'FETCH_FAILED');
  log('FEED: ' + feedArea.substring(0, 800));
  await page.screenshot({ path: ssDir + '/t5-feed.png', fullPage: false });
  
  // Check for lifecycle messages with compact styling
  const feedHTML = await page.locator('.wb-content').first().innerHTML().catch(() => '');
  const lifecycleCount = (feedHTML.match(/lifecycle/gi) || []).length;
  const compactCount = (feedHTML.match(/compact/gi) || []).length;
  const systemCount = (feedHTML.match(/system-msg|started|completed/gi) || []).length;
  log('FEED_LIFECYCLE_CLASSES: ' + lifecycleCount);
  log('FEED_COMPACT_CLASSES: ' + compactCount);
  log('FEED_SYSTEM_REFS: ' + systemCount);

  // ---- REPORT TAB ----
  log('\n=== REPORT TAB ===');
  await page.click('.wb-tab:has-text("研究报告")');
  await page.waitForTimeout(3000);
  
  const repArea = await page.locator('.wb-content').first().innerText().catch(() => 'FETCH_FAILED');
  log('REPORT: ' + repArea.substring(0, 800));
  await page.screenshot({ path: ssDir + '/t5b-report.png', fullPage: false });

  // Unicode check on page
  const bodyText = await page.textContent('body');
  const uniMatches = bodyText.match(/\\u[0-9a-fA-F]{4}/g);
  log('\nUNICODE_ESCAPES: ' + (uniMatches ? uniMatches.length : 0));

  // Console errors
  log('\nCONSOLE_ERRORS: ' + (errs.length > 0 ? JSON.stringify(errs) : 'NONE'));

  await browser.close();
  fs.writeFileSync(ssDir + '/results2.txt', R.join('\n'), 'utf8');
  log('\n=== DONE ===');
})().catch(e => { console.error('FATAL:', e.message); process.exit(1); });