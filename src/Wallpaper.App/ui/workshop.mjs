import {WorkshopSearchState,workshopUrl} from './workshop-model.mjs';
import {renderTitle} from './model.mjs';
const $=id=>document.getElementById(id);

export class WorkshopUi {
  constructor({call,configure,selectionChanged}){
    this.call=call;this.configure=configure;this.selectionChanged=selectionChanged;this.state=new WorkshopSearchState();this.visible=false;
    $('workshopSearchForm').onsubmit=e=>{e.preventDefault();this.search()};
    $('workshopCancel').onclick=()=>this.cancel();
    $('configureTavily').onclick=configure;
    $('workshopResults').onclick=e=>{const b=e.target.closest('[data-workshop-id]');if(b){this.state.selected=b.dataset.workshopId;this.render()}};
    $('workshopResults').ondblclick=e=>{if(e.target.closest('[data-workshop-id]'))this.open()};
    $('workshopOpen').onclick=()=>this.open();
    $('saveTavilyKey').onclick=()=>this.saveKey();
    $('clearTavilyKey').onclick=()=>this.clearKey();
    $('tavilyAccount').onclick=()=>call('tavilyAccount').catch(e=>this.keyError(e.message));
    $('settingsDialog').addEventListener('close',()=>{$('tavilyKey').value=''});
  }
  async refreshStatus(){const status=await this.call('tavilyStatus');this.configured=status.key.configured;$('workshopTestMode').hidden=!status.testMode;$('clearTavilyKey').disabled=!status.key.hasSavedKey;$('tavilyKeyStatus').textContent=status.key.source==='saved'?'已保存 Key，搜索时验证。':status.key.source==='environment'?'使用环境变量 TAVILY_API_KEY。':'尚未配置 Tavily API Key。';return status;}
  async show(){this.visible=true;this.render();try{await this.refreshStatus();if(this.state.phase==='idle')$('workshopHint').textContent=this.configured?'输入关键词，搜索 Steam 创意工坊壁纸页面。':'请先点击“配置 Tavily”，保存 API Key 后再搜索。'}catch(e){$('workshopHint').textContent=e.message}}
  hide(){this.visible=false;this.cancel();}
  cancel(){this.state.cancel();this.call('workshopCancel').catch(()=>{});this.render();}
  async search(){const query=$('workshopQuery').value.trim();if(!query){$('workshopHint').textContent='请输入壁纸关键词。';return}const revision=this.state.begin(query);this.render();try{await this.refreshStatus();if(revision!==this.state.revision)return;if(!this.configured)throw new Error('请先点击“配置 Tavily”，保存 API Key 后再搜索。');const response=await this.call('workshopSearch',{query});if(this.state.finish(revision,response.results))this.render();}catch(e){if(this.state.fail(revision,e.message))this.render()}}
  render(){const s=this.state;$('workshopCancel').hidden=s.phase!=='loading';$('workshopResults').setAttribute('aria-busy',String(s.phase==='loading'));$('workshopHint').textContent=s.phase==='loading'?'正在通过 Tavily 搜索 Steam 创意工坊…':s.phase==='error'?s.error:s.phase==='empty'?'没有找到相关条目。尝试更具体的关键词，或换用英文关键词。':s.results.length?`${s.results.length} 个相关页面`:'输入关键词，搜索 Steam 创意工坊壁纸页面。';
    $('workshopResults').innerHTML=s.results.map(x=>`<button class="card workshopCard ${x.id===s.selected?'selected':''}" data-workshop-id="${renderTitle(x.id)}" aria-pressed="${x.id===s.selected}"><span class="workshopCardType">Steam 创意工坊</span><strong>${renderTitle(x.title)}</strong><span class="workshopExcerpt">${renderTitle(x.description)}</span><span class="workshopCardFoot">查看详情</span></button>`).join('');
    const item=s.results.find(x=>x.id===s.selected);$('workshopTitle').textContent=item?.title||'选择创意工坊条目';$('workshopDescription').textContent=item?.description||'搜索后可查看相关页面的标题和摘要。';$('workshopUrl').textContent=item?.url||'';$('workshopOpen').disabled=!item;this.selectionChanged?.(!!item);
  }
  async open(){const item=this.state.results.find(x=>x.id===this.state.selected);const url=workshopUrl(item?.url);if(!url)return;try{await this.call('workshopOpen',{url})}catch(e){$('workshopHint').textContent=e.message}}
  keyError(text){$('tavilyKeyStatus').textContent=text;}
  async saveKey(){const key=$('tavilyKey').value;$('tavilyKey').value='';$('saveTavilyKey').disabled=true;try{await this.call('tavilySaveKey',{key});await this.refreshStatus();if(this.visible&&this.state.phase==='error'){this.state.cancel();this.render()}}catch(e){this.keyError(e.message)}finally{$('saveTavilyKey').disabled=false}}
  async clearKey(){try{await this.call('tavilyClearKey');await this.refreshStatus()}catch(e){this.keyError(e.message)}}
}
