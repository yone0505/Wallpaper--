import {test} from 'node:test';
import assert from 'node:assert/strict';
import {WorkshopSearchState,workshopUrl} from '../src/Wallpaper.App/ui/workshop-model.mjs';
test('older response cannot replace newest search',()=>{const s=new WorkshopSearchState();const a=s.begin('first'),b=s.begin('second');assert.equal(s.finish(a,[{id:'old'}]),false);assert.equal(s.finish(b,[{id:'new'}]),true);assert.equal(s.results[0].id,'new');});
test('cancel invalidates pending result and preserves existing results',()=>{const s=new WorkshopSearchState();s.finish(s.begin('a'),[{id:'a'}]);const b=s.begin('b');s.cancel();assert.equal(s.finish(b,[{id:'b'}]),false);assert.equal(s.results[0].id,'a');assert.equal(s.phase,'idle');});
test('search selection repairs removed result and empty state',()=>{const s=new WorkshopSearchState();s.finish(s.begin('a'),[{id:'a'}]);assert.equal(s.selected,'a');s.finish(s.begin('empty'),[]);assert.equal(s.selected,null);assert.equal(s.phase,'empty');});
test('URL output only permits canonical Steam details',()=>{assert.equal(workshopUrl('https://steamcommunity.com/sharedfiles/filedetails/?id=12'),'https://steamcommunity.com/sharedfiles/filedetails/?id=12');assert.equal(workshopUrl('https://steamcommunity.com.evil.com/sharedfiles/filedetails/?id=12'),'');assert.equal(workshopUrl('javascript:alert(1)'),'');});
