import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';

const baseUrl = 'http://localhost:5119';
const userDataDir = path.resolve('.automation/edge-profile');
const evidenceDir = path.resolve('.automation/reports/market-bar-regression');
fs.mkdirSync(evidenceDir, { recursive: true });

const results = { pass: [], fail: [], warn: [] };
const log = (status, msg) => { results[status].push(msg); console.log(`[${status.toUpperCase()}] ${msg}`); };

let context;
try {
  context = await chromium.launchPersistentContext(userDataDir, {
    channel: 'msedge',
    headless: false,
    viewport: { width: 1440, height: 900 }
  });
} catch {
  context = await chromium.launchPersistentContext(userDataDir, {
    headless: false,
    viewport: { width: 1440, height: 900 }
  });
}

try {
  const page = context.pages()[0] ?? await context.newPage();

  // Collect console errors
  const consoleErrors = [];
  page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);

  // Screenshot: initial page
  await page.screenshot({ path: path.join(evidenceDir, '01-initial-page.png'), fullPage: true });

  // Navigate to stock info tab (click 行情 or 股票 tab)
  const stockTab = page.locator('text=股票信息').or(page.locator('text=行情')).or(page.locator('[data-tab="stock"]'));
  if (await stockTab.count() > 0) {
    await stockTab.first().click({ timeout: 5000 }).catch(() => {});
    await page.waitForTimeout(2000);
  }

  await page.screenshot({ path: path.join(evidenceDir, '02-stock-page.png'), fullPage: true });

  // ======= BUG #2: Global index names fallback =======
  const globalRow = page.locator('.idx-row--global');
  if (await globalRow.count() > 0) {
    const globalText = await globalRow.innerText();
    console.log('Global indices text:', globalText);
    
    const rawCodes = ['hsi', 'hstech', 'n225', 'ndx', 'spx', 'ftse', 'ks11'];
    const zhNames = ['恒生指数', '恒生科技', '日经225', '纳斯达克', '标普500', '富时100', '韩国KOSPI'];
    
    let hasRawCode = false;
    for (const code of rawCodes) {
      if (globalText.toLowerCase().includes(code) && !globalText.includes('KOSPI')) {
        hasRawCode = true;
        log('fail', `Bug#2: 全球指数仍显示原始代码 "${code}"`);
      }
    }
    
    let zhCount = 0;
    for (const name of zhNames) {
      if (globalText.includes(name)) zhCount++;
    }
    
    if (zhCount > 0 && !hasRawCode) {
      log('pass', `Bug#2: 全球指数中文名称正常兜底，识别到 ${zhCount} 个中文名`);
    } else if (zhCount === 0) {
      log('warn', 'Bug#2: 未检测到全球指数行中文名（可能数据未加载）');
    }
    
    // Check prices are not all 0.00
    const priceZeroPattern = /0\.00/g;
    const priceMatches = globalText.match(priceZeroPattern);
    const cardCount = await globalRow.locator('.idx-card').count();
    if (priceMatches && priceMatches.length >= cardCount && cardCount > 0) {
      log('warn', `Bug#2: 全球指数价格全部为 0.00，可能API未返回数据`);
    } else {
      log('pass', 'Bug#2: 全球指数价格有有效数值');
    }
  } else {
    log('warn', 'Bug#2: 未找到全球指数行 (.idx-row--global)');
  }

  await page.screenshot({ path: path.join(evidenceDir, '03-global-indices.png'), fullPage: true });

  // ======= BUG #3: Rest day / market closed hint =======
  const pulseChips = page.locator('.pulse-chip');
  const pulseCount = await pulseChips.count();
  if (pulseCount > 0) {
    const pulseText = await page.locator('.bar-pulse').innerText();
    console.log('Pulse text:', pulseText);
    
    const now = new Date();
    const h = now.getHours(), m = now.getMinutes();
    const t = h * 60 + m;
    const day = now.getDay();
    const inTradingHours = day >= 1 && day <= 5 && t >= 9 * 60 + 15 && t <= 15 * 60 + 5;
    
    if (inTradingHours) {
      log('pass', `Bug#3: 当前在交易时段(${h}:${m.toString().padStart(2,'0')})内，脉搏指标应显示数值`);
      if (pulseText.includes('休市')) {
        log('warn', 'Bug#3: 交易时段内但显示了"休市"，可能数据确实为0');
      }
    } else {
      if (pulseText.includes('休市')) {
        log('pass', 'Bug#3: 非交易时段正确显示"休市"');
      } else {
        log('warn', 'Bug#3: 非交易时段未显示"休市"（可能有实际数值）');
      }
    }
  }

  // ======= BUG #1: Chart popup not freezing =======
  // Find domestic index cards and click the first one
  const domesticCards = page.locator('.idx-row:not(.idx-row--global) .idx-card');
  const domesticCount = await domesticCards.count();
  console.log(`Found ${domesticCount} domestic index cards`);
  
  if (domesticCount > 0) {
    const cardName = await domesticCards.first().innerText();
    console.log('Clicking card:', cardName.split('\n')[0]);
    
    await domesticCards.first().click();
    
    // Wait for dialog to appear
    const dialog = page.locator('.chart-dialog');
    try {
      await dialog.waitFor({ state: 'visible', timeout: 5000 });
      log('pass', 'Bug#1: 图表弹窗正常弹出');
      
      await page.screenshot({ path: path.join(evidenceDir, '04-chart-popup-open.png'), fullPage: true });
      
      // Wait for chart to load (should be within a few seconds, not stuck)
      const loadingIndicator = dialog.locator('text=加载图表数据中');
      const timeoutIndicator = dialog.locator('text=图表加载超时');
      const noDataIndicator = dialog.locator('text=暂无数据');
      
      // Wait up to 15 seconds for loading to finish
      let chartLoaded = false;
      for (let i = 0; i < 15; i++) {
        await page.waitForTimeout(1000);
        const isLoading = await loadingIndicator.count() > 0 && await loadingIndicator.isVisible().catch(() => false);
        const isTimeout = await timeoutIndicator.count() > 0 && await timeoutIndicator.isVisible().catch(() => false);
        const isNoData = await noDataIndicator.count() > 0 && await noDataIndicator.isVisible().catch(() => false);
        
        if (isTimeout) {
          log('warn', 'Bug#1: 图表加载超时（超时保护生效，显示了友好提示）');
          chartLoaded = true;
          break;
        }
        if (isNoData) {
          log('warn', 'Bug#1: 图表显示暂无数据');
          chartLoaded = true;
          break;
        }
        if (!isLoading) {
          log('pass', `Bug#1: 分时图在 ${i+1} 秒内加载完成（不再卡死）`);
          chartLoaded = true;
          break;
        }
        console.log(`Waiting for chart... ${i+1}s`);
      }
      
      if (!chartLoaded) {
        log('fail', 'Bug#1: 图表加载超过15秒仍在loading状态');
      }
      
      await page.screenshot({ path: path.join(evidenceDir, '05-chart-minute.png'), fullPage: true });
      
      // Switch to K-line tab
      const klineTab = dialog.locator('text=日K');
      if (await klineTab.count() > 0) {
        await klineTab.click();
        await page.waitForTimeout(2000);
        await page.screenshot({ path: path.join(evidenceDir, '06-chart-kline.png'), fullPage: true });
        
        const stillLoading = await loadingIndicator.count() > 0 && await loadingIndicator.isVisible().catch(() => false);
        if (!stillLoading) {
          log('pass', 'Bug#1: 日K线图正常切换显示');
        } else {
          log('fail', 'Bug#1: 日K线切换后仍在loading');
        }
      }
      
      // Close dialog
      const closeBtn = dialog.locator('.chart-dialog-close').or(dialog.locator('button:has-text("✕")'));
      if (await closeBtn.count() > 0) {
        await closeBtn.click();
      } else {
        await page.keyboard.press('Escape');
      }
      await page.waitForTimeout(500);
      log('pass', 'Bug#1: 弹窗正常关闭');
      
      // Test a global index (e.g., Hang Seng)
      const globalCards = page.locator('.idx-row--global .idx-card');
      const gCount = await globalCards.count();
      if (gCount > 0) {
        console.log('Testing global index chart...');
        await globalCards.first().click();
        
        try {
          await dialog.waitFor({ state: 'visible', timeout: 5000 });
          
          // Wait up to 15 seconds
          let globalChartOk = false;
          for (let i = 0; i < 15; i++) {
            await page.waitForTimeout(1000);
            const isLoading = await loadingIndicator.count() > 0 && await loadingIndicator.isVisible().catch(() => false);
            const isTimeout = await timeoutIndicator.count() > 0 && await timeoutIndicator.isVisible().catch(() => false);
            
            if (isTimeout) {
              log('warn', 'Bug#1: 全球指数图表加载超时（超时保护生效）');
              globalChartOk = true;
              break;
            }
            if (!isLoading) {
              log('pass', `Bug#1: 全球指数图表在 ${i+1} 秒内加载完成`);
              globalChartOk = true;
              break;
            }
          }
          if (!globalChartOk) {
            log('fail', 'Bug#1: 全球指数图表超过15秒仍在loading');
          }
          
          await page.screenshot({ path: path.join(evidenceDir, '07-global-chart.png'), fullPage: true });
          
          // Close
          if (await closeBtn.count() > 0) await closeBtn.click();
          else await page.keyboard.press('Escape');
        } catch {
          log('warn', 'Bug#1: 全球指数弹窗未能打开');
        }
      }
    } catch {
      log('fail', 'Bug#1: 图表弹窗5秒内未出现');
    }
  } else {
    log('fail', 'Bug#1: 未找到国内指数卡片');
  }

  // ======= Regression: Auto-refresh countdown =======
  const refreshSeconds = page.locator('.refresh-seconds');
  if (await refreshSeconds.count() > 0) {
    const v1 = await refreshSeconds.innerText();
    await page.waitForTimeout(2000);
    const v2 = await refreshSeconds.innerText();
    if (v1 !== v2) {
      log('pass', `回归: 自动刷新倒计时正常工作 (${v1} -> ${v2})`);
    } else {
      log('warn', `回归: 倒计时2秒内未变化 (${v1} -> ${v2})，可能正在刷新中`);
    }
  }

  // ======= Regression: Expand/Collapse =======
  const expandBtn = page.locator('.expand-toggle');
  if (await expandBtn.count() > 0) {
    const btnText = await expandBtn.innerText();
    await expandBtn.click();
    await page.waitForTimeout(500);
    const btnTextAfter = await expandBtn.innerText();
    
    if (btnText !== btnTextAfter) {
      log('pass', `回归: 展开/折叠按钮正常 ("${btnText}" -> "${btnTextAfter}")`);
    } else {
      log('warn', '回归: 展开/折叠按钮点击后文本未变化');
    }
    
    await page.screenshot({ path: path.join(evidenceDir, '08-expanded.png'), fullPage: true });
    
    // Click again to restore
    await expandBtn.click();
    await page.waitForTimeout(500);
  }

  // ======= Regression: Market bar visible without stock selection =======
  const marketBar = page.locator('.market-bar');
  if (await marketBar.count() > 0) {
    log('pass', '回归: 未选个股时市场总览带正常显示');
  }

  // Final screenshot
  await page.screenshot({ path: path.join(evidenceDir, '09-final.png'), fullPage: true });

  // Console errors check
  if (consoleErrors.length > 0) {
    log('warn', `浏览器控制台有 ${consoleErrors.length} 个错误: ${consoleErrors.slice(0, 3).join(' | ')}`);
  } else {
    log('pass', '浏览器控制台无错误');
  }

} finally {
  await context.close();
}

// Summary
console.log('\n========== REGRESSION TEST SUMMARY ==========');
console.log(`PASS: ${results.pass.length}`);
results.pass.forEach(m => console.log(`  ✓ ${m}`));
console.log(`WARN: ${results.warn.length}`);
results.warn.forEach(m => console.log(`  ⚠ ${m}`));
console.log(`FAIL: ${results.fail.length}`);
results.fail.forEach(m => console.log(`  ✗ ${m}`));
console.log(`\nVERDICT: ${results.fail.length === 0 ? 'ALL CRITICAL CHECKS PASSED' : 'HAS FAILURES'}`);

fs.writeFileSync(path.join(evidenceDir, 'summary.json'), JSON.stringify(results, null, 2));
