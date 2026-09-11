// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
using NeonOrbit.Core;
using System.Globalization;

int passed = 0, failed = 0;
var results = new List<string>();
void Test(string name, Action test)
{
    try { test(); passed++; results.Add("PASS " + name); Console.WriteLine(results[^1]); }
    catch (Exception ex) { failed++; results.Add("FAIL " + name + ": " + ex.Message); Console.WriteLine(results[^1]); }
}
void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
void Near(double a, double b, double tolerance = 1e-8) => Check(Math.Abs(a-b) < tolerance, $"{a} != {b} ± {tolerance}");
void Throws(Action f) { try { f(); } catch (Exception) { return; } throw new Exception("Expected rejection"); }
var settings = new LabSettings { RandomMode = RandomMode.Seeded };
Test("Seeded stream reproducibility", () => {
    var a = new RandomStream(RandomMode.Seeded, 42); var b = new RandomStream(RandomMode.Seeded, 42);
    for (int i=0;i<10000;i++) Near(a.Next(),b.Next());
});
Test("Independent seeds produce distinct streams", () => Check(new RandomStream(RandomMode.Seeded,1).Next() != new RandomStream(RandomMode.Seeded,2).Next()));
Test("Hybrid event replay reproduces all draws without secure entropy", () => {
    var events = new List<EntropyEvent>(); ulong count = 0;
    var a = new RandomStream(RandomMode.Hybrid,42,()=> ++count % 5 == 0 ? 100 : count);
    a.Injected += events.Add; var samples = Enumerable.Range(0,1000).Select(_=>a.Next()).ToArray();
    Check(events.Count>0);
    var b = new RandomStream(RandomMode.Hybrid,42,()=>throw new Exception("Unexpected entropy"),events);
    foreach(var value in samples) Near(value,b.Next());
});
Test("Secure stream is bounded", () => { var r=new RandomStream(RandomMode.Secure,0);for(int i=0;i<2000;i++){var x=r.Next();Check(x>=0 && x<=1);} });
Test("All distribution hard limits across 100k samples each", () => {
    var rng=new RandomStream(RandomMode.Seeded,2026);
    foreach(var d in new[]{settings.High,settings.Low,settings.Wow})
        for(int i=0;i<100000;i++){var x=d.Sample(rng);Check(x>=d.Min && x<=d.Max);Check(x!=d.Min && x!=d.Max,"Artificial boundary spike");}
});
Test("High truncated-normal moments and tails", () => {
    var rng=new RandomStream(RandomMode.Seeded,7);var a=Enumerable.Range(0,100000).Select(_=>settings.High.Sample(rng)).ToArray();
    Near(a.Average(),25.276,.12);var typical=a.Count(x=>x>=20&&x<=30)/(double)a.Length;Check(typical>.67&&typical<.73);
    Check(a.Any(x=>x<20)&&a.Any(x=>x>35));
});
Test("Low distribution typical band and tails",()=>{
    var rng=new RandomStream(RandomMode.Seeded,23);var a=Enumerable.Range(0,100000).Select(_=>settings.Low.Sample(rng)).ToArray();
    Check(a.Average()>7.45&&a.Average()<7.65);Check(a.Count(x=>x>=5&&x<=10)>75000);Check(a.Any(x=>x<5)&&a.Any(x=>x>10));
});
Test("Invalid distributions rejected",()=>{Throws(()=>new Distribution(2,0,1,3).Sample(new(RandomMode.Seeded,1)));Throws(()=>new Distribution(double.NaN,1,1,3).Validate());});
Test("Settings enforce agreed phase bounds",()=>Throws(()=>(settings with{High=new(25,5,14,50)}).Validate()));
Test("Settings reject infinite motion and invalid session limits",()=>{Throws(()=>(settings with{DiameterMm=double.NaN}).Validate());Throws(()=>(settings with{MaximumSessionMinutes=0}).Validate());});
Test("Initial high and explicit stop prohibit movement",()=>{var e=new LabEngine(settings);Check(!e.CanMove);e.Start(0);Check(e.CanMove);Check(e.SampledSeconds>=900&&e.SampledSeconds<=3000);e.Stop(1);Check(!e.CanMove);});
Test("Phase transition is exact at deadline",()=>{var e=new LabEngine(settings);e.Start(0);var end=e.PhaseEnd;e.Tick(end-.001);Check(e.State==NeonState.High);e.Tick(end);Check(e.State==NeonState.Low&&!e.CanMove);});
Test("Wow immediately overrides high",()=>{var e=new LabEngine(settings);e.Start(0);e.Input(.1,false);Check(e.State==NeonState.Wow&&!e.CanMove);Check(e.RecoveryEnd>=1.1&&e.RecoveryEnd<=25.1);});
Test("New physical input extends the recovery from latest event",()=>{var e=new LabEngine(settings);e.Start(0);e.Input(1,false);e.Input(2,false);Check(e.RecoveryEnd>=3);e.Tick(e.RecoveryEnd);Check(e.State==NeonState.High);});
Test("Held buttons block recovery until released",()=>{var e=new LabEngine(settings);e.Start(0);e.Input(1,true);e.Tick(30);Check(e.State==NeonState.Wow&&!e.CanMove);e.Input(31,false);e.Tick(e.RecoveryEnd);Check(e.CanMove);});
Test("Wow recovery during scheduled low cannot move",()=>{var e=new LabEngine(settings);e.Start(0);var end=e.PhaseEnd;e.Input(end-.01,false);e.Tick(end);Check(e.Phase==NeonState.Low);e.Tick(e.RecoveryEnd);Check(e.State==NeonState.Low&&!e.CanMove);});
Test("Physical activity in low is recognised",()=>{var e=new LabEngine(settings);e.Start(0);e.Tick(e.PhaseEnd);e.Input(e.PhaseEnd-1,false);Check(e.State==NeonState.Wow&&!e.CanMove);});
Test("Restart samples fresh high",()=>{var e=new LabEngine(settings);e.Start(0);var first=e.SampledSeconds;e.Stop(1);e.Start(2);Check(e.Phase==NeonState.High&&e.SampledSeconds!=first);});
Test("Idempotent start does not reset interval",()=>{var e=new LabEngine(settings);e.Start(0);var end=e.PhaseEnd;e.Start(1);Near(end,e.PhaseEnd);});
Test("Maximum session terminates motion",()=>{var e=new LabEngine(settings with{MaximumSessionMinutes=1});e.Start(0);e.Tick(60);Check(!e.Running&&!e.CanMove);});
Test("Clock regression is rejected",()=>{var e=new LabEngine(settings);e.Start(10);Throws(()=>e.Tick(9));});
Test("Test Mode compresses only high and low by 60",()=>{var a=new LabEngine(settings);var b=new LabEngine(settings,true);a.Start(0);b.Start(0);Near(a.SampledSeconds/60,b.SampledSeconds,.001);a.Input(1,false);b.Input(1,false);Near(a.RecoveryEnd,b.RecoveryEnd);});
Test("New normal engine cannot inherit Test Mode",()=>{var e=new LabEngine(settings,true);e.Start(0);var n=new LabEngine(settings);n.Start(0);Check(n.SampledSeconds>=900);});
Test("Next deadline is recovery when sooner than phase",()=>{var e=new LabEngine(settings);e.Start(0);e.Input(1,false);Near(e.NextDeadline(1),e.RecoveryEnd);});
Test("DPI conversions at four scaling levels",()=>{foreach(var dpi in new[]{96.0,120,144,192})Near(Orbit.DiameterPixels(25.4,dpi),dpi);});
Test("Physical calibration overrides scaling",()=>Near(Orbit.DiameterPixels(20,144,4.2),84));
Test("Orbit returns to starting point every seven seconds",()=>{var o=Orbit.At(new(500,500),new(0,0,1920,1080),80);var a=o.Position(0,7);var b=o.Position(7,7);Near(a.X,b.X);Near(a.Y,b.Y);});
Test("All edge and mixed-monitor orbits remain bounded",()=>{
    foreach(var area in new[]{new RectD(0,0,1920,1040),new RectD(-1920,-300,0,780)})
    foreach(var anchor in new[]{new PointD(area.Left,area.Top),new PointD(area.Right-1,area.Bottom-1),new PointD((area.Left+area.Right)/2,(area.Top+area.Bottom)/2)}){
        var o=Orbit.At(anchor,area,160);for(int i=0;i<1000;i++){var p=o.Position(i*.01,7);Check(p.X>=area.Left&&p.X<area.Right&&p.Y>=area.Top&&p.Y<area.Bottom);}
    }
});
Test("Tiny monitor safely reduces radius",()=>{var o=Orbit.At(new(4,4),new(0,0,10,10),100);Check(o.Radius<=4);});
Test("Timeline avoids double counting wow overlays",()=>{
    var events=new[]{new LabEvent(0,"start","High"),new LabEvent(2,"wow_start","Wow"),new LabEvent(3,"phase","Low"),new LabEvent(5,"wow_end","Wow"),new LabEvent(8,"stop","Stopped")};
    var t=SessionLog.Timeline(events,10);Near(t.Sum(x=>x.Seconds),10);Near(t.Where(x=>x.State=="Wow").Sum(x=>x.Seconds),3);Near(t.Where(x=>x.State=="Low").Sum(x=>x.Seconds),3);
});
var temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"neon-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
Test("Corrupt settings fall back with warning",()=>{var p=Path.Combine(temp,"bad.json");File.WriteAllText(p,"{broken");var s=LabSettings.Load(p,out var warning);Check(warning!=null&&s.Awake);});
Test("Settings round-trip excludes test timing",()=>{var p=Path.Combine(temp,"settings.json");settings.Save(p);var s=LabSettings.Load(p,out var warning);Check(warning==null&&s==settings);Check(!File.ReadAllText(p).Contains("TestMode"));});
Test("Disabled log creates no events or file",()=>{var p=Path.Combine(temp,"disabled.jsonl");using var l=new SessionLog(p);l.Add(new(0,"start","High"));Check(!File.Exists(p)&&l.Events.Count==0);});
Test("Log off clears retained events and stops writes",()=>{var p=Path.Combine(temp,"log.jsonl");using var l=new SessionLog(p);l.Enable(true);l.Add(new(0,"start","High"));l.Enable(false);var size=new FileInfo(p).Length;l.Add(new(1,"stop","Stopped"));Check(l.Events.Count==0&&new FileInfo(p).Length==size);});
Test("Report encodes HTML and writes companion CSV",()=>{using var l=new SessionLog(Path.Combine(temp,"report.jsonl"));l.Enable(true);l.Add(new(0,"start","High"));l.Add(new(0,"phase","High",25));l.Export(Path.Combine(temp,"report.html"),30,17,"<script>");var html=File.ReadAllText(Path.Combine(temp,"report.html"));Check(html.Contains("&lt;script&gt;")&&!html.Contains("<script>"));Check(File.Exists(Path.Combine(temp,"report.csv")));});
Test("Delete removes only current session file",()=>{var p=Path.Combine(temp,"delete.jsonl");using var l=new SessionLog(p);l.Enable(true);l.Add(new(0,"start","High"));l.Delete();Check(!File.Exists(p)&&!l.Enabled);Check(File.Exists(Path.Combine(temp,"settings.json")));});
Directory.CreateDirectory("artifacts");File.WriteAllLines("artifacts/core-tests.txt",results.Append($"{passed} passed, {failed} failed."));
Console.WriteLine($"{passed} passed, {failed} failed. Test scratch: {temp}");
return failed == 0 ? 0 : 1;
