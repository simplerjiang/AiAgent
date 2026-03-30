import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require('../../frontend/node_modules/playwright');

const b = await chromium.launch({ channel: 'msedge', headless: false });
const p = await b.newPage({ viewport: { width: 1440, height: 900 } });
const msgs = [];
p.on('console', m => {
  if (m.type() === 'error' || m.type() === 'warning')
    msgs.push('[' + m.type() + '] ' + m.text());
});
p.on('pageerror', e => msgs.push('[PAGE_ERROR] ' + e.message));

const SS = 'c:/Users/kong/AiAgent/.automation/screenshots';

await p.goto('http://localhost:5119/');
await p.waitForTimeout(3000);
const stockTab = p.getByText('股票信息').first();
if (await stockTab.isVisible()) await stockTab.click();
await p.waitForTimeout(2000);

// ===== A2 deep: check toolbar row =====
console.log('--- A2: Toolbar alignment ---');
// The search input
const searchInput = p.locator('input[placeholder*="股票"]').first();
const searchBox = await searchInput.boundingBox().catch(() => null);
console.log('  Search input box:', JSON.stringify(searchBox));
// The mode switch btn
const modeBtns = p.locator('.btn.btn-sm.btn-ghost');
const modeBtnCount = await modeBtns.count();
console.log('  .btn.btn-sm.btn-ghost count:', modeBtnCount);
for (let i = 0; i < modeBtnCount; i++) {
  const txt = await modeBtns.nth(i).innerText().catch(() => '?');
  const box = await modeBtns.nth(i).boundingBox().catch(() => null);
  console.log(`  Mode btn ${i}: text="${txt.trim()}", box=`, JSON.stringify(box));
}
// Also check what's actually on the same row as search
const searchRow = searchInput.locator('xpath=ancestor::div[contains(@class,"flex")]').first();
const searchRowChildren = await searchRow.innerText().catch(() => 'n/a');
console.log('  Search row ancestor text:', searchRowChildren.replace(/\n/g, ' | ').substring(0, 200));

// ===== Search stock =====
console.log('\n--- Search 600519 ---');
await searchInput.fill('600519');
await p.waitForTimeout(500);
const queryBtn = p.locator('button').filter({ hasText: '查询' }).first();
await queryBtn.click();
await p.waitForTimeout(6000);
await p.screenshot({ path: `${SS}/phase2-after-search.png`, fullPage: true });

// Check canvas
const canvasCount = await p.locator('canvas').count();
console.log('  Canvas count:', canvasCount);

// Check chart containers
const chartContainers = await p.locator('[class*=chart], [class*=kline], [class*=terminal]').count();
console.log('  Chart/terminal containers:', chartContainers);
// Check if chart loaded or still placeholder
const bodyText = await p.innerText('body');
const hasChartPlaceholder = bodyText.includes('等待加载股票') || bodyText.includes('查询股票后');
const hasStockName = bodyText.includes('贵州茅台') || bodyText.includes('600519');
console.log('  Still showing placeholder:', hasChartPlaceholder);
console.log('  Stock name visible:', hasStockName);

// ===== C9: Drag handle between summary and chart =====
console.log('\n--- C9: Horizontal drag handle ---');
// Check for elements with resize cursor styles
const allElements = await p.evaluate(() => {
  const results = [];
  document.querySelectorAll('*').forEach(el => {
    const style = getComputedStyle(el);
    if (style.cursor === 'row-resize' || style.cursor === 'ns-resize' || style.cursor === 'col-resize') {
      results.push({ tag: el.tagName, cls: el.className.substring(0, 80), cursor: style.cursor });
    }
  });
  return results;
});
console.log('  Elements with resize cursor:', JSON.stringify(allElements));

// Check for draggable elements
const dragElements = await p.locator('[draggable], [class*=drag], [class*=grip]').count();
console.log('  Draggable/grip elements:', dragElements);
// Check the splitter grip
const gripEls = await p.locator('[class*=grip]').count();
console.log('  Grip elements:', gripEls);
for (let i = 0; i < gripEls; i++) {
  const box = await p.locator('[class*=grip]').nth(i).boundingBox().catch(() => null);
  const parent = await p.locator('[class*=grip]').nth(i).locator('xpath=ancestor::*[contains(@class,"splitter")]').first().getAttribute('class').catch(() => 'none');
  console.log(`  Grip ${i}: box=`, JSON.stringify(box), 'parent class:', parent);
}

// ===== D10: News tab with stock =====
console.log('\n--- D10: News tab content ---');
const newsTab = p.getByText('新闻影响').first();
await newsTab.click();
await p.waitForTimeout(2000);
await p.screenshot({ path: `${SS}/phase2-news-with-stock.png`, fullPage: true });
const newsBody = await p.innerText('body');
// Check for specific news panel sections
const newsKeywords = ['市场新闻', '个股新闻', 'Market News', 'Stock News', '影响分析', '新闻影响'];
for (const kw of newsKeywords) {
  console.log(`  Contains "${kw}":`, newsBody.includes(kw));
}

// ===== Take other tab screenshots =====
const aiTab = p.getByText('AI 分析').first();
await aiTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: `${SS}/phase2-ai-with-stock.png`, fullPage: true });

const overviewTab = p.getByText('全局总览').first();
await overviewTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: `${SS}/phase2-overview-tab.png`, fullPage: true });

const planTab = p.getByText('交易计划').first();
await planTab.click();
await p.waitForTimeout(1500);
await p.screenshot({ path: `${SS}/phase2-plan-tab.png`, fullPage: true });

// Console errors
console.log('\n--- E13: Console errors ---');
const errors = msgs.filter(m => m.startsWith('[error]') || m.startsWith('[PAGE_ERROR]'));
console.log('  Errors:', errors.length);
errors.forEach(e => console.log('  ', e.substring(0, 250)));
const warnings = msgs.filter(m => m.startsWith('[warning]'));
console.log('  Warnings:', warnings.length);
warnings.slice(0, 5).forEach(w => console.log('  ', w.substring(0, 200)));

await b.close();
console.log('\n=== Focused QA Complete ===');
