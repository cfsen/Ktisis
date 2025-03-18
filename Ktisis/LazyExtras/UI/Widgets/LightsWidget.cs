using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

namespace Ktisis.LazyExtras.UI.Widgets;
class LightsWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	
	bool dialogOpen = false;

	public LightsWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Light;
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

		lui.DrawHeader(FontAwesomeIcon.Lightbulb, "Lights");
		DrawImportExportControls();
		DrawPresetSpawnControls();

		if(ImGui.Button("Remove all lights"))
			ctx.LazyExtras.lights.LightsDeleteAll();

		lui.DrawFooter();

		ImGui.EndGroup();
	}

	private void DrawPresetSpawnControls() {
		ImGui.Text("Spawn preset");
		if(ImGui.Button("3 point"))
			ctx.LazyExtras.lights.SpawnStudioLights();
		ImGui.SameLine();
		if(ImGui.Button("Apartment exterior"))
			ctx.LazyExtras.lights.SpawnStudioApartmentAmbientLights();
	}

	// Dialog handling

	private (LazyIOFlag, string) IODataDispatcher() {
		ctx.LazyExtras.lights.LightsSave();
		return (LazyIOFlag.Save | LazyIOFlag.Light, ctx.LazyExtras.lights._json);
	}
	private void IODataReceiver(bool success, List<string>? data) {
		if (!success) return;
		if (data == null) return;

		// 0: Data, 1: file name, 2: path to directory, 3: directory name
		ctx.LazyExtras.lights._json = data[0];
		ctx.LazyExtras.lights.ImportLightJson(data[0]);
	}
	private void DrawImportExportControls() {
		lui.BtnSave(IODataDispatcher, "WLIGHT_Dispathcer", "Save", ctx.LazyExtras.io);
		ImGui.SameLine();
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Light, IODataReceiver, "WLIGHT_Receiver", "Load", ctx.LazyExtras.io);
	}
	private void dbg(string s) => Ktisis.Log.Debug($"LightsWidget: {s}");
}
