using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;

using Newtonsoft.Json.Bson;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.UI.Controls;
class FileNavigator {
	public string? DirectoryNameFriendly = null;
	public string? FileNameFriendly = null;
	public int FileCount = 0;
	public bool StateChanged = false;

	// maybe just do an enable bool instead of dealing with null type checking

	private string? DirectoryPath = null; 
	public string? FilePath = null;
	private List<(string, string)> Files = []; // full path, friendly path
	private int Cursor = 0;
	private string Filter;

	private LazyIO io;
	private LazyUi lui;
	private LazyUiSizes uis;

	public FileNavigator(LazyIO _io, LazyUi _ui, LazyUiSizes _uis, string _filter) {
		this.io = _io;
		this.Filter = _filter;
		this.lui = _ui;
		this.uis = _uis;
	}
	
	public void Draw() {
		ImGui.BeginGroup();
		using(ImRaii.Disabled(FileCount < 1)) {

		if(lui.BtnIcon(Dalamud.Interface.FontAwesomeIcon.ArrowLeft, $"CtrlFileNav{Filter}ArrowLeft", uis.BtnSmall, "Previous"))
			TraverseDirectory(-1);
		//ImGui.SameLine();
		//ImGui.Text(FileNameFriendly ?? "No pose loaded");
		ImGui.SameLine();
		//ImGui.SetCursorPosX(uis.SidebarW-2*uis.BtnSmall.X-2*uis.Space);
		if(lui.BtnIcon(Dalamud.Interface.FontAwesomeIcon.ArrowRight, $"CtrlFileNav{Filter}ArrowRight", uis.BtnSmall, "Next"))
			TraverseDirectory(1);
		}
		ImGui.EndGroup();
	}
	public void DrawCyclePosition() {
		if(Files.Count > 0) 
			ImGui.Text($"{Cursor+1}/{Files.Count.ToString()}: ");
	}
	public void UpdateState(string path, string filePath) {
		Reset();
		DirectoryPath = path;
		FilePath = filePath;
		FileNameFriendly = io.GetFriendlyName(filePath);
		PopulateFileList(path);
		SetCursorPosition();
	}

	// Logic

	private void TraverseDirectory(int motion) {
		int idx = 0;
		int check_idx = Cursor+motion;
		int lim_idx = FileCount-1;

		if(check_idx > lim_idx)
			idx = 0;
		else if(check_idx < 0)
			idx = lim_idx;
		else
			idx = check_idx;

		//dbg($"Index changed: {Cursor} -> {idx}");
		Cursor = idx;

		FilePath = Files[Cursor].Item1;
		FileNameFriendly = Files[Cursor].Item2;
		StateChanged = true;
	}

	private void PopulateFileList(string path) {
		//dbg("PopulateFileList");
		var files = io.ScanDir(path, Filter);
		//dbg($"ScanDir: {path}\r\t\t\t\t\t\tCount={files.Length}");
		if(files.Length == 0) return;
		foreach(string s in files) {
			//dbg($"\tAdd: {s}");
			Files.Add((s, io.GetFriendlyName(s)));
		}
		FileCount = files.Length;
	}
	private void SetCursorPosition() {
		for(int i = 0; i < FileCount; i++) {
			if(Files[i].Item2 == FilePath) {
				Cursor = i;
				break;
			}
		}
		//dbg($"Cursor set to idx: {Cursor}");
	}
	private void Reset() {
		Files.Clear();
		DirectoryNameFriendly = null;
		FileNameFriendly = null;
		FileCount = 0;
		FileCount = 0;
		Cursor = 0;
	}

	private void dbg(string s) => Ktisis.Log.Debug($"FileNavigator: {s}");
}
