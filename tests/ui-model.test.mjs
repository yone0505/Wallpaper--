import {test} from 'node:test';
import assert from 'node:assert/strict';
import {visibleItems, selectedItem, renderTitle, safeUrl} from '../src/Wallpaper.App/ui/model.mjs';
const items=[{id:'a',title:'星河',kind:'web'},{id:'b',title:'Ocean',kind:'video'},{id:'c',title:'Alpha',kind:'video'}];
test('case insensitive title search',()=>assert.deepEqual(visibleItems(items,'oC','all','title').map(x=>x.id),['b']));
test('kind filter and sort preserve original',()=>{assert.deepEqual(visibleItems(items,'','video','title').map(x=>x.id),['c','b']); assert.equal(items[0].id,'a')});
test('missing selection uses first and empty stays null',()=>{assert.equal(selectedItem(items,'removed').id,'a');assert.equal(selectedItem([],'x'),null)});
test('untrusted title escaped',()=>assert.equal(renderTitle('<img src=x> & "'), '&lt;img src=x&gt; &amp; &quot;'));
test('thumbnail protocols constrained',()=>{assert.equal(safeUrl('javascript:alert(1)'), '');assert.equal(safeUrl('https://media-a.wallpaper.local/image.png'),'https://media-a.wallpaper.local/image.png');assert.equal(safeUrl('https://outside.com/x'),'')});
