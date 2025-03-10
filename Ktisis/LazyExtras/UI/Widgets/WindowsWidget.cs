using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Windows;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class WindowsWidget :ILazyWidget {
	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;

	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	public WindowsWidget(IEditorContext ctx) {
		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;

		this.Category = LazyWidgetCat.Misc;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		if(lui.BtnIcon(FontAwesomeIcon.Lightbulb, "OpenLightEditor", uis.BtnSmall, "Light editor"))
			dp("Light editor");
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Passport, "OpenCharacterEditor", uis.BtnSmall, "Pose editor"))
			ctx.Interface.TogglePosingWindow();
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Camera, "OpenCameraEditor", uis.BtnSmall, "Camera editor"))
			ctx.Interface.ToggleCameraWindow();
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.SyncAlt, "OpenTransformEditor", uis.BtnSmall, "Transform editor"))
			ctx.Interface.ToggleTransformWindow();
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.CameraRetro, "OpenLazyCamera", uis.BtnSmall, "Lazy camera"))
			ctx.Interface.ToggleLazyCamera();
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Lightbulb, "OpenLazyLights", uis.BtnSmall, "Lazy lights"))
			ctx.Interface.ToggleLazyLights();
	}
	private static void dp(string s) {
		Ktisis.Log.Debug(s);
	}
}
