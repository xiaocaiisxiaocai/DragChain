const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  const errors = [];
  const failedRequests = [];

  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });

  page.on('requestfailed', req => {
    failedRequests.push({ url: req.url(), failure: req.failure()?.errorText });
  });

  page.on('response', resp => {
    if (resp.status() >= 400) {
      failedRequests.push({ url: resp.url(), status: resp.status() });
    }
  });

  await page.goto('http://localhost:5173', { waitUntil: 'networkidle', timeout: 15000 });

  console.log('=== Failed Requests (4xx/5xx) ===');
  if (failedRequests.length === 0) {
    console.log('None!');
  } else {
    failedRequests.forEach(r => {
      console.log(`  [${r.status || r.failure?.errorText}] ${r.url}`);
    });
  }

  console.log('\n=== Console Errors ===');
  if (errors.length === 0) {
    console.log('None!');
  } else {
    errors.forEach(e => console.log('  ' + e));
  }

  await browser.close();
})();
