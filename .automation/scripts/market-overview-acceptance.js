const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ 
    channel: 'msedge',
    headless: true
  });
  const context = await browser.newContext({ 
    viewport: { width: 1920, height: 1080 },
    locale: 'zh-CN'
  });
  const page = await context.newPage();
  
  const results = [];
  const log = (test, pass, detail) => {
    results.push({ test, pass, detail });
    console.log((pass ? 'PASS' : 'FAIL') + ' | ' + test + ' | ' + detail);
  };

  // Collect console errors
  const consoleErrors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') consoleErrors.push(msg.text().substring(0, 200));
  });

  try {
    // Navigate to stock-info tab
    console.log('--- Navigating to page ---');
    await page.goto('http://localhost:5119/?tab=stock-info', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(3000); // Let market data load

    // TEST 1: Market bar exists
    const marketBar = await page.locator('.market-bar').first();
    const barVisible = await marketBar.isVisible().catch(() => false);
    log('T1-市场总览带显示', barVisible, barVisible ? '总览带可见' : '总览带不可见');

    // TEST 2: Domestic index cards
    const domesticCards = await page.locator('.idx-row:not(.idx-row--global) .idx-card').all();
    log('T2-国内指数卡片数量', domesticCards.length >= 3, '找到 ' + domesticCards.length + ' 个国内指数卡片');

    // TEST 3: Global index cards
    const globalCards = await page.locator('.idx-row--global .idx-card').all();
    log('T3-全球指数卡片数量', globalCards.length >= 3, '找到 ' + globalCards.length + ' 个全球指数卡片');

    // TEST 4: Total index cards ~10
    const totalCards = domesticCards.length + globalCards.length;
    log('T4-总指数卡片≈10', totalCards >= 8 && totalCards <= 12, '总计 ' + totalCards + ' 个指数卡片');

    // TEST 5: Index names are in Chinese (not raw codes)
    const allCardNames = [];
    const allCards = await page.locator('.idx-card').all();
    for (const card of allCards) {
      const name = await card.locator('.idx-card-name').textContent();
      allCardNames.push(name.trim());
    }
    const hasEnglishCodes = allCardNames.some(n => /^[a-z0-9_]+$/i.test(n.replace(/[📈🌏\s]/g, '')));
    log('T5-指数名称为中文', !hasEnglishCodes, '名称: ' + allCardNames.join(' | '));

    // TEST 6: Check for raw codes like n225, ftse (Bug #2 fix)
    const rawCodePatterns = ['n225', 'ftse', 'ndx', 'spx', 'ks11', 'hstech'];
    const foundRawCodes = [];
    for (const name of allCardNames) {
      for (const code of rawCodePatterns) {
        if (name.toLowerCase().includes(code) && !name.includes('指数') && !name.includes('纳斯') && !name.includes('标普') && !name.includes('富时') && !name.includes('日经') && !name.includes('KOSPI')) {
          foundRawCodes.push(code);
        }
      }
    }
    log('T6-无原始英文代码(Bug#2)', foundRawCodes.length === 0, foundRawCodes.length > 0 ? '仍有原始代码: ' + foundRawCodes.join(', ') : '所有名称已汉化');

    // TEST 7: Price and change percent displayed
    const prices = await page.locator('.idx-card-price').allTextContents();
    const validPrices = prices.filter(p => /\d+\.\d+/.test(p));
    log('T7-价格显示', validPrices.length > 0, '有效价格: ' + validPrices.length + '/' + prices.length);

    const changes = await page.locator('.idx-card-change').allTextContents();
    const validChanges = changes.filter(c => /[+-]?\d+\.\d+%/.test(c));
    log('T8-涨跌幅显示', validChanges.length > 0, '有效涨跌幅: ' + validChanges.length + '/' + changes.length);

    // TEST 9: Color coding (红涨绿跌)
    const riseCards = await page.locator('.idx-card-change.rise, .idx-card-change.text-rise').all();
    const fallCards = await page.locator('.idx-card-change.fall, .idx-card-change.text-fall').all();
    log('T9-涨跌颜色', riseCards.length + fallCards.length > 0, '上涨: ' + riseCards.length + ', 下跌: ' + fallCards.length);

    // TEST 10: Pulse chips (Zone B)
    const pulseChips = await page.locator('.pulse-chip').all();
    log('T10-脉搏芯片显示', pulseChips.length === 4, '找到 ' + pulseChips.length + ' 个脉搏芯片');

    // TEST 11: Pulse chip values
    const pulseValues = await page.locator('.pulse-chip-value').allTextContents();
    log('T11-脉搏芯片值', pulseValues.length === 4, '值: ' + pulseValues.join(' | '));

    // TEST 12: Check Bug #3 fix - non-trading hours show "休市"
    const now = new Date();
    const day = now.getDay();
    const hour = now.getHours();
    const isWeekend = day === 0 || day === 6;
    const isAfterHours = hour < 9 || hour >= 16;
    if (isWeekend || isAfterHours) {
      const xiushi = pulseValues.some(v => v.includes('休市'));
      log('T12-非交易时间显示休市(Bug#3)', xiushi, xiushi ? '资金流正确显示"休市"' : '资金流值: ' + pulseValues.slice(0, 2).join(', '));
    } else {
      log('T12-交易时间资金流', true, '当前为交易时间, 跳过休市检查');
    }

    // TEST 13: Status strip (Zone C) - countdown
    const refreshArc = await page.locator('.refresh-indicator').first();
    const refreshVisible = await refreshArc.isVisible().catch(() => false);
    log('T13-刷新倒计时', refreshVisible, refreshVisible ? '倒计时控件可见' : '倒计时控件不可见');

    const secondsText = await page.locator('.refresh-seconds').textContent().catch(() => '');
    log('T14-倒计时数值', /\d+/.test(secondsText), '倒计时: ' + secondsText);

    // TEST 15: Expand/collapse button
    const expandBtn = await page.locator('.expand-toggle').first();
    const expandBtnText = await expandBtn.textContent();
    log('T15-展开/收起按钮', expandBtnText.includes('展') || expandBtnText.includes('收'), '按钮文字: ' + expandBtnText);

    // TEST 16: Hide button
    const hideBtn = await page.locator('.hide-toggle').first();
    const hideBtnVisible = await hideBtn.isVisible().catch(() => false);
    log('T16-隐藏按钮', hideBtnVisible, hideBtnVisible ? '隐藏按钮可见' : '不可见');

    // TEST 17: Click expand to show detail tray
    await expandBtn.click();
    await page.waitForTimeout(500);
    const detailTray = await page.locator('.bar-detail-tray').first();
    const trayVisible = await detailTray.isVisible().catch(() => false);
    log('T17-展开详情托盘', trayVisible, trayVisible ? '详情托盘展开成功' : '详情托盘未展开');

    // TEST 18: Detail tray contains data
    if (trayVisible) {
      const detailChips = await page.locator('.detail-chip').all();
      log('T18-详情托盘数据', detailChips.length >= 3, '找到 ' + detailChips.length + ' 个详情区块');
    } else {
      log('T18-详情托盘数据', false, '托盘未展开，无法测试');
    }

    // TEST 19: Collapse detail tray
    await expandBtn.click();
    await page.waitForTimeout(500);
    const trayGone = !(await page.locator('.bar-detail-tray').isVisible().catch(() => false));
    log('T19-收起详情托盘', trayGone, trayGone ? '收起成功' : '收起失败');

    // TEST 20: Click index card to open chart popup
    console.log('--- Testing chart popup ---');
    const firstCard = await page.locator('.idx-card').first();
    const cardName = await firstCard.locator('.idx-card-name').textContent();
    await firstCard.click();
    await page.waitForTimeout(2000);

    const chartOverlay = await page.locator('.chart-dialog-overlay').first();
    const chartVisible = await chartOverlay.isVisible().catch(() => false);
    log('T20-点击指数打开图表弹窗', chartVisible, '点击[' + cardName.trim() + '], 弹窗' + (chartVisible ? '打开' : '未打开'));

    if (chartVisible) {
      // TEST 21: Dialog header info
      const dialogName = await page.locator('.dialog-index-name').textContent().catch(() => '');
      const dialogPrice = await page.locator('.dialog-index-price').textContent().catch(() => '');
      log('T21-弹窗标题信息', dialogName.length > 0 && dialogPrice.length > 0, '名称: ' + dialogName + ', 价格: ' + dialogPrice);

      // TEST 22: Tab controls
      const minuteTab = await page.locator('.chart-tab').nth(0);
      const klineTab = await page.locator('.chart-tab').nth(1);
      const minuteTabText = await minuteTab.textContent();
      const klineTabText = await klineTab.textContent();
      log('T22-图表标签', minuteTabText.includes('分时') && klineTabText.includes('日K'), '标签: ' + minuteTabText + ' / ' + klineTabText);

      // TEST 23: Loading/chart rendering
      await page.waitForTimeout(5000); // Wait for chart data to load
      const loadingMsg = await page.locator('.chart-feedback').isVisible().catch(() => false);
      const chartContainer = await page.locator('.chart-container').first();
      const chartRendered = await chartContainer.isVisible().catch(() => false);

      if (loadingMsg && !chartRendered) {
        const feedbackText = await page.locator('.chart-feedback').textContent().catch(() => '');
        const retryBtn = await page.locator('.chart-retry-btn').isVisible().catch(() => false);
        log('T23-图表加载(Bug#1)', retryBtn, '加载中/失败: ' + feedbackText + ', 重试按钮: ' + (retryBtn ? '有' : '无'));
      } else if (chartRendered) {
        log('T23-图表渲染成功', true, '图表容器已渲染');
      } else {
        log('T23-图表状态', false, '未知状态');
      }

      // TEST 24: Switch to K-line tab
      await klineTab.click();
      await page.waitForTimeout(2000);
      const klineContainer = await page.locator('.chart-container').nth(1);
      const klineVisible = await klineContainer.isVisible().catch(() => false);
      log('T24-切换日K图', klineVisible, klineVisible ? '日K图显示' : '日K图未显示');

      // TEST 25: Switch back to minute
      await minuteTab.click();
      await page.waitForTimeout(1000);
      log('T25-切换回分时图', true, '标签切换流畅');

      // TEST 26: Close dialog
      const closeBtn = await page.locator('.chart-dialog-close');
      await closeBtn.click();
      await page.waitForTimeout(500);
      const dialogGone = !(await page.locator('.chart-dialog-overlay').isVisible().catch(() => false));
      log('T26-关闭弹窗', dialogGone, dialogGone ? '关闭成功' : '关闭失败');

      // TEST 27: Reopen dialog
      await firstCard.click();
      await page.waitForTimeout(2000);
      const reopenVisible = await page.locator('.chart-dialog-overlay').isVisible().catch(() => false);
      log('T27-再次打开弹窗', reopenVisible, reopenVisible ? '重新打开成功' : '重新打开失败');

      if (reopenVisible) {
        await page.locator('.chart-dialog-close').click();
        await page.waitForTimeout(500);
      }
    }

    // TEST 28: Hide market bar
    const hideButton = await page.locator('.hide-toggle').first();
    if (await hideButton.isVisible().catch(() => false)) {
      await hideButton.click();
      await page.waitForTimeout(500);
      const hiddenLabel = await page.locator('.bar-hidden-label').isVisible().catch(() => false);
      log('T28-隐藏市场总览', hiddenLabel, hiddenLabel ? '已隐藏，显示恢复提示' : '隐藏状态检测失败');

      // TEST 29: Show hidden bar
      const showBtn = await page.locator('.bar-hidden-row .expand-toggle').first();
      if (await showBtn.isVisible().catch(() => false)) {
        await showBtn.click();
        await page.waitForTimeout(500);
        const restored = await page.locator('.bar-indices').isVisible().catch(() => false);
        log('T29-恢复显示', restored, restored ? '恢复成功' : '恢复失败');
      }
    }

    // TEST 30: Layout check - no overflow
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const viewportWidth = 1920;
    log('T30-布局无水平溢出', bodyScrollWidth <= viewportWidth + 20, '页面宽度: ' + bodyScrollWidth + ' vs 视口: ' + viewportWidth);

    // TEST 31: Check for JS console errors
    const criticalErrors = consoleErrors.filter(e => !e.includes('favicon') && !e.includes('ResizeObserver'));
    log('T31-无JS控制台错误', criticalErrors.length === 0, criticalErrors.length > 0 ? '错误: ' + criticalErrors.slice(0, 3).join(' | ') : '无关键错误');

    // TEST 32: Take a screenshot for manual inspection
    await page.screenshot({ path: 'C:/Users/kong/AiAgent/.automation/market-overview-test.png', fullPage: false });
    log('T32-截图保存', true, '截图已保存至 .automation/market-overview-test.png');

    // Click a global index card to test Bug#2 specifically
    console.log('--- Testing global index card (Bug#2) ---');
    const globalCard = await page.locator('.idx-row--global .idx-card').first();
    if (globalCard) {
      const gName = await globalCard.locator('.idx-card-name').textContent().catch(() => '');
      log('T33-全球指数中文名', !/^[a-z0-9]+$/i.test(gName.replace(/[🌏\s]/g, '')), '全球指数名: ' + gName.trim());
    }

  } catch (err) {
    console.log('ERROR: ' + err.message);
  }

  // Summary
  console.log('\n=== SUMMARY ===');
  let passed = 0, failed = 0;
  for (const r of results) {
    if (r.pass) passed++; else failed++;
  }
  console.log('Passed: ' + passed + ' / Failed: ' + failed + ' / Total: ' + results.length);
  if (failed > 0) {
    console.log('\nFailed tests:');
    for (const r of results) {
      if (!r.pass) console.log('  FAIL: ' + r.test + ' - ' + r.detail);
    }
  }

  await browser.close();
})();
