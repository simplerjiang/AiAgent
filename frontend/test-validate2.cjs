const { chromium } = require('playwright');
const ssDir = 'C:/Users/kong/AiAgent/.automation/screenshots';

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: false });
  const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
  const page = await context.newPage();
  const consoleErrors = [];
  page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });

  await page.goto('http://localhost:5000', { waitUntil: 'networkidle' });

  // STEP 2: Search for stock
  console.log('=== STEP 2: Search for stock ===');
  const searchInput = await page.locator('input[placeholder*="输入"]').first();
  await searchInput.fill('浦发银行');
  await page.waitForTimeout(500);
  
  // Click search button
  const searchBtn = await page.locator('button:has-text("查询")').first();
  await searchBtn.click();
  await page.waitForTimeout(3000);
  await page.screenshot({ path: ssDir + '/step2-searched.png', fullPage: false });
  console.log('Search result screenshot saved');

  // Check if there are search results dropdown or direct load
  const pageUrl = page.url();
  console.log('URL after search:', pageUrl);
  
  // STEP 3: Check 4 tabs in workbench
  console.log('\n=== STEP 3: Verify 4 Tabs ===');
  const wbTabs = await page.locator('.wb-tab').allTextContents();
  console.log('Workbench tabs found:', JSON.stringify(wbTabs));
  
  const hasReport = wbTabs.some(t => t.includes('研究报告'));
  const hasProgress = wbTabs.some(t => t.includes('团队进度'));
  const hasFeed = wbTabs.some(t => t.includes('讨论动态'));
  const hasHistory = wbTabs.some(t => t.includes('历史记录'));
  console.log('Tab check - Report:', hasReport, 'Progress:', hasProgress, 'Feed:', hasFeed, 'History:', hasHistory);

  // Click history tab
  console.log('\nClicking History tab...');
  const historyTab = await page.locator('.wb-tab:has-text("历史记录")').first();
  await historyTab.click();
  await page.waitForTimeout(2000);
  
  // Take screenshot of history panel
  await page.screenshot({ path: ssDir + '/step3-history-tab.png', fullPage: false });
  
  // Get history panel content
  const historyContent = await page.locator('.wb-panel').first().textContent().catch(() => '');
  console.log('History panel content:', historyContent.substring(0, 500));

  // STEP 4: Check team progress tab for re-run buttons
  console.log('\n=== STEP 4: Verify Team Progress & ReRun buttons ===');
  const progressTab = await page.locator('.wb-tab:has-text("团队进度")').first();
  await progressTab.click();
  await page.waitForTimeout(2000);
  await page.screenshot({ path: ssDir + '/step4-progress-tab.png', fullPage: false });
  
  const progressContent = await page.locator('.wb-panel').first().textContent().catch(() => '');
  console.log('Progress panel content:', progressContent.substring(0, 500));
  
  // Check for re-run buttons
  const rerunBtns = await page.locator('button:has-text("重跑"), button:has-text("🔄")').count();
  console.log('Re-run buttons found:', rerunBtns);

  // STEP 5: Check discussion feed tab
  console.log('\n=== STEP 5: Verify Discussion Feed ===');
  const feedTab = await page.locator('.wb-tab:has-text("讨论动态")').first();
  await feedTab.click();
  await page.waitForTimeout(2000);
  await page.screenshot({ path: ssDir + '/step5-feed-tab.png', fullPage: false });
  
  const feedContent = await page.locator('.wb-panel').first().textContent().catch(() => '');
  console.log('Feed panel content:', feedContent.substring(0, 500));

  // Check for lifecycle messages styling
  const lifecycleMsgs = await page.locator('.lifecycle-msg, .feed-lifecycle, [class*="lifecycle"], .compact-msg, .system-msg').count();
  console.log('Lifecycle-styled messages:', lifecycleMsgs);

  // Go back to report tab  
  console.log('\n=== Check Report Tab ===');
  const reportTab = await page.locator('.wb-tab:has-text("研究报告")').first();
  await reportTab.click();
  await page.waitForTimeout(2000);
  await page.screenshot({ path: ssDir + '/step5b-report-tab.png', fullPage: false });
  
  const reportContent = await page.locator('.wb-panel').first().textContent().catch(() => '');
  console.log('Report panel content:', reportContent.substring(0, 500));

  // STEP 6: Console errors
  console.log('\n=== STEP 6: Console Errors ===');
  console.log(consoleErrors.length > 0 ? 'ERRORS:\n' + consoleErrors.join('\n') : 'No JS errors');

  // Check for unicode escapes in page content
  console.log('\n=== Check for Unicode Escapes ===');
  const bodyText = await page.textContent('body');
  const unicodePattern = /\\u[0-9a-fA-F]{4}/g;
  const unicodeMatches = bodyText.match(unicodePattern);
  console.log('Unicode escape sequences found:', unicodeMatches ? unicodeMatches.length : 0);
  if (unicodeMatches) console.log('Samples:', unicodeMatches.slice(0, 5));

  await browser.close();
  console.log('\n=== ALL STEPS COMPLETE ===');
})().catch(err => { console.error('FATAL:', err.message); process.exit(1); });