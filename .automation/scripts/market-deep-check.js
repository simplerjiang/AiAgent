const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, locale: 'zh-CN' });
  const page = await context.newPage();
  
  await page.goto('http://localhost:5119/?tab=stock-info', { waitUntil: 'networkidle', timeout: 30000 });
  await page.waitForTimeout(3000);

  // Deep check 1: Color class verification
  console.log('=== COLOR CLASS VERIFICATION ===');
  const cards = await page.locator('.idx-card').all();
  for (const card of cards) {
    const name = await card.locator('.idx-card-name').textContent();
    const change = await card.locator('.idx-card-change');
    const changeText = await change.textContent();
    const classes = await change.getAttribute('class');
    console.log(name.trim().replace(/[📈🌏]/g, '') + ': ' + changeText.trim() + ' -> classes: [' + classes + ']');
  }

  // Deep check 2: Pulse chip color classes
  console.log('\n=== PULSE CHIP COLORS ===');
  const pulseValues = await page.locator('.pulse-chip-value').all();
  for (let i = 0; i < pulseValues.length; i++) {
    const text = await pulseValues[i].textContent();
    const classes = await pulseValues[i].getAttribute('class');
    console.log('Chip ' + i + ': ' + text.trim() + ' -> classes: [' + classes + ']');
  }

  // Deep check 3: Computed colors via getComputedStyle
  console.log('\n=== COMPUTED COLORS ===');
  const riseElem = await page.locator('.idx-card-change.rise').first();
  if (await riseElem.count() > 0) {
    const riseColor = await riseElem.evaluate(el => window.getComputedStyle(el).color);
    console.log('Rise color: ' + riseColor);
  }
  const fallElem = await page.locator('.idx-card-change.fall').first();
  if (await fallElem.count() > 0) {
    const fallColor = await fallElem.evaluate(el => window.getComputedStyle(el).color);
    console.log('Fall color: ' + fallColor);
  }

  // Deep check 4: Chart popup - take screenshots
  console.log('\n=== CHART POPUP TEST ===');
  
  // Click domestic index first
  const szCard = await page.locator('.idx-card').first();
  await szCard.click();
  await page.waitForTimeout(5000);
  
  const chartDialog = await page.locator('.chart-dialog');
  if (await chartDialog.isVisible()) {
    await page.screenshot({ path: 'C:/Users/kong/AiAgent/.automation/chart-popup-minute.png', fullPage: false });
    console.log('Minute chart screenshot saved');

    // Check chart canvas dimensions
    const minuteContainer = await page.locator('.chart-container').first();
    const box = await minuteContainer.boundingBox();
    console.log('Minute chart container size: ' + JSON.stringify(box));

    // Check if klinecharts rendered actual canvas content
    const canvasCount = await page.locator('.chart-container canvas').count();
    console.log('Canvas elements in chart: ' + canvasCount);

    // Switch to K-line
    await page.locator('.chart-tab').nth(1).click();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'C:/Users/kong/AiAgent/.automation/chart-popup-kline.png', fullPage: false });
    console.log('K-line chart screenshot saved');

    // Close
    await page.locator('.chart-dialog-close').click();
    await page.waitForTimeout(500);
  }

  // Deep check 5: Click a global index (test Bug#2 fix thoroughly)
  console.log('\n=== GLOBAL INDEX CHART TEST ===');
  const globalCard = await page.locator('.idx-row--global .idx-card').nth(2); // 日经225
  const globalName = await globalCard.locator('.idx-card-name').textContent();
  console.log('Clicking global index: ' + globalName.trim());
  await globalCard.click();
  await page.waitForTimeout(6000);

  if (await page.locator('.chart-dialog').isVisible()) {
    const dialogTitle = await page.locator('.dialog-index-name').textContent();
    const dialogSymbol = await page.locator('.dialog-index-symbol').textContent();
    console.log('Dialog title: ' + dialogTitle + ' | Symbol: ' + dialogSymbol);
    
    // Check for loading/error states
    const feedback = await page.locator('.chart-feedback').isVisible().catch(() => false);
    if (feedback) {
      const feedbackText = await page.locator('.chart-feedback').textContent();
      const retryBtn = await page.locator('.chart-retry-btn').isVisible().catch(() => false);
      console.log('Feedback: ' + feedbackText + ' | Retry button: ' + retryBtn);
    } else {
      const canvas = await page.locator('.chart-container canvas').count();
      console.log('Chart rendered with ' + canvas + ' canvas elements');
    }
    
    await page.screenshot({ path: 'C:/Users/kong/AiAgent/.automation/chart-popup-global.png', fullPage: false });
    await page.locator('.chart-dialog-close').click();
  } else {
    console.log('WARN: Global index chart dialog did not open');
  }

  // Deep check 6: Expanded detail tray screenshot
  console.log('\n=== DETAIL TRAY TEST ===');
  await page.locator('.expand-toggle').first().click();
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'C:/Users/kong/AiAgent/.automation/market-detail-tray.png', fullPage: false });
  
  // Check detail chip content
  const detailChips = await page.locator('.detail-chip').all();
  for (const chip of detailChips) {
    const title = await chip.locator('.detail-chip-title').textContent().catch(() => '');
    const rows = await chip.locator('.detail-chip-row').allTextContents();
    console.log('Detail: ' + title.trim() + ' -> ' + rows.join(' | '));
  }

  // Deep check 7: Auto-refresh - wait and observe countdown changing
  console.log('\n=== AUTO-REFRESH TEST ===');
  const s1 = await page.locator('.refresh-seconds').textContent();
  await page.waitForTimeout(3000);
  const s2 = await page.locator('.refresh-seconds').textContent();
  console.log('Countdown: ' + s1 + ' -> ' + s2 + ' (should decrease by ~3)');
  const diff = parseInt(s1) - parseInt(s2);
  console.log('Difference: ' + diff + ' seconds (expected ~3)');

  await browser.close();
  console.log('\n=== DONE ===');
})();
