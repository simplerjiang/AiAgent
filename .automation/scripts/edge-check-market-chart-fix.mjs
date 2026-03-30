import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';

const baseUrl = 'http://localhost:5119';
const userDataDir = path.resolve('.automation', 'edge-profile');
const evidenceDir = path.resolve('.automation', 'reports', 'market-bar-chart-fix');
fs.mkdirSync(evidenceDir, { recursive: true });

const results = [];
const log = (msg) => { console.log(msg); results.push(msg); };

const context = await chromium.launchPersistentContext(userDataDir, {
  channel: 'msedge',
  headless: true,
  viewport: { width: 1440, height: 900 }
});

try {
  const page = context.pages()[0] ?? await context.newPage();
  const consoleErrors = [];
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });

  // Navigate to stock-info tab
  await page.goto(`${baseUrl}/?tab=stock-info`, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);
  await page.screenshot({ path: path.join(evidenceDir, '01-initial-load.png'), fullPage: true });

  // ============ T1: Check market bar exists with Chinese names ============
  const marketBarVisible = await page.locator('.market-bar, .market-overview, .stock-top-market').first().isVisible().catch(() => false);
  log(`T1-市场总览带可见: ${marketBarVisible}`);

  // Find index cards - try multiple selectors
  const indexCards = page.locator('.index-card, .market-index-card, [class*="index-card"]');
  const cardCount = await indexCards.count();
  log(`T1-指数卡片数量: ${cardCount}`);
  
  if (cardCount === 0) {
    // Try to find the expand toggle
    const expandBtn = page.locator('button:has-text("展开"), button:has-text("更多"), [class*="expand"]');
    if (await expandBtn.count() > 0) {
      await expandBtn.first().click();
      await page.waitForTimeout(1000);
    }
  }

  // Take a mid-page screenshot to see market bar
  await page.screenshot({ path: path.join(evidenceDir, '02-market-bar.png'), fullPage: true });

  // ============ T2: Click first index card to open chart popup ============
  // Locate clickable index items
  const clickableIndex = page.locator('.index-card, .market-index-card, [class*="index"][class*="card"], [class*="index-item"]').first();
  const hasClickable = await clickableIndex.count() > 0;
  log(`T2-可点击指数卡片存在: ${hasClickable}`);

  if (hasClickable) {
    await clickableIndex.click({ timeout: 5000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(evidenceDir, '03-chart-popup-opened.png'), fullPage: true });

    // ============ T3: Check popup content ============
    const dialogVisible = await page.locator('.chart-dialog, .chart-dialog-overlay').first().isVisible().catch(() => false);
    log(`T3-图表弹窗可见: ${dialogVisible}`);

    // Check active tab
    const activeTabText = await page.locator('.chart-tab.active, .chart-dialog-tabs .active').first().textContent().catch(() => 'N/A');
    log(`T3-当前激活标签: ${activeTabText.trim()}`);

    // Check for blank vs content
    const chartFeedback = await page.locator('.chart-feedback').first().textContent().catch(() => null);
    log(`T3-图表反馈文本: ${chartFeedback ?? '无(正常渲染中)'}`);

    // Check if canvas is rendering
    const canvasCount = await page.locator('.chart-dialog canvas').count();
    log(`T3-弹窗内canvas数量: ${canvasCount}`);

    // Check for loading indicator
    const minuteLoadingVisible = await page.locator('text=分时数据加载中').isVisible().catch(() => false);
    log(`T3-分时加载提示: ${minuteLoadingVisible}`);

    // Check K-line chart visible
    const klineContainerVisible = await page.locator('.chart-dialog .chart-container').first().isVisible().catch(() => false);
    log(`T3-图表容器可见: ${klineContainerVisible}`);

    // ============ T4: Switch to minute tab ============
    const minuteTab = page.locator('.chart-tab:has-text("分时")');
    if (await minuteTab.count() > 0) {
      await minuteTab.click();
      await page.waitForTimeout(1500);
      await page.screenshot({ path: path.join(evidenceDir, '04-minute-tab.png'), fullPage: true });
      
      const minuteFeedback = await page.locator('.chart-feedback').first().textContent().catch(() => null);
      const minuteCanvas = await page.locator('.chart-dialog canvas').count();
      log(`T4-切换分时后反馈: ${minuteFeedback ?? '无反馈文本(有图表)'}`);
      log(`T4-切换分时后canvas: ${minuteCanvas}`);
    }

    // ============ T5: Switch back to K-line tab ============
    const klineTab = page.locator('.chart-tab:has-text("日K")');
    if (await klineTab.count() > 0) {
      await klineTab.click();
      await page.waitForTimeout(1000);
      await page.screenshot({ path: path.join(evidenceDir, '05-kline-tab.png'), fullPage: true });
      const klineCanvas = await page.locator('.chart-dialog canvas').count();
      log(`T5-日K标签canvas: ${klineCanvas}`);
    }

    // ============ T6: Close and reopen ============
    const closeBtn = page.locator('.chart-dialog-close, button:has-text("✕")').first();
    if (await closeBtn.count() > 0) {
      await closeBtn.click();
      await page.waitForTimeout(500);
      const dialogGone = !(await page.locator('.chart-dialog-overlay').isVisible().catch(() => false));
      log(`T6-关闭弹窗成功: ${dialogGone}`);
      
      // Reopen
      await clickableIndex.click({ timeout: 5000 });
      await page.waitForTimeout(2000);
      await page.screenshot({ path: path.join(evidenceDir, '06-reopen.png'), fullPage: true });
      const reopenedVisible = await page.locator('.chart-dialog').first().isVisible().catch(() => false);
      log(`T6-重新打开成功: ${reopenedVisible}`);

      // Close again
      await closeBtn.click().catch(() => {});
      await page.waitForTimeout(500);
    }

    // ============ T7: Test a different index (second card) ============
    const secondIndex = page.locator('.index-card, .market-index-card, [class*="index"][class*="card"], [class*="index-item"]').nth(1);
    if (await secondIndex.count() > 0) {
      await secondIndex.click({ timeout: 5000 });
      await page.waitForTimeout(2500);
      await page.screenshot({ path: path.join(evidenceDir, '07-second-index.png'), fullPage: true });
      
      const secondActiveTab = await page.locator('.chart-tab.active').first().textContent().catch(() => 'N/A');
      const secondCanvasCount = await page.locator('.chart-dialog canvas').count();
      log(`T7-第二指数激活标签: ${secondActiveTab.trim()}`);
      log(`T7-第二指数canvas数: ${secondCanvasCount}`);

      await page.locator('.chart-dialog-close').first().click().catch(() => {});
      await page.waitForTimeout(500);
    }
  }

  // ============ T8: Check market pulse chips ============
  const pulseChips = page.locator('[class*="pulse"], [class*="chip"], [class*="badge"]');
  const pulseCount = await pulseChips.count();
  log(`T8-脉搏芯片数量: ${pulseCount}`);

  // ============ T9: Console errors ============
  log(`T9-控制台错误数: ${consoleErrors.length}`);
  if (consoleErrors.length > 0) {
    consoleErrors.slice(0, 5).forEach((e, i) => log(`  错误${i+1}: ${e.substring(0, 120)}`));
  }

  // Take final screenshot
  await page.screenshot({ path: path.join(evidenceDir, '08-final-state.png'), fullPage: true });

  // Write summary
  const summary = results.join('\n');
  fs.writeFileSync(path.join(evidenceDir, 'test-results.txt'), summary);
  console.log('\n=== TEST SUMMARY ===');
  console.log(summary);

} finally {
  await context.close();
}
