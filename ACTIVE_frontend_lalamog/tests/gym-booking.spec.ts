import { test, expect, type Page } from '@playwright/test';
import { execFileSync } from 'node:child_process';

test.use({ launchOptions: { args: ['--disable-renderer-backgrounding','--disable-backgrounding-occluded-windows','--disable-background-timer-throttling','--disable-gpu','--disable-features=CalculateNativeWinOcclusion'] } });
test('owner publishes a gym; matched fighters book, confirm, complete and control public history', async ({ browser }) => {
  const database = process.env.LALAMOG_GYM_TEST_DATABASE;
  test.skip(!database, 'Requires the isolated gym demo database.');
  if (!/^LalamogTests_GymBrowser_[a-f0-9]{32}$/.test(database!)) throw new Error('Refusing a non-isolated database.');
  const apiPid=Number(process.env.LALAMOG_GYM_API_PID);
  if(!Number.isSafeInteger(apiPid)||apiPid<=0)throw new Error('Set the isolated API process ID for the database preflight.');
  function fixtureSql(query:string) { return execFileSync('docker',['exec','-i','lalamog-sql','sh','-c','export SQLCMDPASSWORD=$MSSQL_SA_PASSWORD; exec /opt/mssql-tools18/bin/sqlcmd -I -S localhost -U sa -C -b'],{input:query+'\nGO\n',stdio:['pipe','pipe','pipe']}); }
  const ownerContext=await browser.newContext({viewport:{width:1440,height:1000}});
  const aContext=await browser.newContext({viewport:{width:390,height:900}});
  const bContext=await browser.newContext({viewport:{width:1280,height:900}});
  const publicContext=await browser.newContext({viewport:{width:375,height:900}});
  const owner=await ownerContext.newPage(),a=await aContext.newPage(),b=await bContext.newPage(),visitor=await publicContext.newPage();
  const errors:string[]=[];
  for(const page of [owner,a,b,visitor]) { page.on('pageerror',e=>errors.push(e.message)); await page.route('https://tile.openstreetmap.org/**',r=>r.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#d9dfd2"/></svg>'})); }
  const suffix=Date.now(); const nameA=`AAA Gym A ${suffix}`,nameB=`AAB Gym B ${suffix}`,gymName=`Demo Training Gym ${suffix}`;
  async function register(page:Page,name:string,accountType:string) {
    await page.goto('/login');await page.getByRole('button',{name:'New here? Create an account'}).click();
    await page.getByLabel('Account type',{exact:true}).selectOption(accountType);
    if(accountType==='fighter')await page.getByLabel('Fighter name (unique)',{exact:true}).fill(name.replaceAll(' ','-'));
    await page.getByLabel(accountType==='fighter'?'Alias':'Your display name',{exact:true}).fill(name);
    await page.getByLabel('Email',{exact:true}).fill(`${accountType}-${name.replaceAll(' ','-')}@example.test`);
    await page.getByLabel('Password',{exact:true}).fill('Practice123!Example');
    await page.getByRole('button',{name:'Create account',exact:true}).click();
    await expect(page.getByRole('heading',{name:accountType==='fighter'?'Discover':'My gyms',exact:true})).toBeVisible();
  }
  try {
    await visitor.goto('/gyms');await expect(visitor.getByRole('heading',{name:'Gyms open for fights',exact:true})).toBeVisible();
    fixtureSql(`IF NOT EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE host_process_id=${apiPid} AND database_id=DB_ID(N'${database}')) THROW 51040,'Practice API database isolation check failed. No demo writes permitted.',1;`);
    await register(owner,`Owner ${suffix}`,'gymOwner');
    await owner.getByRole('button',{name:'Add gym',exact:true}).click();
    await owner.getByLabel('Gym name',{exact:true}).fill(gymName);
    await owner.getByLabel('About your gym',{exact:true}).fill('Fictional gym for the complete booking demo.');
    await owner.getByLabel('Public contact information',{exact:true}).fill('demo-gym@example.test');
    await owner.getByLabel('Session price (PHP, both fighters)',{exact:true}).fill('650');
    await owner.getByLabel('Gym address',{exact:true}).fill('Fictional gym, Antipolo, Rizal');
    await owner.getByLabel('Region',{exact:true}).selectOption('0400000000');
    await owner.getByLabel('Province / independent city',{exact:true}).selectOption('0405800000');
    await owner.getByLabel('City / municipality',{exact:true}).selectOption('0405802000');
    await owner.getByLabel('Latitude',{exact:true}).fill('14.5855941');await owner.getByLabel('Longitude',{exact:true}).fill('121.1872028');
    const creation=owner.waitForResponse(r=>r.url().endsWith('/api/gyms')&&r.request().method()==='POST');
    await owner.getByRole('button',{name:'Create gym draft',exact:true}).click();const created=await creation;expect(created.ok()).toBeTruthy();const gym=await created.json();
    await expect(owner.getByRole('status').filter({hasText:'Gym saved.'})).toBeVisible();
    const png=await owner.evaluate(()=>{const c=document.createElement('canvas');c.width=640;c.height=360;const ctx=c.getContext('2d')!;ctx.fillStyle='#253e35';ctx.fillRect(0,0,640,360);ctx.fillStyle='#efe5c9';ctx.font='bold 40px sans-serif';ctx.fillText('DEMO TRAINING GYM',50,160);ctx.font='22px sans-serif';ctx.fillText('Fictional test photo',50,215);return c.toDataURL('image/png').split(',')[1];});
    await owner.getByLabel(`Cover image for ${gymName}`,{exact:true}).setInputFiles({name:'demo-gym.png',mimeType:'image/png',buffer:Buffer.from(png,'base64')});await expect(owner.getByRole('status').filter({hasText:'Gym cover saved.'})).toBeVisible();
    await owner.getByRole('button',{name:'Publish gym',exact:true}).click();await expect(owner.getByText('Published on map',{exact:true})).toBeVisible();
    await visitor.goto(`/gyms/${gym.id}`);await expect(visitor.getByRole('heading',{name:gymName,exact:true})).toBeVisible();await expect(visitor.getByRole('img',{name:`${gymName} gym`,exact:true})).toBeVisible();await expect(visitor.getByText('₱650.00',{exact:true})).toBeVisible();
    await register(a,nameA,'fighter');await register(b,nameB,'fighter');
    for(const page of [a,b]) {await page.getByRole('link',{name:'Profile',exact:true}).click();await page.getByLabel('About you',{exact:true}).fill('Technical sparring demo profile.');await page.getByLabel('City / area',{exact:true}).fill('Antipolo');await page.getByRole('button',{name:'Save profile',exact:true}).click();await expect(page.getByRole('status')).toContainText('Profile saved.');}
    async function like(page:Page,target:string) {
      await page.getByRole('link',{name:'Discover',exact:true}).click();await page.getByRole('button',{name:'Refresh',exact:true}).click();
      for(let n=0;n<50;n++){const alias=page.locator('.fighter-details .fighter-alias');await expect(alias).toBeVisible();const current=(await alias.innerText()).replace(/^“|”$/g,'');if(current===target)break;await Promise.all([page.waitForResponse(r=>r.url().endsWith('/api/matches/swipes')&&r.request().method()==='POST'),page.getByRole('button',{name:'Pass',exact:true}).click()]);await expect(alias).not.toHaveText(`“${current}”`);}
      await expect(page.locator('.fighter-details .fighter-alias')).toHaveText(`“${target}”`);await page.getByRole('button',{name:'Like',exact:true}).click();
    }
    await like(a,nameB);await like(b,nameA);await expect(b.getByRole('dialog')).toBeVisible();await b.getByRole('button',{name:'Keep swiping',exact:true}).click();
    for(const [page,target] of [[a,nameB],[b,nameA]] as const){await page.getByRole('link',{name:'Inbox',exact:true}).click();await page.getByRole('button').filter({hasText:target}).click();}
    await a.getByLabel('Message',{exact:true}).fill('Let’s book the demo gym.');await a.getByRole('button',{name:'Send',exact:true}).click();await expect(b.getByRole('log')).toContainText('Let’s book the demo gym.');
    const matchId=Number(a.url().split('/').pop());
    await a.getByRole('link',{name:'Start the Match',exact:true}).click();await a.getByLabel('Fight location',{exact:true}).selectOption('gym');await a.getByLabel('Registered gym',{exact:true}).selectOption(String(gym.id));
    const future=new Date(Date.now()+86400000);const local=new Date(future.getTime()-future.getTimezoneOffset()*60000).toISOString().slice(0,16);await a.getByLabel('Date & time',{exact:true}).fill(local);
    const requested=a.waitForResponse(r=>r.url().endsWith('/api/fights')&&r.request().method()==='POST');await a.getByRole('button',{name:'Arrange fight',exact:true}).click();const response=await requested;expect(response.ok()).toBeTruthy();const fight=await response.json();await expect(a.getByRole('status')).toContainText('Gym booking requested.');
    await owner.getByRole('button',{name:'Refresh owner dashboard',exact:true}).click();let booking=owner.locator('.session-list > article').filter({hasText:nameA.replaceAll(' ','-')});await expect(booking).toContainText('pending approval');await booking.getByRole('button',{name:'Approve request',exact:true}).click();await expect(booking).toContainText('scheduled');
    for(const page of [a,b]){await page.goto(`/sessions?matchId=${matchId}`);await page.getByLabel('Emergency contact name',{exact:true}).fill('Fictional private contact');await page.getByLabel('Emergency contact phone',{exact:true}).fill('000-000-0000');await page.getByRole('checkbox',{name:'I accept the risks',exact:false}).check();await page.getByRole('checkbox',{name:'Allow my public name',exact:false}).check();await page.getByRole('button',{name:'Submit waiver & confirm',exact:true}).click();await expect(page.getByText('You: waiver submitted',{exact:false})).toBeVisible();}
    // Simulate elapsed time only in this generated disposable database. No application time bypass.
    fixtureSql(`USE [${database}]; UPDATE Fights SET ScheduledAt=DATEADD(MINUTE,-61,SYSDATETIMEOFFSET()) WHERE Id=${Number(fight.id)};`);
    await owner.getByRole('button',{name:'Refresh owner dashboard',exact:true}).click();booking=owner.locator('.session-list > article').filter({hasText:nameA.replaceAll(' ','-')});await booking.getByRole('button',{name:'Mark booking completed',exact:true}).click();await expect(booking).toContainText('completed');
    await visitor.reload();await expect(visitor.locator('.gym-fight-list')).toContainText(nameA.replaceAll(' ','-'));await expect(visitor.locator('.gym-fight-list')).toContainText(nameB.replaceAll(' ','-'));await expect(visitor.getByText('Fictional private contact',{exact:false})).toHaveCount(0);
    for(const width of [360,375,390,1440]){await visitor.setViewportSize({width,height:950});expect(await visitor.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBeTruthy();await visitor.screenshot({path:`test-results/gym-mvp-public-${width}.png`,fullPage:true,animations:'disabled'});}
    await a.getByRole('button',{name:'Refresh',exact:true}).click();await a.getByRole('checkbox',{name:'Allow my public name',exact:false}).click();await expect(a.getByRole('checkbox',{name:'Allow my public name',exact:false})).not.toBeChecked();await visitor.reload();await expect(visitor.locator('.gym-fight-list')).toContainText('Anonymous fight');
    // The same matched pair can use the private path after completion.
    await a.getByLabel('Fight location',{exact:true}).selectOption('private');await a.getByLabel('Date & time',{exact:true}).fill(local);await a.getByLabel('Venue name',{exact:true}).fill('Private demo venue');await a.getByLabel('Venue address',{exact:true}).fill('Private demo address');await a.getByRole('button',{name:'Arrange fight',exact:true}).click();await expect(a.getByRole('status')).toContainText('Fight arranged.');
    await a.getByRole('button',{name:'Cancel fight',exact:true}).click();await a.getByLabel('Cancellation reason',{exact:true}).fill('Private demo cancellation');await a.getByRole('button',{name:'Save cancellation',exact:true}).click();await expect(a.locator('.session-card').filter({hasText:'Private demo venue'})).toContainText('canceled');
    await a.getByLabel('Fight location',{exact:true}).selectOption('gym');await a.getByLabel('Registered gym',{exact:true}).selectOption(String(gym.id));await a.getByLabel('Date & time',{exact:true}).fill(local);await a.getByRole('button',{name:'Arrange fight',exact:true}).click();await expect(a.getByRole('status')).toContainText('Gym booking requested.');
    await owner.getByRole('button',{name:'Refresh owner dashboard',exact:true}).click();booking=owner.locator('.session-list > article').filter({hasText:'pending approval'});await booking.getByRole('button',{name:'Decline request',exact:true}).click();await booking.getByLabel('Reason',{exact:true}).fill('Demo decline: venue unavailable');await booking.getByRole('button',{name:'Save decline',exact:true}).click();await expect(owner.getByText('Demo decline: venue unavailable',{exact:true})).toBeVisible();
    expect(errors).toEqual([]);
  } finally { await Promise.all([ownerContext.close(),aContext.close(),bContext.close(),publicContext.close()]); }
});


test('owner can enable Fighter access and admin can moderate listings and change roles', async ({ browser }) => {
  const database=process.env.LALAMOG_GYM_TEST_DATABASE;test.skip(!database,'Requires an isolated gym demo database.');
  if(!/^LalamogTests_GymBrowser_[a-f0-9]{32}$/.test(database!))throw new Error('Invalid practice database.');
  const apiPid=Number(process.env.LALAMOG_GYM_API_PID);if(!Number.isSafeInteger(apiPid)||apiPid<=0)throw new Error('Missing practice API process ID.');
  const ownerContext=await browser.newContext({viewport:{width:390,height:950}}),adminContext=await browser.newContext({viewport:{width:1440,height:1000}});
  const owner=await ownerContext.newPage(),admin=await adminContext.newPage();const name=`Owner controls ${Date.now()}`;
  try {
    await owner.goto('/gyms');await expect(owner.getByRole('heading',{name:'Gyms open for fights',exact:true})).toBeVisible();
    execFileSync('docker',['exec','-i','lalamog-sql','sh','-c','export SQLCMDPASSWORD=$MSSQL_SA_PASSWORD; exec /opt/mssql-tools18/bin/sqlcmd -I -S localhost -U sa -C -b'],{input:`IF NOT EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE host_process_id=${apiPid} AND database_id=DB_ID(N'${database}')) THROW 51040,'Practice database isolation failed',1;\nGO\n`,stdio:['pipe','pipe','pipe']});
    await owner.goto('/login');await owner.getByRole('button',{name:'New here? Create an account'}).click();await owner.getByLabel('Account type',{exact:true}).selectOption('gymOwner');await expect(owner.getByLabel('Account type').locator('option')).toHaveCount(2);
    await owner.getByLabel('Your display name',{exact:true}).fill(name);await owner.getByLabel('Email',{exact:true}).fill(`owner-controls-${Date.now()}@example.test`);await owner.getByLabel('Password',{exact:true}).fill('Practice123!Example');await owner.getByRole('button',{name:'Create account',exact:true}).click();await expect(owner.getByRole('heading',{name:'My gyms',exact:true})).toBeVisible();await expect(owner.getByRole('link',{name:'Discover',exact:true})).toHaveCount(0);
    const csrf=await (await owner.request.get('/api/auth/csrf')).json();const created=await owner.request.post('/api/gyms',{headers:{'X-CSRF-TOKEN':csrf.token},data:{slug:`controls-${Date.now()}`,name:`${name} gym`,description:'Fictional role and moderation demo',contactInformation:'',address:'Fictional public venue',areaCode:'0405802000',latitude:14.6,longitude:121.2,sessionPrice:500,sessionDurationMinutes:60}});expect(created.ok()).toBeTruthy();const gym=await created.json();expect((await owner.request.post(`/api/gyms/${gym.id}/publish`,{headers:{'X-CSRF-TOKEN':csrf.token},data:{}})).ok()).toBeTruthy();
    await owner.getByRole('link',{name:'Profile',exact:true}).click();await owner.getByRole('button',{name:'Enable Fighter access',exact:true}).click();await expect(owner.getByRole('link',{name:'Discover',exact:true})).toBeVisible();await expect(owner.getByRole('link',{name:'My gyms',exact:true})).toBeVisible();expect(await owner.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBeTruthy();await owner.screenshot({path:'test-results/gym-mvp-owner-mobile.png',fullPage:true,animations:'disabled'});
    await admin.goto('/login');await admin.getByLabel('Email',{exact:true}).fill('alex.seed@example.test');await admin.getByLabel('Password',{exact:true}).fill('Lalamog123!Test');await admin.getByRole('button',{name:'Sign in',exact:true}).click();await admin.getByRole('link',{name:'Admin',exact:true}).click();await expect(admin.getByRole('heading',{name:'Admin',exact:true})).toBeVisible();
    const listing=admin.locator('.gym-grid > article').filter({hasText:`${name} gym`});await expect(listing).toContainText('Published');await listing.getByRole('button',{name:'Unpublish listing',exact:true}).click();await expect(listing).toContainText('Unpublished');
    const account=admin.locator('form.panel').filter({has:admin.getByRole('heading',{name,exact:true})});await account.getByRole('checkbox',{name:'GYM_OWNER',exact:true}).uncheck();await account.getByRole('button',{name:'Save account roles',exact:true}).click();await expect(admin.getByRole('status').filter({hasText:'Changes saved.'})).toBeVisible();
    await owner.reload();await expect(owner.getByRole('link',{name:'My gyms',exact:true})).toHaveCount(0);expect((await owner.request.get('/api/gyms/mine')).status()).toBe(403);await admin.screenshot({path:'test-results/gym-mvp-admin-desktop.png',fullPage:true,animations:'disabled'});expect(await admin.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBeTruthy();
  } finally { await ownerContext.close();await adminContext.close(); }
});
