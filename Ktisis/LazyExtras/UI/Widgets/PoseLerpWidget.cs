using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

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
	private float sliderBufferMouth = 0.0f;
	private float sliderBufferTongue = 0.0f;
	private float sliderBufferCheeks = 0.0f;
	private float sliderBufferLids = 0.0f;
	private float sliderBufferEyes = 0.0f;
	private float sliderBufferBrow = 0.0f;
	private float sliderBufferHair = 0.0f;

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
		ImGui.SetCursorPosX(uis.SidebarW/3);
			ImGui.Text("Full LERP");
			ImGui.Text("Origin");
			ImGui.SameLine();
			if(ImGui.SliderFloat("Target##WPOSELERP_FullLerp", ref ctx.LazyExtras.pose.lerp.lerpFactor, 0.0f, 1.0f))
				ctx.LazyExtras.pose.lerp.Slide();
			
			LerpSlider("Hair", ref sliderBufferHair);
			LerpSlider("Brow", ref sliderBufferBrow);
			LerpSlider("Eyes", ref sliderBufferEyes);
			LerpSlider("Lids", ref sliderBufferLids);
			LerpSlider("Cheeks", ref sliderBufferCheeks);
			LerpSlider("Mouth", ref sliderBufferMouth);
			LerpSlider("Tongue", ref sliderBufferTongue);

		}

		lui.DrawFooter();
		ImGui.EndGroup();
	}

	private void LerpSlider(string label, ref float sliderBuffer) {
		ImGui.SetCursorPosX(uis.SidebarW/3);
		ImGui.Text(label);
		ImGui.Text("Origin");
		ImGui.SameLine();
		if(ImGui.SliderFloat($"Target##WPOSELERP_FullLerp{label}", ref sliderBuffer, 0.0f, 1.0f)) {
			ctx.LazyExtras.pose.lerp.SetFilterGroupValue(label, sliderBuffer);
			ctx.LazyExtras.pose.lerp.Slide();
		}
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
		sliderBufferMouth = 0.0f;
	}
	private void DrawImportExportControls() {
		//lui.BtnSave(IODataDispatcher, "WPOSELERP_Dispathcer", "Save", ctx.LazyExtras.io);
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Dev, IODataReceiver, "WPOSELERP_Receiver", "Load", ctx.LazyExtras.io);
	}

	// Debug

	private void dbg(string s) => Ktisis.Log.Debug($"DemoWidget: {s}");
}
