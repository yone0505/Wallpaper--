using Wallpaper.Core;

var root = Path.Combine(Path.GetTempPath(), "wallpaper-tests-" + Guid.NewGuid());
Directory.CreateDirectory(root);
var passed = 0;
void Test(string name, Action check) { check(); Console.WriteLine("PASS: " + name); passed++; }
void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected InvalidDataException"); }
try
{
    var video = Path.Combine(root, "中文 视频.mp4"); File.WriteAllBytes(video, [0]);
    var html = Path.Combine(root, "index.html"); File.WriteAllText(html, "<h1>test</h1>");
    var project = Path.Combine(root, "project.json");
    Test("video unicode path", () => Equal("video", Library.Import(video).Kind));
    Test("web import", () => Equal("web", Library.Import(html).Kind));
    Test("missing rejected", () => Reject(() => Library.Import(Path.Combine(root,"missing.mp4"))));
    Test("unsupported rejected", () => Reject(() => Library.Import(Path.Combine(root,"file.exe"))));
    Test("project web resolves entry", () => { File.WriteAllText(project,"{\"title\":\"测试项目\",\"type\":\"web\",\"file\":\"index.html\"}"); var item=Library.Import(project); Equal("测试项目",item.Title); Equal(html,item.Path); });
    Test("scene project rejected", () => { File.WriteAllText(project,"{\"type\":\"scene\",\"file\":\"scene.pkg\"}"); Reject(()=>Library.Import(project)); });
    Test("project traversal rejected", () => { File.WriteAllText(project,"{\"type\":\"web\",\"file\":\"../outside.html\"}"); Reject(()=>Library.Import(project)); });
    Test("project absolute path rejected", () => { File.WriteAllText(project,"{\"type\":\"video\",\"file\":\"C:\\\\outside.mp4\"}"); Reject(()=>Library.Import(project)); });
    Test("malformed project rejected", () => { File.WriteAllText(project,"{"); Reject(()=>Library.Import(project)); });
    Test("stable id case insensitive", () => Equal(Library.Id(video),Library.Id(video.ToUpperInvariant())));
    Test("duplicate import retains first", () => { var s=new AppState(); Equal(true,s.Add(Library.Import(video))); Equal(false,s.Add(Library.Import(video))); Equal(1,s.Items.Count); });
    Test("volume clamps and interval bounds", () => { var s=new Settings { Volume=400,IntervalSeconds=0,Fit="bad"}; s.Normalize(); Equal(100,s.Volume); Equal(10,s.IntervalSeconds); Equal("cover",s.Fit); });
    Test("playlist advances wraps skips removed", () => { Equal("b",Playlist.Next(["a","b"],"a")); Equal("a",Playlist.Next(["a","b"],"b")); Equal("a",Playlist.Next(["a","b"],"removed")); Equal(null,Playlist.Next([],"a")); });
    Test("remove cleans monitor and playlist without deleting file", () => { var s=new AppState(); var i=Library.Import(video); s.Add(i); s.Assignments["display"]=i.Id; s.Playlist.Add(i.Id); s.Remove(i.Id); Equal(0,s.Assignments.Count); Equal(0,s.Playlist.Count); Equal(true,File.Exists(video)); });
    Test("state round trip", () => { var store=new StateStore(Path.Combine(root,"state")); var s=new AppState(); s.Add(Library.Import(video)); store.Save(s); Equal(1,store.Load().State.Items.Count); });
    Test("corrupt state preserved and reported", () => { var dir=Path.Combine(root,"corrupt"); var store=new StateStore(dir); Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir,"state.json"),"{"); var loaded=store.Load(); Equal(0,loaded.State.Items.Count); Equal(true,loaded.Warning?.Contains("损坏")??false); Equal(1,Directory.GetFiles(dir,"state.corrupt-*.json").Length); });
    Test("fullscreen policy honors setting", () => { Equal(true,PlaybackPolicy.ShouldPause(true,true)); Equal(false,PlaybackPolicy.ShouldPause(true,false)); Equal(false,PlaybackPolicy.ShouldPause(false,true)); });
    Test("modern desktop child WorkerW", () => Equal((long)3, DesktopTopology.Select([new(1,0,"Progman"),new(2,1,"SHELLDLL_DefView"),new(3,1,"WorkerW"),new(4,0,"WorkerW")])));
    Test("legacy desktop following icon host", () => Equal((long)4,DesktopTopology.Select([new(1,0,"Progman"),new(2,0,"WorkerW"),new(3,2,"SHELLDLL_DefView"),new(4,0,"WorkerW")])));
    Test("unrelated WorkerW rejected", () => Equal((long)0,DesktopTopology.Select([new(5,0,"WorkerW")])));
    Test("bridge unknown method rejected", () => Reject(()=>BridgeRequest.Parse("{\"id\":1,\"method\":\"shell\",\"args\":{}}")));
    Test("bridge valid command", () => Equal("apply",BridgeRequest.Parse("{\"id\":2,\"method\":\"apply\",\"args\":{\"id\":\"x\"}}").Method));
    Test("bridge nonobject and malformed rejected", () => { Reject(()=>BridgeRequest.Parse("[]")); Reject(()=>BridgeRequest.Parse("{")); });
    Console.WriteLine($"All {passed} core tests passed.");
}
finally { Directory.Delete(root, true); }
