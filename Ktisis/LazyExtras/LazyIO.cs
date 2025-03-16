using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;

using Lumina.Excel.Sheets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace Ktisis.LazyExtras;
[Singleton]
public class LazyIO :IDisposable {
	private FileDialogManager _fdm;
	private IDalamudPluginInterface _dpi;

	private LazyIOSettings _cfg;
	
	private bool _dialogOpen = false;

	private string _readFileData = "";
	private string _readFileName = "";
	private string _saveBuffer = "";
	private string _lastLoadDirectory = "";

	public LazyIO(IDalamudPluginInterface dpi) {
		_fdm = new();
		_dpi = dpi;
		_cfg = GetIOConfig();
		SetupQuickAccess();
	}

	// ImGui

	public void DrawDialog() => _fdm.Draw();
	public string LoadFileData() => _readFileData;
	public string LastLoadDirectory(bool friendly = false) => friendly ? Path.GetFileName(_lastLoadDirectory) : _lastLoadDirectory;
	public string GetFriendlyName(string path) => Path.GetFileName(path) ?? "Error: invalid path.";
	public string LoadedFileName() => _readFileName;
	public string SetSaveBuffer(string s) => _saveBuffer = s;

	// Imgui:load

	public void OpenLightDialog(Action <bool, List<string>> callback) {
		_dialogOpen = true;
		_fdm.OpenFileDialog("Load pose file", ".klights", 
			CreateCallback(callback, LazyIOFlag.Lights | LazyIOFlag.Load), 1, _cfg.LastLoadLightDir);
	}
	public void OpenPoseDialog(Action<bool, List<string>> callback) {
		_dialogOpen = true;
		dbg(_cfg.LastLoadPoseDir);
		_fdm.OpenFileDialog("Load pose file", ".pose", 
			CreateCallback(callback, LazyIOFlag.Poses | LazyIOFlag.Load), 1, _cfg.LastLoadPoseDir);
	}
	public void OpenOffsetDialog(Action<bool, List<string>> callback) {
		_dialogOpen = true;
		dbg(_cfg.LastLoadPoseDir);
		_fdm.OpenFileDialog("Load pose file", ".koffsets", 
			CreateCallback(callback, LazyIOFlag.Offset | LazyIOFlag.Load), 1, _cfg.LastLoadOffsetDir);
	}
	public void OpenPoseDirDialog(Action<bool, string> callback) {
		_dialogOpen = true;
		_fdm.OpenFolderDialog("Open pose directory", 
			CreateCallback(callback, LazyIOFlag.Poses | LazyIOFlag.Load), _cfg.LastLoadPoseDir);
	}

	// Imgui:save

	public void OpenLightSaveDialog(Action<bool, string> callback) {
		_dialogOpen = true;
		_fdm.SaveFileDialog("Save lights", ".klights", "Lights", ".klights", 
			CreateCallback(callback, LazyIOFlag.Lights | LazyIOFlag.Save), _cfg.LastLoadLightDir);
	}
	public void OpenPoseSaveDialog(Action<bool, string> callback) {
		_dialogOpen = true;
		_fdm.SaveFileDialog("Save pose", ".pose", "Pose", ".pose", 
			CreateCallback(callback, LazyIOFlag.Poses | LazyIOFlag.Save), _cfg.LastLoadPoseDir);
	}
	public void OpenOffsetSaveDialog(Action<bool, string> callback) {
		_dialogOpen = true;
		_fdm.SaveFileDialog("Save offsets", ".koffsets", "Offsets", ".koffsets", 
			CreateCallback(callback, LazyIOFlag.Poses | LazyIOFlag.Save), _cfg.LastSaveOffsetDir);
	}

	// Callbacks

	public Action<bool, string> CreateCallback(Action<bool, string> callback, LazyIOFlag dtype) {
		return (valid, items) => {
			_dialogOpen = false;

			UpdateLastDir(dtype);
			if(valid)
				HandleCallback(items, dtype);

			callback(valid, items);
		};
	} 
	private Action<bool, List<string>> CreateCallback(Action<bool, List<string>> callback, LazyIOFlag dtype, bool save = false) {
		return (valid, items) => { 
			_dialogOpen = false;

			UpdateLastDir(dtype);
			if(valid)
				HandleCallback(items[0], dtype);

			callback(valid, items);
		};
	}

