import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require('../../frontend/node_modules/playwright');

// Helper to safely get bounding box
async function safeBox(locator) {
  try { return await locator.boundingBox({ timeout: 3000 }); } catch { return null; }
}

const b = await chromium.launch({ channel: 'msedge', headless: false });
const p = await b.newPage({ viewport: { width: 1440, height: 900 } });
const msgs = [];
p.on('console', m => {
  if (m.type() === 'error' || m.type() === 'warning')
    msgs.push('[' + m.type() + '] ' + m.text());
});
p.on('pageerror', e => msgs.push('[PAGE_ERROR] ' + e.message));

await p.goto('http://localhost:5119/');
await p.waitForTimeout(3000);

// Click stock info tab
const stockTab = p.getByText('股票信息').first();
if (await stockTab.isVisible()) await stockTab.click();
await p.waitForTimeout(2000);

// ===== A1: No internal dev identifiers =====
const pageText = await p.innerText('body');
const hasGoal012 = pageText.includes('GOAL-012');
const hasTerminalTitle = pageText.includes('股票信息终端');
const hasLayoutDesc = pageText.includes('左侧聚焦行情与图表');
console.log('--- A1: Internal Dev Identifiers ---');
console.log('  No GOAL-012:', !hasGoal012);
console.log('  No 股票信息终端:', !hasTerminalTitle);
console.log('  No 左侧聚焦行情与图表:', !hasLayoutDesc);

// ===== A2: Top toolbar =====
console.log('\n--- A2: Top Toolbar ---');
const searchInput = p.locator('input').first();
const searchVisible = await searchInput.isVisible().catch(() => false);
console.log('  Search input visible:', searchVisible);
// Dump all inputs for diagnosis
const inputInfos = await p.locator('input').evaluateAll(els => els.map(el => ({ type: el.type, ph: el.placeholder, cls: el.className.substring(0, 80), vis: el.offsetParent !== null })));
console.log('  All inputs:', JSON.stringify(inputInfos));
const ghostBtns = await p.locator('.btn.btn-ghost').count();
const ghostSmBtns = await p.locator('.btn.btn-sm.btn-ghost').count();
console.log('  .btn.btn-ghost count:', ghostBtns);
console.log('  .btn.btn-sm.btn-ghost count:', ghostSmBtns);

// Check if search and mode switch are on same row
const searchBox = await safeBox(searchInput);
const modeBtn = p.locator('.btn.btn-ghost').first();
const modeBtnVisible = await modeBtn.isVisible().catch(() => false);
if (searchBox && modeBtnVisible) {
  const modeBtnBox = await safeBox(modeBtn);
  if (modeBtnBox) {
    const sameRow = Math.abs(searchBox.y - modeBtnBox.y) < 30;
    console.log('  Search & mode btn same row:', sameRow, '(y diff:', Math.abs(searchBox.y - modeBtnBox.y), ')');
  }
}

// ===== A3: Market Overview =====
console.log('\n--- A3: Market Overview ---');
const marketKeywords = ['Market Overview', '市场概览', '市场总览', '大盘', '指数'];
const hasMarket = marketKeywords.some(k => pageText.includes(k));
console.log('  Market section present:', hasMarket);
// Check for collapse button (chevron or text)
const collapseElements = await p.locator('[class*=collapse], [class*=chevron], svg[class*=chevron]').count();
const expandBtns = await p.locator('button').filter({ hasText: /收起|展开|折叠/ }).count();
console.log('  Collapse-related elements:', collapseElements);
console.log('  Expand/collapse buttons:', expandBtns);
// Also look for clickable header
const clickableHeaders = await p.locator('[class*=cursor-pointer]').count();
console.log('  cursor-pointer elements:', clickableHeaders);

// ===== B4/B5: Two-column layout & Splitter =====
console.log('\n--- B4/B5: Layout & Splitter ---');
const splitterEls = await p.locator('[class*=splitter]').count();
console.log('  Splitter elements:', splitterEls);
// Check splitter HTML for dots
if (splitterEls > 0) {
  const splitterHtml = await p.locator('[class*=splitter]').first().innerHTML();
  const hasDots = splitterHtml.includes('dot') || splitterHtml.includes('circle') || splitterHtml.includes('●') || splitterHtml.includes('•');
  console.log('  Splitter has dots:', hasDots);
  console.log('  Splitter HTML (first 400):', splitterHtml.substring(0, 400));
}

// ===== B6: Right panel tabs =====
console.log('\n--- B6: Right Panel Tabs ---');
const allTabTexts = await p.locator('[role=tab], .tab-btn, .tabs button').allInnerTexts();
console.log('  Tab labels:', JSON.stringify(allTabTexts));
const expectedTabs = ['交易计划', '新闻影响', 'AI 分析', '全局总览'];
for (const t of expectedTabs) {
  const found = allTabTexts.some(label => label.includes(t) || label.replace(/\s+/g, '').includes(t.replace(/\s+/g, '')));
  console.log(`  Tab "${t}" found:`, found);
}

