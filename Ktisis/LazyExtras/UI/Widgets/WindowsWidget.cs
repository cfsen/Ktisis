using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Windows;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class WindowsWidget :ILazyWidget {
	private IEditorContext ctx;
	private LazyBase lb;
	private LazyUiSizes uis;

	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	public WindowsWidget(IEditorContext ctx) {
		this.ctx = ctx;
		this.lb = this.ctx.LazyExtras;
		this.uis = new();

		this.Category = LazyWidgetCat.Misc;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;
	}
	public void Draw() {
		if(ctx.LazyExtras.BtnIcon(FontAwesomeIcon.Lightbulb, "OpenLightEditor", uis.BtnSmall))
			dp("Light editor");
		if (lb.BtnIcon(FontAwesomeIcon.Passport, "OpenCharacterEditor", uis.BtnSmall))
			ctx.Interface.OpenPosingWindow();
		if (lb.BtnIcon(FontAwesomeIcon.Camera, "OpenCameraEditor", uis.BtnSmall))
			ctx.Interface.OpenCameraWindow();
		if (lb.BtnIcon(FontAwesomeIcon.SyncAlt, "OpenTransformEditor", uis.BtnSmall))
			ctx.Interface.OpenTransformWindow();
	}
	private static void dp(string s) {
		Ktisis.Log.Debug(s);
	}
}