	private void HandleCallback(string items, LazyIOFlag dtype) { 
		if(dtype.HasFlag(LazyIOFlag.Load)) {
			var loc = (_fdm.GetType()
				.GetField("dialog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
				.GetValue(_fdm) as FileDialog)?.GetCurrentPath() ?? ".";

			_lastLoadDirectory = loc;
			_readFileName = Path.GetFileName(items);
			_readFileData = ReadFileContents(items) ?? "";

			dbg($"Loading from dir: {loc}");
			dbg($"Load data from: {items}");
			dbg($"Update name of last read file: {_readFileName}");
		}
		else if(dtype.HasFlag(LazyIOFlag.Save)) {
			dbg($"Save to file: {items}");
			SaveFile(items, _saveBuffer);
		}
	}

	private void UpdateLastDir(LazyIOFlag dtype) {
		var loc = (_fdm.GetType()
			.GetField("dialog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
			.GetValue(_fdm) as FileDialog)?.GetCurrentPath() ?? ".";
		dbg($"UpdateLastDir called. dtype={dtype}");
		// Update last opened directory
		bool load = dtype.HasFlag(LazyIOFlag.Load);
		bool save = dtype.HasFlag(LazyIOFlag.Save);
		bool pose = dtype.HasFlag(LazyIOFlag.Poses);

		if (dtype.HasFlag(LazyIOFlag.Load)) {
			if(dtype.HasFlag(LazyIOFlag.Poses))
				_cfg.LastLoadPoseDir = loc;
			else if(dtype.HasFlag(LazyIOFlag.Lights))
				_cfg.LastLoadLightDir = loc;
			else if(dtype.HasFlag(LazyIOFlag.Offset))
				_cfg.LastLoadOffsetDir = loc;
		}
		else if(dtype.HasFlag(LazyIOFlag.Save)) {
			if(dtype.HasFlag(LazyIOFlag.Poses))
				_cfg.LaseSavePoseDir = loc;
			else if(dtype.HasFlag(LazyIOFlag.Lights))
				_cfg.LastSaveLightDir = loc;
			else if(dtype.HasFlag(LazyIOFlag.Offset))
				_cfg.LastSaveOffsetDir = loc;
		}
	}

	// Quick access 

	private void SetupQuickAccess(bool pose = true) {
		if(_cfg.PoseDirectories.Count < 0) return;
		PurgeQuickAccessDefaults();
		int i = _fdm.CustomSideBarItems.Count;
		string logBuffer = "Adding dirs: ";
		foreach(var d in pose ? _cfg.PoseDirectories : _cfg.LightsDirectories) {
			logBuffer += Path.GetFileName(d) + ", ";
			_fdm.CustomSideBarItems.Add((Path.GetFileName(d) ?? "Oh no", d, FontAwesomeIcon.Star, i));
			i++;	
		}
		dbg(logBuffer);
	}
	private void PurgeQuickAccessDefaults() {
		_fdm.CustomSideBarItems.Clear(); 
	}
	private void AddPoseDir(string path)	=>	_cfg.PoseDirectories.Add(path);
	private void AddLightsDir(string path)	=>	_cfg.LightsDirectories.Add(path);
	//private string GetDirName(string d)		=>	d.Substring(d.LastIndexOf('\\')+1, d.Length-d.LastIndexOf('\\')-1);

	// Config
	public string GetConfigPath(string cfgName) {
		return Path.Join(_dpi.GetPluginConfigDirectory(), cfgName);
	}
	public void SaveConfig(string configName, string configData) {
		SaveFile(GetConfigPath(configName), configData);
	}
	private LazyIOSettings GetIOConfig() {
		if(ReadFileContents(GetConfigPath("LazySettings.json")) is not string d) {
			dbg("Unable to load settings, regenerating.");
			return new LazyIOSettings();
		}
		if(DeserializeCfg(d) is not LazyIOSettings s) {
			dbg("Unable to parse settings, regenerating.");
			return new LazyIOSettings();
		}
		return s;
	}
	private void SaveIOConfig() {
		var d = JsonSerializer.Serialize<LazyIOSettings>(_cfg);
		SaveFile(GetConfigPath("LazySettings.json"), d);
	}

	// Directory scan

	public string[] ScanDir(string path, string filter) {
		return Directory.GetFiles(path, "*" + filter);
	}

	// File

	public (string data, string filename, string dir, string dirname)? ReadFile(string path) {
		if(ReadFileContents(path) is not string data) return null;
		return (data, LoadedFileName(), LastLoadDirectory(), LastLoadDirectory(true));
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

	// Deserialize, serialize

	private LazyIOSettings? DeserializeCfg(string json) {
		try {
			var d = JsonSerializer.Deserialize<LazyIOSettings>(json);
			if(d == null) return null;
			return d;
		}
		catch {
			dbg("Failed to deserialize settings.");
			return null;
		}
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
	Lights = 4,
	Poses = 8,
	Expression = 16,
	Offset = 32,
	Scene = 64
}

[Serializable]
public class LazyIOSettings {
	public string LastLoadPoseDir { get; set; } = "";
	public string LaseSavePoseDir { get; set; } = "";
	public string LastSaveOffsetDir { get; set; } = "";
	public string LastLoadOffsetDir { get; set; } = "";
	public string LastSaveSceneDir { get; set; } = "";
	public string LastLoadSceneDir { get; set; } = "";
	public string LastLoadLightDir { get; set; } = "";
	public string LastSaveLightDir { get; set; } = "";
	public List<string> PoseDirectories { get; set; } = [];
	public List<string> LightsDirectories { get; set; } = [];
	public bool PurgeDefaultQuickAxDirs { get; set; } = false;
}
