import {visibleItems,selectedItem,renderTitle,safeUrl} from './model.mjs';
const $=id=>document.getElementById(id);
let serial=0,state=null,thumbnails={},selected=null,kind='all',playlistOpen=false;
const requests=new Map();
function call(method,args={}) { return new Promise((resolve,reject)=>{const id=++serial;const timeout=setTimeout(()=>{requests.delete(id);reject(new Error('操作超时，请重试。'))},120000);requests.set(id,{resolve,reject,timeout});window.chrome.webview.postMessage({id,method,args});}); }
window.wallpaper={call};
window.chrome.webview.addEventListener('message',e=>{const data=e.data;if(data.eventName==='state'){update(data.result);return}const r=requests.get(data.id);if(!r)return;clearTimeout(r.timeout);requests.delete(data.id);if(data.error)r.reject(new Error(data.error));else r.resolve(data.result)});
function message(text){$('message').textContent=text;}
async function action(method,args={}){try{return await call(method,args)}catch(e){message(e.message);info('操作未完成',e.message);throw e;}}
const symbol='<svg viewBox="0 0 100 100"><circle cx="50" cy="50" r="28"/><ellipse cx="50" cy="50" rx="45" ry="15" transform="rotate(-25 50 50)"/><circle cx="78" cy="23" r="4"/></svg>';
function media(item,large=false){const t=thumbnails[item.id];if(!t)return `<div class="fallback">${symbol}</div>`;const src=safeUrl(t.source);if(!src)return `<div class="fallback">${symbol}</div>`;if(t.hasPreview)return `<img src="${renderTitle(src)}" alt="">`;if(item.kind==='video')return `<video src="${renderTitle(src)}#t=0.3" muted preload="metadata" ${large?'controls':''}></video>`;return `<div class="fallback">${symbol}</div>`;}
function update(snapshot){if(!snapshot)return;state=snapshot.state;thumbnails=snapshot.thumbnails;const current=$('monitor').value;const monitorItems=state.monitors.map(m=>`<option value="${renderTitle(m.id)}">${renderTitle(m.name)}</option>`).join('');if($('monitor').innerHTML!==monitorItems)$('monitor').innerHTML=monitorItems;if(state.monitors.some(m=>m.id===current))$('monitor').value=current;$('fit').value=state.settings.fit;$('quickMuted').checked=state.settings.muted;$('quickVolume').value=state.settings.volume;$('playlistEnabled').checked=state.settings.playlistEnabled;render();if(state.warning)message(state.warning);}
function render(){if(!state)return;const all=state.items;const shown=visibleItems(all,$('search').value,kind,$('sort').value);const item=selectedItem(all,selected);selected=item?.id??null;$('grid').innerHTML=shown.map(x=>`<button class="card ${x.id===selected?'selected':''}" data-id="${renderTitle(x.id)}" aria-label="${renderTitle(x.title)}" aria-pressed="${x.id===selected}">${media(x)}<span class="badge">${x.kind==='web'?'网页':'视频'}</span><span class="caption">${renderTitle(x.title)}</span></button>`).join('');$('empty').hidden=shown.length>0;$('count').textContent=`${shown.length} / ${all.length} 个壁纸`;$('libraryCount').textContent=`共 ${all.length} 个壁纸`;
  $('itemTitle').textContent=item?.title||'选择壁纸';$('itemKind').innerHTML=item?`<span>${item.kind==='web'?'网页':'视频'} · 本地</span>`:'';$('itemPath').textContent=item?.path||'';
  if($('preview').dataset.id!==selected){$('preview').dataset.id=selected||'';$('preview').innerHTML=item?media(item,true):'';}
  for(const id of ['apply','addPlaylist','remove'])$(id).disabled=!item;
  const playing=state.playback.find(x=>x.monitor===$('monitor').value);$('status').textContent=playing?(playing.error||((playing.paused?'已暂停 · ':'正在播放 · ')+(all.find(x=>x.id===playing.id)?.title||''))):'尚未播放';$('pause').textContent=state.manualPause?'继续':'暂停';$('pause').disabled=state.playback.length===0;$('stop').disabled=!playing;
  $('playlistCount').textContent=state.playlist.length;$('playlistItems').innerHTML=state.playlist.map(id=>{const x=all.find(x=>x.id===id);return x?`<button data-remove-playlist="${renderTitle(id)}" title="单击移出播放列表">${renderTitle(x.title)}<small>${x.kind==='web'?'网页':'视频'} · 点击移除</small></button>`:''}).join('');
}
function info(title,text){$('infoTitle').textContent=title;$('infoText').textContent=text;if(!$('infoDialog').open)$('infoDialog').showModal();}
$('infoClose').onclick=()=>$('infoDialog').close();
for(const input of ['search','sort'])$(input).addEventListener('input',render);
$('grid').onclick=e=>{const button=e.target.closest('[data-id]');if(button){selected=button.dataset.id;render()}};
$('grid').ondblclick=e=>{const button=e.target.closest('[data-id]');if(button){selected=button.dataset.id;apply()}};
async function apply(){if(selected){await action('apply',{id:selected,monitor:$('monitor').value});message('壁纸已应用到所选显示器。')}}
$('apply').onclick=()=>apply().catch(()=>{});
$('ok').onclick=async()=>{try{await apply();await call('window',{action:'close'})}catch{}};
$('stop').onclick=()=>action('stop',{monitor:$('monitor').value}).catch(()=>{});
$('pause').onclick=()=>action('pause').catch(()=>{});
$('monitor').onchange=render;
$('remove').onclick=async()=>{if(selected){try{await action('remove',{id:selected});message('已从本地库移除，源文件保留。')}catch{}}};
$('addPlaylist').onclick=async()=>{if(selected){try{await action('playlist',{ids:[...new Set([...state.playlist,selected])]});playlistOpen=true;$('playlistItems').hidden=false;message('已加入播放列表。')}catch{}}};
$('playlistItems').onclick=e=>{const b=e.target.closest('[data-remove-playlist]');if(b)action('playlist',{ids:state.playlist.filter(x=>x!==b.dataset.removePlaylist)}).catch(()=>{})};
$('togglePlaylist').onclick=()=>{playlistOpen=!playlistOpen;$('playlistItems').hidden=!playlistOpen};
$('next').onclick=()=>action('next',{monitor:$('monitor').value}).catch(()=>{});
async function quickSettings(){await action('settings',{...state.settings,fit:$('fit').value,muted:$('quickMuted').checked,volume:Number($('quickVolume').value),playlistEnabled:$('playlistEnabled').checked});}
for(const id of ['fit','quickMuted','quickVolume','playlistEnabled'])$(id).onchange=()=>quickSettings().catch(()=>{});
async function importFiles(folder=false){const count=await action(folder?'importFolder':'import');message(`新导入 ${count} 个壁纸`);}
for(const id of ['import','emptyImport'])$(id).onclick=()=>importFiles().catch(()=>{});
$('importFolder').onclick=()=>importFiles(true).catch(()=>{});
$('toggleFilter').onclick=()=>{$('filters').hidden=!$('filters').hidden};
document.querySelectorAll('[name=kind]').forEach(x=>x.onchange=()=>{kind=x.value;render()});
document.querySelectorAll('[data-window]').forEach(x=>x.onclick=()=>call('window',{action:x.dataset.window}));
$('titlebar').onmousedown=e=>{if(!e.target.closest('button'))call('window',{action:'drag'})};
document.querySelectorAll('[data-source]').forEach(x=>x.onclick=()=>{if(x.dataset.source!=='installed')info(x.textContent,'Steam 内容浏览和订阅接口尚未实现。当前版本可以导入本机已下载的视频及网页项目。')});
$('mobile').onclick=()=>info('移动设备','手机配对和传输尚未实现，仍保留在完整功能验收清单中。');
$('editor').onclick=()=>info('壁纸编辑器','场景引擎和编辑器尚未实现。当前可导入本地视频、HTML 和 video/web project.json。');
$('display').onclick=()=>{$('monitor').focus();message('在右侧选择显示器，再应用所选壁纸。')};
function tab(name){document.querySelectorAll('[data-panel]').forEach(x=>x.hidden=x.dataset.panel!==name);document.querySelectorAll('[data-tab]').forEach(x=>x.classList.toggle('selected',x.dataset.tab===name))}
document.querySelectorAll('[data-tab]').forEach(x=>x.onclick=()=>tab(x.dataset.tab));
$('settings').onclick=()=>{const s=state.settings;$('fullscreen').value=String(s.pauseOnFullscreen);$('settingsFit').value=s.fit;$('volume').value=s.volume;$('volumeValue').textContent=s.volume+'%';$('muted').checked=s.muted;$('interval').value=s.intervalSeconds;$('autoPlaylist').checked=s.playlistEnabled;tab('performance');$('settingsDialog').showModal()};
$('volume').oninput=()=>{$('volumeValue').textContent=$('volume').value+'%'};
for(const id of ['closeSettings','cancelSettings'])$(id).onclick=()=>$('settingsDialog').close();
$('saveSettings').onclick=async()=>{try{await action('settings',{...state.settings,volume:Number($('volume').value),muted:$('muted').checked,fit:$('settingsFit').value,pauseOnFullscreen:$('fullscreen').value==='true',intervalSeconds:Number($('interval').value),playlistEnabled:$('autoPlaylist').checked});$('settingsDialog').close();message('设置已保存。')}catch{}};
$('exitApp').onclick=()=>call('window',{action:'exit'});
call('state').then(update).catch(e=>message(e.message));
