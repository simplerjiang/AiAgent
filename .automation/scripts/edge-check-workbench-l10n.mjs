import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'

const baseUrl = 'http://localhost:5119'
const userDataDir = path.resolve('.automation', 'edge-profile')
const evidenceDir = path.resolve('.automation', 'reports', 'workbench-l10n')
fs.mkdirSync(evidenceDir, { recursive: true })

const context = await chromium.launchPersistentContext(userDataDir, {
  channel: 'msedge',
  headless: false,
  viewport: { width: 1440, height: 1000 }
})

const findings = []
const errors = []

function log(msg) { console.log(msg); findings.push(msg) }

try {
  const page = context.pages()[0] ?? (await context.newPage())
  page.on('pageerror', err => errors.push('pageerror:' + err.message))

  // Step 1: Navigate to app
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 30000 })
  await page.waitForTimeout(2000)
  log('[STEP 1] Loaded homepage OK')

  // Step 2: Search for 深科技 (sz000021)
  const symbolInput = page.locator('input[placeholder*="股票"]').first()
  if (await symbolInput.count()) {
    await symbolInput.click()
    await symbolInput.fill('深科技')
    await page.waitForTimeout(800)
    // Try dropdown first, then button
    const dropdown = page.locator('.search-dropdown-item, .autocomplete-item, .el-autocomplete-suggestion__list li').first()
    if (await dropdown.count()) {
      await dropdown.click({ timeout: 3000 })
    } else {
      await page.keyboard.press('Enter')
    }
    await page.waitForTimeout(3000)
    log('[STEP 2] Searched for 深科技')
  } else {
    log('[STEP 2] ERROR: Cannot find stock search input')
  }

  await page.screenshot({ path: path.join(evidenceDir, '01-after-search.png'), fullPage: false })

  // Step 3: Scroll down to find Trading Workbench area
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight))
  await page.waitForTimeout(1500)
  await page.screenshot({ path: path.join(evidenceDir, '02-scrolled-bottom.png'), fullPage: false })

  // Find workbench tabs
  const tabNames = await page.locator('.el-tabs__item, [role="tab"]').allTextContents()
  log('[STEP 3] Tabs found: ' + JSON.stringify(tabNames))

  // Step 4: Click 研究报告 tab
  const reportTab = page.locator('.el-tabs__item:has-text("研究报告"), [role="tab"]:has-text("研究报告")').first()
  if (await reportTab.count()) {
    await reportTab.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
    log('[STEP 4] Clicked 研究报告 tab')
  } else {
    log('[STEP 4] WARNING: No 研究报告 tab found, checking visible content')
  }

  // Take full-page screenshot for report tab
  await page.screenshot({ path: path.join(evidenceDir, '03-report-tab.png'), fullPage: true })

  // BUG-1 Check: Block titles should be Chinese
  const allText = await page.evaluate(() => document.body.innerText)
  
  // Check for English block titles that should be translated
  const englishBlockTitles = ['CompanyOverview', 'TechnicalAnalysis', 'MarketContext', 'NewsAnalysis', 'BullResearch', 'BearResearch']
  const foundEnglishBlocks = englishBlockTitles.filter(t => {
    // Check if appears as a standalone heading/label, not in LLM text
    return false // We'll do visual check via screenshots
  })
  
  // Check specifically for 公司概览 text
  const has公司概览 = allText.includes('公司概览')
  log('[BUG-1] 公司概览 block title present: ' + has公司概览)
  
  // Check for Chinese block titles
  const chineseBlocks = ['公司概览', '技术分析', '市场背景', '新闻分析', '看多研究', '看空研究']
  for (const cb of chineseBlocks) {
    const found = allText.includes(cb)
    log('[BUG-1] Block "' + cb + '": ' + (found ? 'FOUND' : 'not found'))
  }

  // BUG-2 Check: Evidence source labels should be Chinese (not CompanyOverviewMcp)
  const mcpEnglishNames = ['CompanyOverviewMcp', 'TechnicalAnalysisMcp', 'MarketContextMcp', 'NewsAnalysisMcp']
  for (const mcp of mcpEnglishNames) {
    const foundMcp = allText.includes(mcp)
    log('[BUG-2] English MCP "' + mcp + '" in text: ' + (foundMcp ? 'FAIL - still English' : 'OK - translated'))
  }

  // Step 5: Click 团队进度 tab
  const progressTab = page.locator('.el-tabs__item:has-text("团队进度"), [role="tab"]:has-text("团队进度")').first()
  if (await progressTab.count()) {
    await progressTab.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
    log('[STEP 5] Clicked 团队进度 tab')
  } else {
    log('[STEP 5] WARNING: No 团队进度 tab found')
  }

  await page.screenshot({ path: path.join(evidenceDir, '04-progress-tab.png'), fullPage: true })
  
  const progressText = await page.evaluate(() => document.body.innerText)

  // BUG-3 Check: Role names should NOT show snake_case English
  const snakeCaseRoles = ['company_overview_analyst', 'technical_analyst', 'news_analyst', 'bull_researcher', 'bear_researcher', 'market_context_analyst']
  for (const role of snakeCaseRoles) {
    const foundRole = progressText.includes(role)
    log('[BUG-3] snake_case "' + role + '": ' + (foundRole ? 'FAIL - still snake_case' : 'OK - translated'))
  }

  // BUG-4 Check: Status should show Chinese, not "Degraded"
  // Find elements specifically in the progress area
  const degradedInUI = await page.locator('.workbench-progress, .team-progress, .progress-panel').locator('text=Degraded').count()
  log('[BUG-4] "Degraded" as UI label: ' + (degradedInUI > 0 ? 'FAIL (' + degradedInUI + ' instances)' : 'OK - translated'))
  
  // Check for 降级完成
  const has降级 = progressText.includes('降级完成') || progressText.includes('降级')
  log('[BUG-4] 降级完成/降级 present: ' + has降级)

  // Step 6: Click 讨论动态 tab
  const feedTab = page.locator('.el-tabs__item:has-text("讨论动态"), [role="tab"]:has-text("讨论动态")').first()
  if (await feedTab.count()) {
    await feedTab.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
    log('[STEP 6] Clicked 讨论动态 tab')
  } else {
    log('[STEP 6] WARNING: No 讨论动态 tab found')
  }

  await page.screenshot({ path: path.join(evidenceDir, '05-feed-tab.png'), fullPage: true })

  const feedText = await page.evaluate(() => document.body.innerText)

  // BUG-5 Check: Feed should show Chinese tool names and roles
  const feedEnglishTerms = ['CompanyOverviewMcp', 'TechnicalAnalysisMcp', 'MarketContextMcp', 'NewsAnalysisMcp', 'company_overview_analyst', 'technical_analyst']
  for (const term of feedEnglishTerms) {
    const foundTerm = feedText.includes(term)
    log('[BUG-5] English term "' + term + '" in feed: ' + (foundTerm ? 'FAIL - still English' : 'OK - translated'))
  }

  // Check Chinese tool names in feed
  const chineseToolNames = ['公司概况', '技术分析', '市场背景', '新闻分析']
  for (const cn of chineseToolNames) {
    const found = feedText.includes(cn)
    log('[BUG-5] Chinese tool "' + cn + '" in feed: ' + (found ? 'FOUND' : 'not found'))
  }

  // Final summary
  log('')
  log('=== SUMMARY ===')
  log('Console errors: ' + errors.length)
  if (errors.length > 0) {
    for (const e of errors.slice(0, 5)) log('  ' + e)
  }

  fs.writeFileSync(path.join(evidenceDir, 'findings.txt'), findings.join('\n'))
  
} catch (e) {
  console.error('SCRIPT ERROR:', e.message)
  log('SCRIPT ERROR: ' + e.message)
  fs.writeFileSync(path.join(evidenceDir, 'findings.txt'), findings.join('\n'))
} finally {
  await context.close()
}
