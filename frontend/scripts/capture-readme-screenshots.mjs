import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'

const baseUrl = process.env.UI_BASE_URL || 'http://localhost:5119'
const outputDir = path.resolve('docs', 'screenshots')

fs.mkdirSync(outputDir, { recursive: true })

async function launchBrowser() {
  try {
    return await chromium.launch({
      channel: 'msedge',
      headless: true
    })
  } catch {
    return chromium.launch({ headless: true })
  }
}

async function waitForAppShell(page) {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 45000 })
  await page.waitForLoadState('networkidle', { timeout: 45000 }).catch(() => {})
  await page.getByText('SimplerJiang AI Agent').waitFor({ timeout: 15000 })
}

const browser = await launchBrowser()

try {
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1
  })
  const page = await context.newPage()

  await waitForAppShell(page)

  const symbolInput = page.getByPlaceholder('输入股票代码/名称/拼音缩写')
  await symbolInput.fill('sh600000')
  await page.getByRole('button', { name: '查询' }).click()
  await page.waitForTimeout(5000)
  await page.screenshot({
    path: path.join(outputDir, 'stock-terminal-1920x1080.png')
  })

  await page.getByRole('button', { name: '情绪轮动' }).click()
  await page.waitForTimeout(3000)
  await page.screenshot({
    path: path.join(outputDir, 'market-sentiment-1920x1080.png')
  })

  await page.goto(`${baseUrl}/?tab=admin-llm`, { waitUntil: 'domcontentloaded', timeout: 45000 })
  await page.waitForLoadState('networkidle', { timeout: 45000 }).catch(() => {})
  await page.getByText('管理员登录').waitFor({ timeout: 15000 })
  await page.screenshot({
    path: path.join(outputDir, 'llm-onboarding-1920x1080.png')
  })

  await context.close()
} finally {
  await browser.close()
}