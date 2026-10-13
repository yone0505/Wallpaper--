export function visibleItems(items, search='', kind='all', sort='recent') {
  const result=items.filter(x => x.title.toLocaleLowerCase().includes(search.toLocaleLowerCase()) && (kind==='all'||x.kind===kind));
  return sort==='title' ? result.sort((a,b)=>a.title.localeCompare(b.title,'zh-CN')) : result.reverse();
}
export function selectedItem(items,id) { return items.find(x=>x.id===id)||items[0]||null; }
export function renderTitle(value) { return String(value).replace(/[&<>"']/g,x=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[x])); }
export function safeUrl(value) { try { const u=new URL(value); return u.protocol==='https:' && /^[a-z0-9-]+\.wallpaper\.local$/.test(u.hostname) ? u.href : ''; } catch { return ''; } }
