import {createRequire} from 'node:module';
import {createServer} from 'node:http';
import {spawn} from 'node:child_process';
import {mkdir,readFile} from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {randomUUID} from 'node:crypto';
const require=createRequire(path.resolve('work/qa/package.json'));
const {chromium}=require('playwright');
const profile=path.resolve('work','workshop-gui-'+randomUUID());
const key='tvly-gui-test-only';let count=0;let slowSeen=0;let child;let browser;let page;
process.on('exit',()=>{if(child&&child.exitCode===null)child.kill();});
const server=createServer(async(req,res)=>{
  let body='';for await(const chunk of req)body+=chunk;
  count++;assert.equal(req.url,'/search');assert.equal(req.headers.authorization,`Bearer ${key}`);
  const data=JSON.parse(body);assert.deepEqual(data.include_domains,['steamcommunity.com']);assert.equal(data.search_depth,'basic');assert.equal(data.max_results,10);assert.equal(data.api_key,undefined);
  res.setHeader('Content-Type','application/json');
  if(data.query.includes('rate')){res.statusCode=429;res.end(JSON.stringify({detail:{error:key}}));return}
  const query=data.query.includes('empty')?'empty':data.query.includes('slow')?'slow':'海洋';
  const reply=()=>res.end(JSON.stringify({results:query==='empty'?[]:[
    {title:'测试响应：'+query,url:'https://steamcommunity.com/sharedfiles/filedetails/?id=2130919433&l=schinese',content:'Wallpaper Engine 搜索结果测试摘要'},
    {title:'重复条目',url:'https://steamcommunity.com/sharedfiles/filedetails/?id=2130919433'},
    {title:'<img src=x onerror=alert(1)>',url:'https://steamcommunity.com/sharedfiles/filedetails/?id=9999',content:'<script>window.workshopInjected=true</script>'},
    {title:'外部网站',url:'https://example.com/unsafe'},
    {title:'另一款游戏',url:'https://steamcommunity.com/sharedfiles/filedetails/?id=44&appid=440'}]}));
  if(query==='slow'){slowSeen++;const timer=setTimeout(reply,1500);res.once('close',()=>clearTimeout(timer));}else reply();
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const url=`http://127.0.0.1:${server.address().port}/search`;
async function launch(){
 child=spawn(process.env.WALLPAPER_EXE,['--data-dir',profile,'--debug-port','32222','--qa-tavily-url',url],{windowsHide:true,stdio:'ignore',env:{...process.env,TAVILY_API_KEY:''}});
 for(let i=0;i<100;i++){try{browser=await chromium.connectOverCDP('http://127.0.0.1:32222',{timeout:500});break}catch{await new Promise(r=>setTimeout(r,100))}}
 assert.ok(browser,'test instance starts');
 for(let i=0;i<50&&!page;i++){page=browser.contexts().flatMap(c=>c.pages()).find(p=>p.url().includes('manager.wallpaper.local'));if(!page)await new Promise(r=>setTimeout(r,100));}
 await page.waitForSelector('.card');
}
async function exit(){
 await page.evaluate(()=>window.wallpaper.call('window',{action:'exit'})).catch(()=>{});
 await browser.close().catch(()=>{});
 for(let i=0;i<50&&child.exitCode===null;i++)await new Promise(r=>setTimeout(r,100));
 assert.ok(child.exitCode!==null,'normal shutdown');browser=null;page=null;
}
async function search(query){await page.locator('#workshopQuery').fill(query);await page.locator('#workshopSearch').click();}
try{
 await launch();
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.locator('[data-source=workshop]').click();
 await page.waitForFunction(()=>document.querySelector('#workshopHint').textContent.includes('配置'));
 await search('海洋');await page.waitForFunction(()=>document.querySelector('#workshopHint').textContent.includes('Key'));assert.equal(count,0);
 await page.locator('#configureTavily').click();await page.locator('#tavilyKey').fill(key);await page.locator('#saveTavilyKey').click();
 await page.waitForFunction(()=>document.querySelector('#tavilyKeyStatus').textContent.includes('已保存'));
 assert.equal(await page.locator('#tavilyKey').inputValue(),'');
 await page.locator('#cancelSettings').click();
 await search('海洋');await page.waitForSelector('[data-workshop-id]');
 assert.equal(await page.locator('[data-workshop-id]').count(),2);
 assert.equal(await page.locator('#workshopResults img').count(),0);
 assert.equal(await page.evaluate(()=>window.workshopInjected),undefined);
 assert.equal(await page.locator('#workshopOpen').isEnabled(),true);
 await page.locator('[data-workshop-id="9999"]').click();assert.ok((await page.locator('#workshopDescription').textContent()).includes('<script>'));
 await page.locator('[data-workshop-id="2130919433"]').click();
 await mkdir('work/validation',{recursive:true});await page.screenshot({path:'work/validation/workshop.png'});
 const requestCount=count;await search('slow');while(slowSeen===0)await new Promise(r=>setTimeout(r,30));await search('海洋');await page.waitForFunction(()=>document.querySelector('#workshopHint').textContent==='2 个相关页面');
 assert.ok((await page.locator('#workshopTitle').textContent()).includes('海洋'));assert.ok(count>=requestCount+2);
 await search('slow');await page.locator('#workshopCancel').click();await page.waitForFunction(()=>document.querySelector('#workshopCancel').hidden);
 await search('empty');await page.waitForFunction(()=>document.querySelector('#workshopHint').textContent.includes('没有找到'));assert.equal(await page.locator('[data-workshop-id]').count(),0);
 await search('rate');await page.waitForFunction(()=>document.querySelector('#workshopHint').textContent.includes('频繁'));assert.equal((await page.locator('#workshopHint').textContent()).includes(key),false);
 const state=JSON.parse(await readFile(path.join(profile,'state.json'),'utf8'));assert.equal(JSON.stringify(state).includes(key),false);
 const status=await page.evaluate(()=>window.wallpaper.call('tavilyStatus'));assert.equal(status.key.configured,true);assert.equal(status.testMode,true);assert.equal(JSON.stringify(status).includes(key),false);
 const blocked=await page.evaluate(()=>window.wallpaper.call('workshopOpen',{url:'file:///C:/Windows'}).then(()=>false,()=>true));assert.equal(blocked,true);
 await page.locator('[data-source=installed]').click();assert.equal(await page.locator('#installedPage').isVisible(),true);assert.equal(await page.locator('#ok').textContent(),'确定');
 assert.deepEqual(errors,[]);await exit();await launch();
 assert.equal((await page.evaluate(()=>window.wallpaper.call('tavilyStatus'))).key.configured,true,'credential persists across application restart');
 await page.locator('#settings').click();await page.locator('[data-tab=plugins]').click();await page.waitForFunction(()=>document.querySelector('#tavilyKeyStatus').textContent.includes('已保存'));
 await page.screenshot({path:'work/validation/tavily-settings.png'});
 await page.locator('#clearTavilyKey').click();await page.waitForFunction(()=>document.querySelector('#tavilyKeyStatus').textContent.includes('尚未配置'));
 await page.locator('#cancelSettings').click();
 console.log('PASS: real WebView2 to local HTTP test server, credential save/restart/clear, Steam filtering, escaping, empty/error states, cancellation/latest result, local library return; live Tavily search requires user key');
}finally{
 if(page){await page.evaluate(()=>window.wallpaper.call('tavilyClearKey')).catch(()=>{});await exit().catch(()=>{});}
 if(child&&child.exitCode===null)child.kill();
 await new Promise(resolve=>server.close(resolve));
}
