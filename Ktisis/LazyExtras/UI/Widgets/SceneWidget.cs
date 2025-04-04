using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Collections.Generic;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class SceneWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	//private bool dialogOpen;

	public SceneWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Scene;
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
		//if(dialogOpen)
		//	ctx.LazyExtras.io.DrawDialog();
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.Ad, "Scene");
		
		DrawImportExportControls();

		lui.DrawFooter();
		ImGui.EndGroup();
	}

	public void DrawAsInline() {
		ImGui.SameLine();
		using (ImRaii.Group()) {
			DrawImportExportControls();
		}
	}

	// Dialog handling

	private (LazyIOFlag, string) IODataDispatcher() {
		return (LazyIOFlag.Save | LazyIOFlag.Scene, ctx.LazyExtras.scene.ExportSceneFile());
	}
	private void IODataReceiver(bool success, List<string>? data) {
		if (!success) return;
		if (data == null) return;

		// 0: Data, 1: file name, 2: path to directory, 3: directory name
		ctx.LazyExtras.scene.Import(data[0]);

	}
	private void DrawImportExportControls() {
		using (ImRaii.Disabled(!ctx.Posing.IsEnabled)) {
		lui.BtnSave(IODataDispatcher, "WSCENEMGR_Dispathcer", "Save", ctx.LazyExtras.io);
		ImGui.SameLine();
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Scene, IODataReceiver, "WSCENEMGR_Receiver", "Load", ctx.LazyExtras.io);
		}
	}
	private void dbg(string s) => Ktisis.Log.Debug($"SceneWidget: {s}");
}
