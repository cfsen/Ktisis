using Dalamud.Interface;
using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Collections.Generic;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class DemoWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	private bool dialogOpen;

	public DemoWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.None;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.Ad, "Demo");
		
		ImGui.Text("Demo widget");

		lui.DrawFooter();
		ImGui.EndGroup();
	}

	// Saving & loading

	private (LazyIOFlag, string) IODataDispatcher() {
		return (LazyIOFlag.Save | LazyIOFlag.Pose, "pose data");
	}
	private void IODataReceiver(bool success, List<string>? data) {
		if (success) {
			// 0: Data, 1: file name, 2: path to directory, 3: directory name
			foreach (var d in data!)
				dbg(d);
		}
	}
	private void DrawImportExportControls() {
		lui.BtnSave(IODataDispatcher, "WDEMO_Dispathcer", "Save", ctx.LazyExtras.io);
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Pose, IODataReceiver, "WDEMO_Receiver", "Load", ctx.LazyExtras.io);
	}

	// Debug

	private void dbg(string s) => Ktisis.Log.Debug($"DemoWidget: {s}");
}
