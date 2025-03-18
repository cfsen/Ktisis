using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Collections.Generic;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class PoseLerpWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	private bool lerpEnabled = false;

	public PoseLerpWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Pose;
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
		lui.DrawHeader(FontAwesomeIcon.PersonWalkingArrowRight, "Expression LERP");
		
		using(ImRaii.Disabled(ctx.LazyExtras.SelectedActor == null || !lerpEnabled)) {
			DrawImportExportControls();
			ImGui.SameLine();
			if(lui.BtnIcon(FontAwesomeIcon.ArrowsSpin, "WPOSELERP_Refresh", uis.BtnSmall, "Refresh actor"))
				ctx.LazyExtras.pose.lerp.RefreshActor(ctx.LazyExtras.SelectedActor);
		}
		ImGui.SameLine();
		if(ImGui.Checkbox("Enable", ref lerpEnabled))
			ctx.LazyExtras.pose.lerp.ToggleLerp();

		using(ImRaii.Disabled(ctx.LazyExtras.SelectedActor == null || !lerpEnabled)) {
			if(ImGui.SliderFloat("LERP", ref ctx.LazyExtras.pose.lerp.lerpFactor, 0.0f, 1.0f))
				ctx.LazyExtras.pose.lerp.Slide();
		}

		lui.DrawFooter();
		ImGui.EndGroup();
	}

	// Saving & loading

	//private (LazyIOFlag, string) IODataDispatcher() {
	//	return (LazyIOFlag.Save | LazyIOFlag.Dev, "pose data");
	//}
	private void IODataReceiver(bool success, List<string>? data) {
		if (!success) return;
		if (data == null) return;
		// 0: Data, 1: file name, 2: path to directory, 3: directory name
		ctx.LazyExtras.pose.lerp.SetupLerp(ctx.LazyExtras.SelectedActor, data[0]);
	}
	private void DrawImportExportControls() {
		//lui.BtnSave(IODataDispatcher, "WPOSELERP_Dispathcer", "Save", ctx.LazyExtras.io);
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Dev, IODataReceiver, "WPOSELERP_Receiver", "Load", ctx.LazyExtras.io);
	}

	// Debug

	private void dbg(string s) => Ktisis.Log.Debug($"DemoWidget: {s}");
}
