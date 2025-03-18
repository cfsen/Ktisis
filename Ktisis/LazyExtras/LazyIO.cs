using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.LazyExtras.Datastructures;

using Lumina.Excel.Sheets;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace Ktisis.LazyExtras;
[Singleton]
public class LazyIO :IDisposable {
	private FileDialogManager fdm;
	private IDalamudPluginInterface dpi;

	private LazyIOSettings cfg;
	
	public bool dialogOpen = false;

	private string readFileData = "";
	private string readFileName = "";
	private string saveBuffer = "";
	private string lastLoadDirectory = "";

	private Dictionary<string, Dictionary<string, string>> dialogUI;

	public LazyIO(IDalamudPluginInterface _dpi) {
		fdm = new();
		dpi = _dpi;
		cfg = GetIOConfig();
		SetupQuickAccess();
		dialogUI = DialogOpFlags();
	}

	// ImGui

	public void DrawDialog() => fdm.Draw();
	public string LastLoadDirectory(bool friendly = false) => friendly ? Path.GetFileName(lastLoadDirectory) : lastLoadDirectory;
	public string GetFriendlyName(string path) => Path.GetFileName(path) ?? "Error: invalid path.";
	public string LoadedFileName() => readFileName;
	public string SetSaveBuffer(string s) => saveBuffer = s;

	// Dialog handlers

	public void OpenDialogLoad(LazyIOFlag flag, Action<bool, List<string>> callback) {
		dialogOpen = true;

		LazyIOFlag opType = flag.HasFlag(LazyIOFlag.Save) ? LazyIOFlag.Save : LazyIOFlag.Load;
		string flagKey = (flag ^ opType).ToString();

		cfg.LastDirectory.TryGetValue(flag.ToString(), out string? path);

		fdm.OpenFileDialog(
			opType.ToString() + dialogUI[flagKey]["Title"], 
			dialogUI[flagKey]["Extension"], 
			CreateCallback(callback, flag), 
			1, 
			path ??= ""
		);
	}
	public void OpenDialogSave(LazyIOFlag flag, Action<bool, string> callback) {
		dialogOpen = true;

		LazyIOFlag opType = flag.HasFlag(LazyIOFlag.Save) ? LazyIOFlag.Save : LazyIOFlag.Load;
		string flagKey = (flag ^ opType).ToString();

		cfg.LastDirectory.TryGetValue(flag.ToString(), out string? path);

		fdm.SaveFileDialog(
			opType.ToString() + dialogUI[flagKey]["Title"], 
			dialogUI[flagKey]["Extension"], 
			dialogUI[flagKey]["DefaultName"], 
			dialogUI[flagKey]["DefaultExtension"], 
			CreateCallback(callback, flag), 
			path ??= ""
		);
	}

	// Callbacks

	public Action<bool, string> CreateCallback(Action<bool, string> callback, LazyIOFlag dtype) {
		return (valid, items) => {
			dialogOpen = false;

			UpdateLastDir(dtype);
			if(valid)
				HandleCallback(items, dtype);

			callback(valid, items);
		};
	} 
	private Action<bool, List<string>> CreateCallback(Action<bool, List<string>> callback, LazyIOFlag dtype, bool save = false) {
		return (valid, items) => { 
			dialogOpen = false;

			UpdateLastDir(dtype);
			if(valid)
				HandleCallback(items[0], dtype);

			callback(valid, items);
		};
	}

