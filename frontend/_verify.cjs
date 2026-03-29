const {chromium}=require('playwright');
(async()=>{
  const browser=await chromium.launch({channel:'msedge',headless:true});
  const ctx=await browser.newContext({viewport:{width:1440,height:900}});
  const page=await ctx.newPage();
  const errors=[];
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
  await page.goto('http://localhost:5119/',{waitUntil:'networkidle',timeout:30000});
  await page.waitForTimeout(1000);
  
  // Search for stock by code
  const si=page.locator('input').first();
  await si.click();
  await si.fill('000021');
  await page.waitForTimeout(2000);
  
  // Click suggestion or press Enter
  try {
    const sug = page.getByText('000021', {exact: false}).first();
    if(await sug.isVisible({timeout:1000})) {
      await sug.click();
      console.log('Clicked suggestion');
    }
  } catch(e) {
    await page.keyboard.press('Enter');
    console.log('Pressed Enter');
  }
  
  await page.waitForTimeout(5000);
  await page.screenshot({path:'c:/Users/kong/AiAgent/.automation/screenshots/02-stock.png'});
  
  const bodyText = await page.textContent('body');
  console.log('Body length:', bodyText.length);
  
  // List visible button/tab text
  const btns = await page.locator('button, [role=tab], .tab-item, .sub-tab').allTextContents();
  console.log('Buttons/Tabs:', btns.filter(t=>t.trim()).slice(0,25).map(t=>t.trim().substring(0,30)).join(' | '));
  
  // Check for key terms
  console.log('Has stock data:', bodyText.length > 3000);
  
  console.log('Errors:', errors.length);
  errors.slice(0,3).forEach(e=>console.log('ERR:', e.substring(0,120)));
  
  await browser.close();
})().catch(e=>console.error('FAIL:',e.message));
