using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets
{
	class DemoWidget :ILazyWidget {
		public LazyWidgetCat Category { get; }
		public int CustomOrder { get; set; }
		public bool InToolbelt { get; set; }
		public bool SupportsToolbelt { get; }
		public Vector2 SizeToolbelt { get; }

		private IEditorContext ctx;
		private LazyUiSizes uis;
		private float gs;

		public DemoWidget(IEditorContext ctx) {
			this.Category = LazyWidgetCat.Misc;
			this.SupportsToolbelt = false;
			this.SizeToolbelt = Vector2.Zero;
			this.InToolbelt = false;

			this.ctx = ctx;
			this.uis = ctx.LazyExtras.Sizes;
			this.gs = ctx.LazyExtras.Sizes.Scale;
		}
		public void Draw() {
			ImGui.Text("Hello");
		}
    }
}
