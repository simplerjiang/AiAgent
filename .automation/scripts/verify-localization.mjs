import { chromium } from 'playwright';

const browser = await chromium.launch({ channel: 'msedge', headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });

await page.goto('http://localhost:5119/', { waitUntil: 'networkidle', timeout: 30000 });
console.log('Page loaded. Title:', await page.title());

await page.waitForTimeout(2000);
await page.screenshot({ path: 'c:/Users/kong/AiAgent/.automation/screenshots/01-initial.png' });

// Find search input
const searchInput = await page.$('input[placeholder*="搜索"], input[placeholder*="股票"], input[placeholder*="代码"]');
if (searchInput) {
  console.log('Found search input');
  await searchInput.click();
  await searchInput.fill('sz000021');
  await page.waitForTimeout(1000);
  
  // Look for dropdown suggestion to click
  const suggestion = await page.$('text=深科技');
  if (suggestion) {
    await suggestion.click();
    console.log('Clicked suggestion');
  } else {
    await page.keyboard.press('Enter');
    console.log('Pressed Enter');
  }
  await page.waitForTimeout(3000);
} else {
  console.log('No search input found');
  // Try clicking on any existing stock entry
  const stockLink = await page.$('text=深科技');
  if (stockLink) {
    await stockLink.click();
    console.log('Clicked 深科技 link');
    await page.waitForTimeout(3000);
  }
}

await page.screenshot({ path: 'c:/Users/kong/AiAgent/.automation/screenshots/02-stock-loaded.png' });

// Now look for workbench/research tab
const workbenchTab = await page.$('text=研究') || await page.$('text=工作台') || await page.$('[data-tab*="workbench"]');
if (workbenchTab) {
  await workbenchTab.click();
  console.log('Clicked workbench tab');
  await page.waitForTimeout(3000);
} else {
  console.log('No workbench tab found, checking current view...');
}

await page.screenshot({ path: 'c:/Users/kong/AiAgent/.automation/screenshots/03-workbench.png' });

// Get full page text
const bodyText = await page.textContent('body');
console.log('Body text length:', bodyText.length);

// Check for untranslated English terms in visible text
const badTerms = [
  'CompanyOverview', 'company_overview_analyst', 'CompanyOverviewMcp', 
  'StockKlineMcp', 'tool_error:', 'Degraded'
];
console.log('\n=== Untranslated terms check ===');
for (const t of badTerms) {
  const found = bodyText.includes(t);
  console.log(`  ${found ? 'FAIL' : 'PASS'} - "${t}": ${found ? 'FOUND on page!' : 'not visible'}`);
}

// Check Chinese translations
const goodTerms = ['公司概览', '公司概况', '工具异常', '降级完成', '开始分析', '分析完成', '市场分析', '情绪分析'];
console.log('\n=== Chinese translations check ===');
for (const t of goodTerms) {
  const found = bodyText.includes(t);
  console.log(`  ${found ? 'FOUND' : 'NOT on page'} - "${t}"`);
}

// Check for any remaining English-style identifiers in the visible DOM
const englishPatterns = bodyText.match(/[A-Z][a-z]+[A-Z][a-z]+(?:Mcp|Analyst)/g) || [];
if (englishPatterns.length > 0) {
  console.log('\n=== Suspicious CamelCase terms on page ===');
  englishPatterns.forEach(p => console.log('  WARNING:', p));
} else {
  console.log('\nNo suspicious CamelCase terms found on visible page.');
}

console.log('\nConsole errors:', errors.length);
errors.slice(0, 5).forEach(e => console.log('  ERR:', e.substring(0, 150)));

await browser.close();
