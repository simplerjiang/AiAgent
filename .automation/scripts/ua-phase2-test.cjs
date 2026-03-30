const {chromium}=require('playwright');
(async()=>{
  const b=await chromium.launch({channel:'msedge',headless:false});
  const p=await b.newPage({viewport:{width:1440,height:900}});
  const msgs=[];
  p.on('console',m=>{if(m.type()==='error'||m.type()==='warning')msgs.push('['+m.type()+'] '+m.text())});
  p.on('pageerror',e=>msgs.push('[PAGE_ERROR] '+e.message));
  
  await p.goto('http://localhost:5119/');
  await p.waitForTimeout(3000);
  await p.screenshot({path:'../.automation/screenshots/ua-phase2-homepage.png',fullPage:true});
  console.log('=== Step 1: Homepage screenshot saved ===');
  
  const bodyText1=await p.innerText('body');
  const lines1=bodyText1.split('\n').filter(l=>l.trim());
  console.log('Homepage content (first 50 lines):');
  lines1.slice(0,50).forEach((l,i)=>console.log(i+': '+l));
  
  const input=p.locator('input[type=text]').first();
  await input.fill('600519');
  await p.waitForTimeout(300);
  const queryBtn = p.locator('button').filter({hasText: /查询/});
  await queryBtn.first().click();
  await p.waitForTimeout(6000);
  await p.screenshot({path:'../.automation/screenshots/ua-phase2-after-search.png',fullPage:true});
  console.log('\n=== Step 2: After search screenshot saved ===');
  
  const bodyText2=await p.innerText('body');
  const lines2=bodyText2.split('\n').filter(l=>l.trim());
  console.log('After search content (first 150 lines):');
  lines2.slice(0,150).forEach((l,i)=>console.log(i+': '+l));
  
  console.log('\n=== Step 3: Looking for tabs ===');
  const allBtns=await p.locator('button').allInnerTexts();
  console.log('All buttons:');
  allBtns.forEach((t,i)=>console.log('  btn '+i+': ['+t+']'));
  
  console.log('\n=== Step 4: Clicking tabs ===');
  const tabNames=['交易计划','新闻影响','AI 分析','全局总览'];
  for(const name of tabNames){
    try{
      const tabBtn=p.locator('button').filter({hasText: new RegExp(name)}).first();
      if(await tabBtn.isVisible({timeout:2000})){
        await tabBtn.click();
        await p.waitForTimeout(2000);
        const safeName=name.replace(/ /g,'_');
        await p.screenshot({path:'../.automation/screenshots/ua-phase2-tab-'+safeName+'.png',fullPage:true});
        console.log('Clicked tab: '+name+' - screenshot saved');
        const tabContent=await p.innerText('body');
        const tabLines=tabContent.split('\n').filter(l=>l.trim());
        console.log('  Content snippet (lines 0-40):');
        tabLines.slice(0,40).forEach((l,i)=>console.log('  '+i+': '+l));
      } else {
        console.log('Tab NOT visible: '+name);
      }
    }catch(e){
      console.log('Error clicking tab '+name+': '+e.message);
    }
  }
  
  console.log('\n=== Step 5: Dev labels check ===');
  const freshBody=await p.innerText('body');
  const devLabels=['TerminalView','Top Market Tape','GOAL-012'];
  for(const label of devLabels){
    const found=freshBody.includes(label);
    console.log('Dev label "'+label+'": '+(found?'FOUND (BAD)':'Not found (GOOD)'));
  }
  
  console.log('\n=== Step 6: Drag handles check ===');
  const dividers=await p.locator('[class*=divider], [class*=resize], [class*=drag], [class*=splitter]').count();
  console.log('Divider/resize elements found: '+dividers);
  
  console.log('\n=== Step 7: Console errors ===');
  msgs.forEach(m=>console.log(m));
  console.log('Total console errors/warnings: '+msgs.length);
  
  await b.close();
  console.log('\n=== Test complete ===');
})().catch(e=>{console.error('FATAL:',e.message);process.exit(1)});