// ===== B7: Tab switching =====
console.log('\n--- B7: Tab Switching ---');

// Switch to 新闻影响
const newsTab = p.getByText('新闻影响').first();
await newsTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: '../.automation/screenshots/phase2-news-tab.png', fullPage: true });
const newsText = await p.innerText('body');
const hasNewsContent = newsText.includes('新闻') || newsText.includes('News');
console.log('  News tab switched OK, has news content:', hasNewsContent);

// Switch to AI 分析
const aiTab = p.getByText('AI 分析').first();
await aiTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: '../.automation/screenshots/phase2-ai-tab.png', fullPage: true });
const aiText = await p.innerText('body');
const hasAI = aiText.includes('AI') || aiText.includes('分析') || aiText.includes('Workbench');
console.log('  AI tab switched OK, has AI content:', hasAI);

// Switch to 全局总览
const overviewTab = p.getByText('全局总览').first();
await overviewTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: '../.automation/screenshots/phase2-overview-tab.png', fullPage: true });

// Switch back to 交易计划
const planTab = p.getByText('交易计划').first();
await planTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: '../.automation/screenshots/phase2-plan-tab.png', fullPage: true });
console.log('  Plan tab (default) switched OK');

// ===== C8/C9: Chart area =====
console.log('\n--- C8: Chart Area ---');
// Search for a stock to render the chart
const searchField = p.locator('input').first();
await searchField.fill('600519');
await p.waitForTimeout(300);
const searchBtn = p.getByText('查询').first();
if (await searchBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
  await searchBtn.click();
} else {
  await searchField.press('Enter');
}
await p.waitForTimeout(6000);
await p.screenshot({ path: '../.automation/screenshots/phase2-with-chart.png', fullPage: true });
const canvasCount = await p.locator('canvas').count();
console.log('  Canvas count after search:', canvasCount);

console.log('\n--- C9: Chart Height Drag Handle ---');
const resizeHandles = await p.locator('[class*=resize], [class*=drag], .divider, [class*=handle]').count();
console.log('  Resize/drag handle elements:', resizeHandles);
// Check for cursor-row-resize style
const rowResizeEls = await p.locator('[style*=cursor][style*=row-resize], [class*=row-resize], [class*=ns-resize]').count();
console.log('  cursor-row-resize elements:', rowResizeEls);

// ===== D10: News tab with stock =====
console.log('\n--- D10: News Tab with stock selected ---');
await newsTab.click();
await p.waitForTimeout(2000);
await p.screenshot({ path: '../.automation/screenshots/phase2-news-with-stock.png', fullPage: true });
const newsStockText = await p.innerText('body');
const hasMarketNews = newsStockText.includes('市场新闻') || newsStockText.includes('Market');
const hasStockNews = newsStockText.includes('个股新闻') || newsStockText.includes('600519');
console.log('  Market news panel:', hasMarketNews);
console.log('  Stock-specific news:', hasStockNews);

// ===== D11: AI Analysis tab =====
console.log('\n--- D11: AI Analysis Tab ---');
await aiTab.click();
await p.waitForTimeout(2000);
await p.screenshot({ path: '../.automation/screenshots/phase2-ai-with-stock.png', fullPage: true });
const aiStockText = await p.innerText('body');
const hasTradingWorkbench = aiStockText.includes('Workbench') || aiStockText.includes('工作台') || aiStockText.includes('分析');
console.log('  TradingWorkbench visible:', hasTradingWorkbench);

// ===== D12: Plan tab (default active) =====
console.log('\n--- D12: Plan Tab (default) ---');
await planTab.click();
await p.waitForTimeout(1500);
const planText = await p.innerText('body');
const hasPlanContent = planText.includes('计划') || planText.includes('交易');
console.log('  Plan content visible:', hasPlanContent);
// Check if plan tab is active by default (has active class or aria-selected)
const planTabSelected = await planTab.getAttribute('aria-selected').catch(() => null);
const planTabClass = await planTab.getAttribute('class').catch(() => '');
console.log('  Plan tab aria-selected:', planTabSelected);
console.log('  Plan tab class (trunc):', planTabClass?.substring(0, 100));

// ===== E13: Console Errors =====
console.log('\n--- E13: Console Check ---');
const errors = msgs.filter(m => m.startsWith('[error]') || m.startsWith('[PAGE_ERROR]'));
const warnings = msgs.filter(m => m.startsWith('[warning]'));
console.log('  JS Errors:', errors.length);
errors.forEach(e => console.log('    ', e.substring(0, 200)));
console.log('  Warnings:', warnings.length);
warnings.slice(0, 5).forEach(w => console.log('    ', w.substring(0, 200)));

await b.close();
console.log('\n=== Visual QA Complete ===');
