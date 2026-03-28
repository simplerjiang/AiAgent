const { chromium } = require('playwright');
const fs = require('fs');
const ssDir = 'C:/Users/kong/AiAgent/.automation/screenshots';

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
  const page = await ctx.newPage();

  await page.goto('http://localhost:5000', { waitUntil: 'networkidle', timeout: 15000 });
  await page.click('button.history-chip:has-text("深科技")');
  await page.waitForTimeout(5000);

  // Click feed tab
  await page.click('.wb-tab:has-text("讨论动态")');
  await page.waitForTimeout(3000);

  // Scroll through the feed and check lifecycle messages
  const feedItems = await page.evaluate(() => {
    const items = [];
    document.querySelectorAll('.feed-item, .wb-feed-item, [class*="feed-item"], [class*="lifecycle"]').forEach(el => {
      items.push({ 
        class: el.className, 
        text: el.textContent.trim().substring(0, 80),
        style: window.getComputedStyle(el).fontSize + ' / ' + window.getComputedStyle(el).opacity + ' / ' + window.getComputedStyle(el).color
      });
    });
    return items;
  });
  
  console.log('Feed items found:', feedItems.length);
  feedItems.slice(0, 20).forEach((item, i) => {
    console.log(`  [${i}] class="${item.class}" style="${item.style}" text="${item.text}"`);
  });

  // Check specifically for "Started" and "Completed" lifecycle messages
  const lifecycleMsgs = await page.evaluate(() => {
    const all = document.querySelectorAll('*');
    const matches = [];
    all.forEach(el => {
      const t = el.textContent.trim();
      if ((t.includes('started') || t.includes('Started') || t.includes('Completed') || t.includes('completed') || t.includes('Dispatching')) && el.children.length <= 2 && t.length < 100) {
        const cs = window.getComputedStyle(el);
        matches.push({ 
          tag: el.tagName, 
          class: el.className.substring(0, 60), 
          text: t.substring(0, 60),
          fontSize: cs.fontSize,
          opacity: cs.opacity,
          color: cs.color 
        });
      }
    });
    return matches.slice(0, 15);
  });
  
  console.log('\nLifecycle message styling:');
  lifecycleMsgs.forEach((m, i) => {
    console.log(`  [${i}] <${m.tag}> cls="${m.class}" font=${m.fontSize} opacity=${m.opacity} color=${m.color} => "${m.text}"`);
  });

  // Check feed for non-lifecycle (actual content) messages for comparison
  const contentMsgs = await page.evaluate(() => {
    const bubbles = document.querySelectorAll('.feed-bubble, .chat-bubble, [class*="bubble"], .feed-msg-body, [class*="msgBody"]');
    return Array.from(bubbles).slice(0, 5).map(el => {
      const cs = window.getComputedStyle(el);
      return { class: el.className.substring(0, 60), fontSize: cs.fontSize, text: el.textContent.trim().substring(0, 80) };
    });
  });
  
  console.log('\nContent message styling (for comparison):');
  contentMsgs.forEach((m, i) => {
    console.log(`  [${i}] cls="${m.class}" font=${m.fontSize} => "${m.text}"`);
  });

  // Check the 404 error - what resource is 404?
  const networkErrors = [];
  page.on('response', resp => {
    if (resp.status() >= 400) networkErrors.push(`${resp.status()} ${resp.url()}`);
  });
  
  // Reload to catch network errors
  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  console.log('\nNetwork errors on reload:', JSON.stringify(networkErrors));

  await browser.close();
  console.log('DONE');
})().catch(e => { console.error('FATAL:', e.message); process.exit(1); });