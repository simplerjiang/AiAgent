import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'

const baseUrl = 'http://localhost:5119'
const userDataDir = path.resolve('..', '.automation', 'edge-profile')
const evidenceDir = path.resolve('..', '.automation', 'reports', 'workbench-l10n')
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

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 30000 })
  await page.waitForTimeout(3000)
  log('[STEP 1] Loaded homepage')

  // Search for sz000021
  const symbolInput = page.locator('input[placeholder*="\u80A1\u7968"]').first()
  await symbolInput.click()
  await symbolInput.fill('sz000021')
  await page.waitForTimeout(500)
  const queryBtn = page.getByRole('button', { name: '\u67E5\u8BE2' }).first()
  await queryBtn.click({ timeout: 5000 })
  
  // Wait for stock to fully load - check for stock name appearing
  log('[STEP 2] Clicked query, waiting for stock load...')
  await page.waitForTimeout(10000)
  
  // Verify stock loaded
  const stockLoaded = await page.evaluate(() => {
    const text = document.body.innerText
    return text.includes('\u6DF1\u79D1\u6280') || text.includes('000021')
  })
  log('[STEP 2] Stock loaded: ' + stockLoaded)

  await page.screenshot({ path: path.join(evidenceDir, '01-stock-loaded.png'), fullPage: false })

  // Scroll to bottom
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight))
  await page.waitForTimeout(2000)

  // Click history tab
  const historyTab = page.locator('text=\u5386\u53F2\u8BB0\u5F55').first()
  if (await historyTab.count()) {
    await historyTab.click({ timeout: 5000 })
    await page.waitForTimeout(3000)
    log('[STEP 3] Clicked history tab')
  }
  
  await page.screenshot({ path: path.join(evidenceDir, '02-history-tab.png'), fullPage: false })

  // Check for sessions
  let sessionCount = await page.locator('.history-session-row').count()
  log('[STEP 3] History sessions found: ' + sessionCount)
  
  if (sessionCount > 0) {
    // Click first session
    await page.locator('.history-session-row').first().click({ timeout: 5000 })
    await page.waitForTimeout(5000)
    log('[STEP 3] Loaded session')
  } else {
    // If no sessions, try the API directly to load session 8
    log('[STEP 3] Trying direct API load...')
    const apiUrl = baseUrl + '/api/stocks/research/sessions/8'
    const sessionData = await page.evaluate(async (url) => {
      const resp = await fetch(url)
      return resp.ok ? await resp.json() : null
    }, apiUrl)
    
    if (sessionData) {
      log('[STEP 3] Got session 8 via API, has ' + (sessionData.turns?.length ?? 0) + ' turns')
      // The data is available, let's check it directly for translation evidence
    }
    
    // Also try loading via the workbench input
    const workbenchInput = page.locator('input[placeholder*="\u7814\u7A76\u6307\u4EE4"]').first()
    if (await workbenchInput.count()) {
      log('[STEP 3] Found workbench input, but no sessions to load from history')
    }
  }

  // Take full page screenshot
  await page.screenshot({ path: path.join(evidenceDir, '03-full-page.png'), fullPage: true })

  // === REPORT TAB ===
  const reportTab = page.locator('text=\u7814\u7A76\u62A5\u544A').first()
  if (await reportTab.count()) {
    await reportTab.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
    log('[TAB] Report')
  }
  await page.screenshot({ path: path.join(evidenceDir, '04-report-tab.png'), fullPage: true })
  const bodyText = await page.evaluate(() => document.body.innerText)

  log('')
  log('--- BUG-1: Block Titles ---')
  for (const cb of ['\u516C\u53F8\u6982\u89C8', '\u6280\u672F\u5206\u6790', '\u5E02\u573A\u80CC\u666F', '\u65B0\u95FB\u5206\u6790', '\u770B\u591A\u7814\u7A76', '\u770B\u7A7A\u7814\u7A76', '\u6700\u7EC8\u51B3\u7B56']) {
    log('  "' + cb + '": ' + (bodyText.includes(cb) ? 'FOUND' : 'not found'))
  }
  const engBlocks = bodyText.match(/\b(CompanyOverview|TechnicalAnalysis|MarketContext|NewsAnalysis|BullResearch|BearResearch)\b/g)
  log('  English block names: ' + (engBlocks ? JSON.stringify([...new Set(engBlocks)]) : 'NONE'))

  log('')
  log('--- BUG-2: Evidence Sources ---')
  for (const mcp of ['CompanyOverviewMcp', 'TechnicalAnalysisMcp', 'MarketContextMcp', 'NewsAnalysisMcp']) {
    log('  "' + mcp + '": ' + (bodyText.includes(mcp) ? 'FAIL' : 'OK'))
  }

  // === PROGRESS TAB ===
  const progressTab = page.locator('text=\u56E2\u961F\u8FDB\u5EA6').first()
  if (await progressTab.count()) {
    await progressTab.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
    log('')
    log('[TAB] Progress')
  }
  await page.screenshot({ path: path.join(evidenceDir, '05-progress-tab.png'), fullPage: true })
  const progressBody = await page.evaluate(() => document.body.innerText)

  log('')
  log('--- BUG-3: Role Names ---')
  for (const role of ['company_overview_analyst', 'technical_analyst', 'news_analyst', 'bull_researcher', 'bear_researcher', 'market_context_analyst', 'research_manager']) {
    log('  "' + role + '": ' + (progressBody.includes(role) ? 'FAIL' : 'OK'))
  }
  for (const zr of ['\u516C\u53F8\u6982\u89C8', '\u6280\u672F\u5206\u6790', '\u65B0\u95FB\u5206\u6790', '\u770B\u591A\u7814\u7A76', '\u770B\u7A7A\u7814\u7A76', '\u5E02\u573A\u80CC\u666F', '\u7814\u7A76\u7BA1\u7406']) {
    log('  zh "' + zr + '": ' + (progressBody.includes(zr) ? 'FOUND' : 'not found'))
  }

  log('')
  log('--- BUG-4: Degraded Status ---')
  const degradedMatches = progressBody.match(/\bDegraded\b/g)
  log('  "Degraded" in UI: ' + (degradedMatches ? degradedMatches.length + ' FAIL' : '0 OK'))
  log('  "\u964D\u7EA7" present: ' + (progressBody.includes('\u964D\u7EA7') ? 'FOUND' : 'not found'))
  log('  "\u964D\u7EA7\u5B8C\u6210" present: ' + (progressBody.includes('\u964D\u7EA7\u5B8C\u6210') ? 'FOUND' : 'not found'))

  // === FEED TAB ===
  const feedTab = page.locator('text=\u8BA8\u8BBA\u52A8\u6001').first()
  if (await feedTab.count()) {
    await feedTab.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
    log('')
    log('[TAB] Feed')
  }
  await page.screenshot({ path: path.join(evidenceDir, '06-feed-tab.png'), fullPage: true })
  const feedBody = await page.evaluate(() => document.body.innerText)

  log('')
  log('--- BUG-5: Feed Tool/Role Names ---')
  for (const term of ['CompanyOverviewMcp', 'TechnicalAnalysisMcp', 'MarketContextMcp', 'NewsAnalysisMcp', 'company_overview_analyst', 'technical_analyst', 'news_analyst', 'bull_researcher', 'bear_researcher']) {
    log('  "' + term + '": ' + (feedBody.includes(term) ? 'FAIL' : 'OK'))
  }
  for (const zt of ['\u516C\u53F8\u6982\u51B5', '\u6280\u672F\u5206\u6790', '\u5E02\u573A\u80CC\u666F', '\u65B0\u95FB\u5206\u6790']) {
    log('  zh "' + zt + '": ' + (feedBody.includes(zt) ? 'FOUND' : 'not found'))
  }

  log('')
  log('=== ERRORS: ' + errors.length + ' ===')
  errors.slice(0, 5).forEach(e => log('  ' + e))

  fs.writeFileSync(path.join(evidenceDir, 'findings.txt'), findings.join('\n'))
  fs.writeFileSync(path.join(evidenceDir, 'body-report.txt'), bodyText.substring(0, 8000))
  fs.writeFileSync(path.join(evidenceDir, 'body-progress.txt'), progressBody.substring(0, 5000))
  fs.writeFileSync(path.join(evidenceDir, 'body-feed.txt'), feedBody.substring(0, 5000))

} catch (e) {
  console.error('SCRIPT ERROR:', e.message)
  log('SCRIPT ERROR: ' + e.message)
  fs.writeFileSync(path.join(evidenceDir, 'findings.txt'), findings.join('\n'))
} finally {
  await context.close()
}
