export class WorkshopSearchState {
  constructor(){this.revision=0;this.phase='idle';this.results=[];this.selected=null;this.error='';this.query='';}
  begin(query){this.query=query;this.phase='loading';this.error='';return ++this.revision;}
  finish(revision,results){if(revision!==this.revision)return false;this.results=results;this.selected=results.some(x=>x.id===this.selected)?this.selected:results[0]?.id??null;this.phase=results.length?'ready':'empty';return true;}
  fail(revision,error){if(revision!==this.revision)return false;this.error=error;this.phase='error';return true;}
  cancel(){this.revision++;this.phase='idle';this.error='';}
}
export function workshopUrl(value){try{const u=new URL(value);return u.protocol==='https:'&&u.hostname==='steamcommunity.com'&&!u.port&&!u.username&&!u.password&&u.pathname==='/sharedfiles/filedetails/'&&/^\?id=[1-9]\d*$/.test(u.search)&&!u.hash?u.href:'';}catch{return '';}}
