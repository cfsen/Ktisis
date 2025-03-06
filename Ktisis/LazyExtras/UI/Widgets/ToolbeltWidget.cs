using ImGuiNET;

using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class ToolbeltWidget : ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool SupportsToolbelt { get; }
	public bool InToolbelt { get; set; }
	public Vector2 SizeToolbelt { get; }
	public ToolbeltWidget() {
		this.Category = LazyWidgetCat.Misc;
		this.SupportsToolbelt = false;
		this.InToolbelt = false;
	}
	public void UpdateScaling() {
		// Not implemented yet
	}
	public void Draw() {
		ImGui.Text("This will let you choose which widgets will appear on the floating toolbelt.");
	}
}
