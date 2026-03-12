import { chromium } from 'playwright';
import fs from 'node:fs';

const baseUrl = process.env.P0R1_BASE_URL || 'http://localhost:5119';
const backendLogPath = process.env.P0R1_BACKEND_LOG || 'backend/SimplerJiangAiAgent.Api/App_Data/logs/llm-requests.txt';

const run = async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true }).catch(async () => {
    return chromium.launch({ headless: true });
  });

  const context = await browser.newContext();
  const page = await context.newPage();

  const consoleErrors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      consoleErrors.push(msg.text());
    }
  });

  const responseErrors = [];
  page.on('response', resp => {
    const url = resp.url();
    if (!url.includes('/api/admin/source-governance/')) {
      return;
    }

    if (resp.status() >= 400) {
      responseErrors.push(`${resp.status()} ${url}`);
    }
  });

  await page.goto(baseUrl, { waitUntil: 'networkidle' });

  await page.getByRole('button', { name: '治理开发者模式' }).click();
  await page.getByPlaceholder('管理员账号').fill('admin');
  await page.getByPlaceholder('管理员密码').fill('admin123');
  await page.getByRole('button', { name: '登录' }).click();

  const toggle = page.getByLabel('开启 Developer Mode（只读诊断）');
  await toggle.check();

  await page.waitForSelector('.cards .card', { timeout: 15000 });
  await page.waitForSelector('text=来源状态', { timeout: 15000 });

  await page.getByPlaceholder('按状态过滤，如 generated').fill('generated');
  await page.getByRole('button', { name: '应用过滤' }).click({ force: true });

  const detailButtons = page.getByRole('button', { name: '详情' });
  if ((await detailButtons.count()) > 0) {
    const detailButton = detailButtons.first();
    await detailButton.click({ force: true });
    await page.waitForSelector('text=变更详情 #', { timeout: 15000 });

    const jumpTraceButton = page.getByRole('button', { name: '跳转 Trace' }).first();
    if (await jumpTraceButton.isEnabled()) {
      await jumpTraceButton.click({ force: true });
    }
  }

  await page.getByPlaceholder('输入 traceId 检索日志').fill('abc12345');
  await page.getByRole('button', { name: '检索 Trace' }).click();

  const hasCoreCard = await page.locator('.cards .card').count();
  const pageContent = await page.content();

  await browser.close();

  if (hasCoreCard < 3) {
    throw new Error(`Expected dashboard cards >=3, got ${hasCoreCard}`);
  }

  if (!pageContent.includes('来源状态') || !pageContent.includes('候选来源') || !pageContent.includes('修复队列')) {
    throw new Error('Expected core sections not found in developer mode page');
  }

  if (consoleErrors.length > 0) {
    throw new Error(`Console errors found: ${consoleErrors.join(' | ')}`);
  }

  if (responseErrors.length > 0) {
    throw new Error(`API errors found: ${responseErrors.join(' | ')}`);
  }

  if (fs.existsSync(backendLogPath)) {
    const logs = fs.readFileSync(backendLogPath, 'utf8');
    const lowered = logs.toLowerCase();
    if (lowered.includes('unhandled exception') || lowered.includes('fatal')) {
      throw new Error(`Backend log contains critical errors. log=${backendLogPath}`);
    }
  }

  console.log('P0-R1 edge check passed');
};

run().catch(err => {
  console.error(err);
  process.exit(1);
});
