using System.Net;
using System.Text.Json;
using Wallpaper.Core;

var passed=0;
void Equal<T>(T expected,T actual) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}"); }
async Task Test(string name,Func<Task> check) { await check(); Console.WriteLine("PASS: "+name); passed++; }
async Task Reject(Func<Task> call,string fragment) { try { await call(); } catch(InvalidDataException ex) { if(ex.Message.Contains(fragment)) return; throw; } throw new Exception("Expected InvalidDataException"); }
const string key="tvly-test-only-secret";
const string valid="https://steamcommunity.com/sharedfiles/filedetails/?id=2130919433";
var fixture="""
{"results":[
 {"title":"Steam Workshop::测试壁纸","url":"https://steamcommunity.com/sharedfiles/filedetails/?id=2130919433&l=schinese","content":"Wallpaper Engine • sunset"},
 {"title":"Duplicate","url":"https://steamcommunity.com/workshop/filedetails/?id=2130919433","content":"same"},
 {"title":"Elsewhere","url":"https://example.com/other","content":"Wallpaper Engine"},
 {"title":"browse","url":"https://steamcommunity.com/workshop/browse/?appid=431960"},
 {"title":"spoof","url":"https://steamcommunity.com.evil.example/sharedfiles/filedetails/?id=12"},
 {"title":"other game","url":"https://steamcommunity.com/sharedfiles/filedetails/?id=33&appid=440"},
 {"title":"Malformed","url":null},
 {"title":"<img onerror=alert(1)>","url":"https://steamcommunity.com/sharedfiles/filedetails/?id=21","content":"<script>"}
]}
""";
await Test("request uses official endpoint bearer and Steam restriction",async()=>{
 var handler=new FakeHandler(async(r,ct)=>{Equal(HttpMethod.Post,r.Method);Equal("https://api.tavily.com/search",r.RequestUri!.AbsoluteUri);Equal("Bearer",r.Headers.Authorization!.Scheme);Equal(key,r.Headers.Authorization.Parameter);using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));Equal("steamcommunity.com",body.RootElement.GetProperty("include_domains")[0].GetString());Equal("basic",body.RootElement.GetProperty("search_depth").GetString());Equal(10,body.RootElement.GetProperty("max_results").GetInt32());Equal(false,body.RootElement.GetProperty("auto_parameters").GetBoolean());Equal(true,body.RootElement.GetProperty("query").GetString()!.Contains("Wallpaper Engine"));Equal(false,body.RootElement.TryGetProperty("api_key",out _));return Response(HttpStatusCode.OK,fixture);});
 using var http=new HttpClient(handler);var result=await new TavilyWorkshopClient(http).SearchAsync("黄昏",key);Equal(2,result.Results.Count);Equal(valid,result.Results[0].Url);
});
await Test("missing key sends no request",async()=>{using var http=new HttpClient(new FakeHandler((_,_)=>throw new Exception("must not send")));await Reject(()=>new TavilyWorkshopClient(http).SearchAsync("x",""),"Key");});
await Test("empty and excessive query rejected before request",async()=>{using var http=new HttpClient(new FakeHandler((_,_)=>throw new Exception("must not send")));var client=new TavilyWorkshopClient(http);await Reject(()=>client.SearchAsync(" ",key),"关键词");await Reject(()=>client.SearchAsync(new string('x',201),key),"200");});
await Test("key newline rejected without exposing key",async()=>{using var http=new HttpClient(new FakeHandler((_,_)=>throw new Exception("must not send")));await Reject(()=>new TavilyWorkshopClient(http).SearchAsync("x",key+"\r\n"+"bad"),"格式");});
foreach(var status in new[]{HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden,HttpStatusCode.TooManyRequests,(HttpStatusCode)432,(HttpStatusCode)433,HttpStatusCode.InternalServerError})
 await Test("friendly secret-free HTTP error "+(int)status,async()=>{using var http=new HttpClient(new FakeHandler((_,_)=>Task.FromResult(Response(status,"{\"detail\":{\"error\":\""+key+"\"}}"))));try{await new TavilyWorkshopClient(http).SearchAsync("x",key);throw new Exception("Expected error");}catch(InvalidDataException ex){Equal(false,ex.Message.Contains(key));Equal(false,ex.Message.Contains("detail"));}});
await Test("malformed and absent results rejected",async()=>{foreach(var text in new[]{"{","{}","{\"results\":null}"}){using var http=new HttpClient(new FakeHandler((_,_)=>Task.FromResult(Response(HttpStatusCode.OK,text))));await Reject(()=>new TavilyWorkshopClient(http).SearchAsync("x",key),"格式");}});
await Test("empty results are valid",async()=>{using var http=new HttpClient(new FakeHandler((_,_)=>Task.FromResult(Response(HttpStatusCode.OK,"{\"results\":[]}"))));Equal(0,(await new TavilyWorkshopClient(http).SearchAsync("x",key)).Results.Count);});
await Test("caller cancellation propagates",async()=>{using var cts=new CancellationTokenSource();using var http=new HttpClient(new FakeHandler(async(_,ct)=>{await Task.Delay(5000,ct);return Response(HttpStatusCode.OK,fixture);}));var pending=new TavilyWorkshopClient(http).SearchAsync("x",key,cts.Token);cts.Cancel();try{await pending;throw new Exception("Expected cancellation");}catch(OperationCanceledException){}});
await Test("timeout also covers stalled response body",async()=>{using var http=new HttpClient(new FakeHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StalledContent()}))){Timeout=TimeSpan.FromMilliseconds(50)};await Reject(()=>new TavilyWorkshopClient(http).SearchAsync("x",key),"超时");});
await Test("unsafe external URLs cannot be opened",()=>{foreach(var url in new[]{"file:///C:/Windows", "steam://run/431960", "javascript:alert(1)", "https://steamcommunity.com@evil.example/sharedfiles/filedetails/?id=12", "https://steamcommunity.com/sharedfiles/filedetails/?id=bad","https://steamcommunity.com:444/sharedfiles/filedetails/?id=12"})Equal(null,SteamWorkshop.CanonicalUrl(url));Equal(valid,SteamWorkshop.CanonicalUrl(valid+"&l=schinese"));return Task.CompletedTask;});
Console.WriteLine($"All {passed} search tests passed.");
static HttpResponseMessage Response(HttpStatusCode status,string json)=>new(status){Content=new StringContent(json)};
sealed class FakeHandler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send):HttpMessageHandler
{ protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>send(request,token); }
sealed class StalledContent:HttpContent
{
 protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context)=>throw new NotSupportedException();
 protected override bool TryComputeLength(out long length){length=0;return false;}
 protected override Task<Stream> CreateContentReadStreamAsync()=>Task.FromResult<Stream>(new StalledStream());
}
sealed class StalledStream:Stream
{
 public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
 public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
 public override void Flush(){}public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();
 public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){await Task.Delay(500,token);return 0;}
 public override long Seek(long o,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
}