	private void HandleCallback(string items, LazyIOFlag dtype) { 
		if(dtype.HasFlag(LazyIOFlag.Load)) {
			var loc = (fdm.GetType()
				.GetField("dialog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
				.GetValue(fdm) as FileDialog)?.GetCurrentPath() ?? ".";

			lastLoadDirectory = loc;
			readFileName = Path.GetFileName(items);
			readFileData = ReadFileContents(items) ?? "";

			dbg($"Loading from dir: {loc}");
			dbg($"Load data from: {items}");
			dbg($"Update name of last read file: {readFileName}");
		}
		else if(dtype.HasFlag(LazyIOFlag.Save)) {
			dbg($"Save to file: {items}");
			SaveFile(items, saveBuffer);
		}
	}

	private void UpdateLastDir(LazyIOFlag dtype) {
		var loc = (fdm.GetType()
			.GetField("dialog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
			.GetValue(fdm) as FileDialog)?.GetCurrentPath() ?? ".";
		dbg($"UpdateLastDir called. dtype={dtype}");

		cfg.LastDirectory[dtype.ToString()] = loc;
		dbg($"Setting: cfg.LastDirectory[{dtype.ToString()}] = {loc}");

	}

	// Quick access 

	private void SetupQuickAccess(bool pose = true) {
		if(cfg.PoseDirectories.Count < 0) return;
		PurgeQuickAccessDefaults();
		int i = fdm.CustomSideBarItems.Count;
		string logBuffer = "Adding dirs: ";
		foreach(var d in pose ? cfg.PoseDirectories : cfg.LightsDirectories) {
			logBuffer += Path.GetFileName(d) + ", ";
			fdm.CustomSideBarItems.Add((Path.GetFileName(d) ?? "Oh no", d, FontAwesomeIcon.Star, i));
			i++;	
		}
		dbg(logBuffer);
	}
	private void PurgeQuickAccessDefaults() {
		fdm.CustomSideBarItems.Clear(); 
	}
	private void AddPoseDir(string path)	=>	cfg.PoseDirectories.Add(path);
	private void AddLightsDir(string path)	=>	cfg.LightsDirectories.Add(path);
	//private string GetDirName(string d)		=>	d.Substring(d.LastIndexOf('\\')+1, d.Length-d.LastIndexOf('\\')-1);

	// Directory scan

	public string[] ScanDir(string path, string filter) {
		return Directory.GetFiles(path, "*" + filter);
	}

	// File

	public (string data, string filename, string dir, string dirname)? ReadFile(string path) {
		if(ReadFileContents(path) is not string data) return null;
		return (data, LoadedFileName(), LastLoadDirectory(), LastLoadDirectory(true));
	}
	public List<string>? ReadFile2(string path) {
		if(ReadFileContents(path) is not string data) return null;
		return [data, LoadedFileName(), LastLoadDirectory(), LastLoadDirectory(true)];
	}
	private string? ReadFileContents(string path) {
		try {
			var d = File.ReadAllText(path);
			return d;
		}
		catch (Exception e) {
			dbg($"Failed to read: {path}");
			dbg(e.ToString());
			return null;
		}
	}

	private void SaveFile(string path, string content) {
		try {
			File.WriteAllText(path, content);
		} 
		catch {
			dbg($"Failed to write file: {path}");
		}
	}

	// UI helpers

	private Dictionary<string, Dictionary<string, string>> DialogOpFlags() {
		// Construct this once at initialization
		Dictionary<string, Dictionary<string, string>> FlagLookup = new Dictionary<string, Dictionary<string, string>>(); 
		// Weave save/load on demand
		//if(flag.HasFlag(LazyIOFlag.Save))
		//	op.Add("OpType", "Save");
		//else
		//	op.Add("OpType", "Load");

		Dictionary<string, string> op = new Dictionary<string, string>();
		op.Add("Title", "Lights preset");
		op.Add("Extension", ".klights");
		op.Add("DefaultName", "Lights");
		op.Add("DefaultExtension", ".klights");
		FlagLookup["Light"] = op;

		op = new Dictionary<string, string>();
		op.Add("Title", "Pose file");
		op.Add("Extension", ".pose");
		op.Add("DefaultName", "Pose");
		op.Add("DefaultExtension", ".pose");
		FlagLookup["Pose"] = op;
		
		op = new Dictionary<string, string>();
		op.Add("Title", "Actor offsets");
		op.Add("Extension", ".koffsets");
		op.Add("DefaultName", "Offsets");
		op.Add("DefaultExtension", ".koffsets");
		FlagLookup["Offset"] = op;

		op = new Dictionary<string, string>();
		op.Add("Title", "Scene");
		op.Add("Extension", ".kscene");
		op.Add("DefaultName", "Scene");
		op.Add("DefaultExtension", ".kscene");
		FlagLookup["Scene"] = op;
		//dbg("Dict init");
		//dbg(FlagLookup.Count.ToString());
		//foreach(var k in FlagLookup)
		//{
		//	dbg($"{k.Key}:{k.Value}");
		//	foreach(var v in FlagLookup[k.Key])
		//	{
		//		dbg($"\t{v.Key}:{v.Value}");
		//	}
		//}
		return FlagLookup;
	}

	// Config

	public string GetConfigPath(string cfgName) {
		return Path.Join(dpi.GetPluginConfigDirectory(), cfgName);
	}
	public void SaveConfig(string configName, string configData) {
		SaveFile(GetConfigPath(configName), configData);
	}
	private LazyIOSettings GetIOConfig() {
		if (ReadFileContents(GetConfigPath("LazySettings.json")) is not string d)
		{
			dbg("Unable to load settings, regenerating.");
			return new LazyIOSettings();
		}
		if (DeserializeCfg(d) is not LazyIOSettings s)
		{
			dbg("Unable to parse settings, regenerating.");
			return new LazyIOSettings();
		}
		return s;
	}

	// Deserialize, serialize

	private LazyIOSettings? DeserializeCfg(string json) {
		try {
			var settings = new JsonSerializerSettings {
				NullValueHandling = NullValueHandling.Ignore,
				MissingMemberHandling = MissingMemberHandling.Ignore
			};
			var d = JsonConvert.DeserializeObject<LazyIOSettings>(json, settings);
			if(d == null) return null;
			return d;
		}
		catch {
			dbg("Failed to deserialize settings.");
			return null;
		}
	}
	private void SaveIOConfig() {
		var d = JsonConvert.SerializeObject(cfg, Formatting.Indented);
		SaveFile(GetConfigPath("LazySettings.json"), d);
	}

	public void Dispose() {
		//Stopwatch t = Stopwatch.StartNew();
		SaveIOConfig();
		//t.Stop();
		//Ktisis.Log.Debug($"LazyIO disposed in {t.ElapsedMilliseconds}");
		dbg("LazyIO disposing.");
	}

	private void dbg(string s) => Ktisis.Log.Debug($"LazyIO: {s}");
}

[Flags]
public enum LazyIOFlag {
	Load = 1,
	Save = 2,
	Light = 4,
	Pose = 8,
	//Expression = 16,
	Offset = 32,
	Scene = 64,

	LoadLight = Load | Light,
	SaveLight = Save | Light,
	LoadPose = Load | Pose,
	SavePose = Save | Pose,
	//LoadExpr = Load | Expression,
	//SaveExpr = Save | Expression,
	LoadOffset = Load | Offset,
	SaveOffset = Save | Offset,
	LoadScene = Load | Scene,
	SaveScene = Save | Scene,
	AllTypes = Light | Pose | Offset | Scene
}

// TODO use dict<enum,path> to store shit
[Serializable]
public class LazyIOSettings {
	public Dictionary<string, string> LastDirectory = [];
	public List<string> PoseDirectories { get; set; } = [];
	public List<string> LightsDirectories { get; set; } = [];
	public bool PurgeDefaultQuickAxDirs { get; set; } = false;
}
