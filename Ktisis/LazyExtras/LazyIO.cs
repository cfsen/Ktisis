using Dalamud.Interface.ImGuiFileDialog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Lumina.Excel.Sheets;
using Dalamud.Plugin;
using System.Text.Json.Serialization;
using System.Text.Json;
using ImGuiNET;
using Ktisis.Common.Extensions;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;


namespace Ktisis.LazyExtras;
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
		_cfg = GetConfig();
		SetupQuickAccess();
	}

	// ImGui

	public void DrawDialog() => _fdm.Draw();
	public string LoadFileData() => _readFileData;
	public string LastLoadDirectory(bool friendly = false) => friendly ? Path.GetFileName(_lastLoadDirectory) : _lastLoadDirectory;
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
		_fdm.SaveFileDialog("Save lights", ".klights", "Lights", ".klights", 
			CreateCallback(callback, LazyIOFlag.Poses | LazyIOFlag.Save), _cfg.LastLoadPoseDir);
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
			dbg($"Load data from: {items}");
			_readFileName = Path.GetFileName(items);
			dbg($"Update name of last read file: {_readFileName}");
			_readFileData = ReadFile(items) ?? "";
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

		if (load && pose) {
			_cfg.LastLoadPoseDir = loc;
		}
		if (save && pose)
		{
			_cfg.LaseSavePoseDir = loc;
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
	private LazyIOSettings GetConfig() {
		if(ReadFile(GetConfigPath()) is not string d) {
			dbg("Unable to load settings, regenerating.");
			return new LazyIOSettings();
		}
		if(DeserializeCfg(d) is not LazyIOSettings s) {
			dbg("Unable to parse settings, regenerating.");
			return new LazyIOSettings();
		}
		return s;
	}
	private string GetConfigPath() {
		return Path.Join(_dpi.GetPluginConfigDirectory(), "LazySettings.json");
	}

	private void SaveConfig() {
		var d = JsonSerializer.Serialize<LazyIOSettings>(_cfg);
		SaveFile(GetConfigPath(), d);
	}

	// Directory scan

	private void ScanDir(string path, string filter) {
		Directory.GetFiles(path);
	}

	// File

	private string? ReadFile(string path) {
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
		Ktisis.Log.Debug("LazyIO dispose");
		SaveConfig();
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
}

[Serializable]
public class LazyIOSettings {
	public string LastLoadPoseDir { get; set; } = "";
	public string LaseSavePoseDir { get; set; } = "";
	public string LastLoadLightDir { get; set; } = "";
	public string LastSaveLightDir { get; set; } = "";
	public List<string> PoseDirectories { get; set; } = [];
	public List<string> LightsDirectories { get; set; } = [];
	public bool PurgeDefaultQuickAxDirs { get; set; } = false;
}
