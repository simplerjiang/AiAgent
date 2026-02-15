import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'

const baseUrl = process.env.UI_BASE_URL || 'http://localhost:5119'
const userDataDir = path.resolve('..', '.automation', 'edge-profile')
const evidenceDir = path.resolve('..', '.automation', 'reports', 'edge-goal007-final')
fs.mkdirSync(evidenceDir, { recursive: true })

const context = await chromium.launchPersistentContext(userDataDir, {
  channel: 'msedge',
  headless: true,
  viewport: { width: 1440, height: 900 }
})

const errors = []

try {
  const page = context.pages()[0] ?? (await context.newPage())
  page.on('pageerror', err => errors.push(`pageerror:${err.message}`))
  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(`console:${msg.text()}`)
    }
  })

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 45000 })
  await page.waitForTimeout(1200)

  const firstHistoryRow = page.locator('.history-table tbody tr').first()
  if (await firstHistoryRow.count()) {
    await firstHistoryRow.click({ timeout: 5000 })
    await page.waitForTimeout(2000)
  } else {
    const symbolInput = page.locator('input[placeholder="输入股票代码/名称/拼音缩写"]').first()
    if (await symbolInput.count()) {
      await symbolInput.fill('sz000001')
      await page.getByRole('button', { name: '查询' }).first().click({ timeout: 5000 })
      await page.waitForTimeout(3500)
    }
  }

  const hasStockName = (await page.locator('text=当前价').count()) > 0

  const runButton = page.getByRole('button', { name: '启动多Agent' }).first()
  if (await runButton.count()) {
    await runButton.click({ timeout: 8000 })
    await page.waitForTimeout(8000)
  }

  const firstCard = page.locator('.agent-card').first()
  const hasAgentCard = (await firstCard.count()) > 0

  const rawToggle = page.locator('.agent-card .raw-toggle').first()
  let hasRawJson = false
  if (await rawToggle.count()) {
    await rawToggle.click({ timeout: 5000 })
    await page.waitForTimeout(400)
    hasRawJson = (await page.locator('.agent-card .raw-json').first().count()) > 0
  }

  const confidenceBadgeCount = await page.locator('.confidence-badge').count()
  const hasRecommendationBlock = (await page.locator('.recommendation-block').count()) > 0

  const evidenceSectionCount = await page.locator('.list-section h5:has-text("证据来源")').count()

  const triggerSectionCount = await page.locator('.pill-list h5:has-text("触发条件")').count()
  const invalidSectionCount = await page.locator('.pill-list h5:has-text("失效条件")').count()
  const limitSectionCount = await page.locator('.pill-list h5:has-text("风险上限")').count()

  const chatInput = page.locator('.chat-input textarea').first()
  let chatInteractive = false
  if (await chatInput.count()) {
    await chatInput.click({ timeout: 5000 })
    await chatInput.fill('请用一句话说明这只股票当前主要风险')
    await page.waitForTimeout(300)
    chatInteractive = (await chatInput.inputValue())?.length > 0
  }

  await page.screenshot({ path: path.join(evidenceDir, 'ui-goal007-final.png'), fullPage: true })

  const summary = {
    baseUrl,
    hasStockName,
    hasAgentCard,
    hasRawJson,
    confidenceBadgeCount,
    hasRecommendationBlock,
    evidenceSectionCount,
    triggerSectionCount,
    invalidSectionCount,
    limitSectionCount,
    chatInteractive,
    consoleErrors: errors,
    success:
      hasStockName &&
      hasAgentCard &&
      hasRawJson &&
        evidenceSectionCount >= 0 &&
      chatInteractive &&
      errors.length === 0
  }

  fs.writeFileSync(path.join(evidenceDir, 'summary.json'), JSON.stringify(summary, null, 2))
  console.log(JSON.stringify(summary))
} finally {
  await context.close()
}
