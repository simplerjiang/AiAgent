const {chromium}=require('playwright');
(async()=>{
  const browser=await chromium.launch({channel:'msedge',headless:true});
  const ctx=await browser.newContext({viewport:{width:1440,height:900}});
  const page=await ctx.newPage();
  const errors=[];
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
  await page.goto('http://localhost:5119/',{waitUntil:'networkidle',timeout:30000});
  await page.waitForTimeout(1500);
  
  // Select stock from sidebar
  const stockItem = page.locator('text=sz000021').first();
  if(await stockItem.isVisible({timeout:2000})) {
    await stockItem.click();
    console.log('Clicked sz000021 in sidebar');
  }
  await page.waitForTimeout(3000);
  
  // Now check the workbench tabs
  const tabNames = ['research-report', 'team-progress', 'feed', 'history'];
  const tabLabels = ['research-report', 'team-progress', 'discussion-feed', 'history'];
  
  // Click research report tab first
  const reportTab = page.getByText('研究报告', {exact: false}).first();
  if(await reportTab.isVisible({timeout:2000})) {
    await reportTab.click();
    console.log('=== Clicked 研究报告 tab ===');
    await page.waitForTimeout(2000);
    const reportText = await page.textContent('body');
    console.log('Body length:', reportText.length);
    
    // BUG-1: CompanyOverview block title
    const hasCompanyOverviewRaw = reportText.includes('CompanyOverview');
    console.log('BUG-1 CompanyOverview raw:', hasCompanyOverviewRaw ? 'FAIL - still English!' : 'PASS');
    
    // BUG-2: CompanyOverviewMcp source
    const hasCompanyOverviewMcp = reportText.includes('CompanyOverviewMcp');
    console.log('BUG-2 CompanyOverviewMcp raw:', hasCompanyOverviewMcp ? 'FAIL - still English!' : 'PASS');
    
    // Check if Chinese translations are present
    const hasGongSiGaiLan = reportText.includes('\u516C\u53F8\u6982\u89C8');
    console.log('Chinese 公司概览 present:', hasGongSiGaiLan);
  }
  
  // Click team progress tab
  const progressTab = page.getByText('团队进度', {exact: false}).first();
  if(await progressTab.isVisible({timeout:2000})) {
    await progressTab.click();
    console.log('\n=== Clicked 团队进度 tab ===');
    await page.waitForTimeout(2000);
    const progressText = await page.textContent('body');
    
    // BUG-3: company_overview_analyst
    const hasRoleRaw = progressText.includes('company_overview_analyst');
    console.log('BUG-3 company_overview_analyst raw:', hasRoleRaw ? 'FAIL' : 'PASS');
    
    // BUG-4: tool_error and Degraded
    const hasToolError = progressText.includes('tool_error:');
    const hasDegradedRaw = /(?<!\u964D\u7EA7)Degraded/.test(progressText);
    console.log('BUG-4 tool_error raw:', hasToolError ? 'FAIL' : 'PASS (not present or translated)');
    console.log('BUG-4 Degraded raw:', hasDegradedRaw ? 'FAIL' : 'PASS');
    
    await page.screenshot({path:'c:/Users/kong/AiAgent/.automation/screenshots/03-progress.png'});
  }
  
  // Click discussion feed tab  
  const feedTab = page.getByText('\u8BA8\u8BBA\u52A8\u6001', {exact: false}).first();
  if(await feedTab.isVisible({timeout:2000})) {
    await feedTab.click();
    console.log('\n=== Clicked \u8BA8\u8BBA\u52A8\u6001 tab ===');
    await page.waitForTimeout(2000);
    const feedText = await page.textContent('body');
    
    // BUG-5: Role company_overview_analyst started
    const hasRoleStarted = feedText.includes('Role company_overview_analyst started');
    const hasRoleEnglish = feedText.includes('Role ') && feedText.includes(' started');
    console.log('BUG-5 "Role X started" raw:', hasRoleStarted ? 'FAIL' : 'PASS');
    console.log('BUG-5 any "Role...started" pattern:', hasRoleEnglish ? 'CHECK NEEDED' : 'PASS');
    
    // Check Chinese equivalents
    const hasStartAnalysis = feedText.includes('\u5F00\u59CB\u5206\u6790');
    const hasFinishAnalysis = feedText.includes('\u5206\u6790\u5B8C\u6210');
    console.log('Chinese \u5F00\u59CB\u5206\u6790 present:', hasStartAnalysis);
    console.log('Chinese \u5206\u6790\u5B8C\u6210 present:', hasFinishAnalysis);
    
    await page.screenshot({path:'c:/Users/kong/AiAgent/.automation/screenshots/04-feed.png'});
  }
  
  // Overall scan for any remaining untranslated CamelCase MCP terms
  const fullText = await page.textContent('body');
  const mcpPattern = /[A-Z][a-z]+(?:[A-Z][a-z]+)+Mcp/g;
  const matches = fullText.match(mcpPattern) || [];
  console.log('\n=== Global CamelCase MCP scan ===');
  if(matches.length > 0) {
    console.log('WARNING - found CamelCase MCP terms:', [...new Set(matches)].join(', '));
  } else {
    console.log('PASS - no CamelCase MCP terms visible');
  }
  
  // Check for snake_case role IDs
  const snakePattern = /[a-z]+_[a-z]+_analyst/g;
  const snakeMatches = fullText.match(snakePattern) || [];
  if(snakeMatches.length > 0) {
    console.log('WARNING - found snake_case roles:', [...new Set(snakeMatches)].join(', '));
  } else {
    console.log('PASS - no snake_case role IDs visible');
  }
  
  console.log('\nConsole errors:', errors.length);
  errors.slice(0,5).forEach(e=>console.log('ERR:', e.substring(0,150)));
  
  await browser.close();
})().catch(e=>console.error('FAIL:',e.message));
