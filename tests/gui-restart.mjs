import {createRequire} from 'node:module';
import {spawn} from 'node:child_process';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
const require=createRequire(path.resolve('work/qa/package.json'));
const {chromium}=require('playwright');
const endpoint='http://127.0.0.1:32222';
let browser=await chromium.connectOverCDP(endpoint);
let page=browser.contexts()[0].pages().find(x=>x.url().includes('manager.wallpaper.local'));
const snap=(await page.evaluate(()=>window.wallpaper.call('state'))).state;
const diag=await page.evaluate(()=>window.wallpaper.call('diagnostics'));
const web=snap.items.find(x=>x.kind==='web');
const monitor=snap.monitors[0].id;
await page.evaluate(({id,monitor})=>window.wallpaper.call('apply',{id,monitor}),{id:web.id,monitor});
await page.evaluate(()=>window.wallpaper.call('settings',{volume:42,muted:true,fit:'contain',pauseOnFullscreen:true,intervalSeconds:300}));
const exit=page.evaluate(()=>window.wallpaper.call('window',{action:'exit'})).catch(()=>{});
await exit; await browser.close().catch(()=>{});
for(let i=0;i<50;i++){try{process.kill(diag.processId,0)}catch{break}await new Promise(r=>setTimeout(r,100));}
const file=JSON.parse(await readFile(path.join(diag.dataDirectory,'state.json'),'utf8'));
assert.equal(file.assignments[monitor],web.id);
const exe=process.env.WALLPAPER_EXE;
assert.ok(exe,'WALLPAPER_EXE must point to the self-contained release');
const child=spawn(exe,['--data-dir',diag.dataDirectory,'--debug-port','32222'],{windowsHide:true,stdio:'ignore'});
process.on('exit',()=>{if(child.exitCode===null)child.kill();});
let connected=false;
for(let i=0;i<100&&!connected;i++){try{browser=await chromium.connectOverCDP(endpoint,{timeout:500});connected=true}catch{await new Promise(r=>setTimeout(r,100))}}
assert.ok(connected,'released application restarts without .NET SDK environment');
page=browser.contexts()[0].pages().find(x=>x.url().includes('manager.wallpaper.local'));
if(!page){await new Promise(r=>setTimeout(r,300));page=browser.contexts()[0].pages()[0];}
await page.waitForSelector('.card');
await page.waitForFunction(async(id)=>{const d=await window.wallpaper.call('diagnostics');return d.windows.length===1&&d.windows[0].ready&&d.windows[0].id===id},web.id);
const restored=(await page.evaluate(()=>window.wallpaper.call('state'))).state;
assert.equal(restored.settings.volume,42); assert.equal(restored.settings.fit,'contain');
await page.locator('.card').filter({hasText:'极光'}).click();
await page.locator('#remove').click();
await page.waitForFunction(()=>document.querySelector('#status').textContent==='尚未播放');
assert.equal((await page.evaluate(()=>window.wallpaper.call('diagnostics'))).windows.length,0);
await readFile(web.path); // removing a library item must keep the original source.
await mkdir('work/validation',{recursive:true});
await writeFile('work/validation/restart.json',JSON.stringify({restored:true,sourceRetained:true,settings:restored.settings},null,2));
await page.evaluate(()=>window.wallpaper.call('window',{action:'exit'})).catch(()=>{});
await browser.close().catch(()=>{});
await new Promise(resolve=>{if(child.exitCode!==null)return resolve();const timer=setTimeout(resolve,5000);child.once('exit',()=>{clearTimeout(timer);resolve()});});
assert.ok(child.exitCode!==null,'application exits and closes its renderer');
console.log('PASS: self-contained restart restores wallpaper/settings, removal preserves source, normal exit');
