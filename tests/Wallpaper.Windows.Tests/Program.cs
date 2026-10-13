using Wallpaper.App;
using Wallpaper.Core;
var target="WallpaperDesktop/Test/"+Guid.NewGuid();
var vault=new TavilyKeyStore(target,()=>null);
void Assert(bool value,string message) { if(!value)throw new Exception(message); Console.WriteLine("PASS: "+message); }
try
{
 Assert(!vault.Status().Configured,"new credential target is not configured");
 vault.Save("tvly-original-test-key");
 Assert(vault.Read()=="tvly-original-test-key","Windows vault round trip");
 Assert(vault.Status().Source=="saved","status reports saved credential");
 var status=System.Text.Json.JsonSerializer.Serialize(vault.Status());
 Assert(!status.Contains("tvly-original-test-key"),"credential status contains no secret");
 vault.Clear(); Assert(!vault.Status().Configured,"credential removed");
 var env=new TavilyKeyStore(target,()=>"tvly-environment-test-key");
 Assert(env.Read()=="tvly-environment-test-key"&&env.Status().Source=="environment","environment fallback");
 env.Save("tvly-saved-test-key");Assert(env.Read()=="tvly-saved-test-key","saved credential takes precedence");
 try{env.Save("bad\r\nkey");throw new Exception("Expected invalid key");}catch(InvalidDataException){}
 Assert(env.Read()=="tvly-saved-test-key","invalid key does not replace saved credential");
 env.Clear(); Assert(env.Read()=="tvly-environment-test-key" && !env.Status().HasSavedKey,"clearing saved key restores environment fallback");
 Console.WriteLine("All 9 Windows credential tests passed.");
}
finally { vault.Clear(); }
