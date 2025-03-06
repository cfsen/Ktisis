using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets
{
	class CameraWidget :ILazyWidget {
		public LazyWidgetCat Category { get; }
		public int CustomOrder { get; set; }
		public bool InToolbelt { get; set; }
		public bool SupportsToolbelt { get; }
		public Vector2 SizeToolbelt { get; }

		private IEditorContext ctx;
		private LazyUi lui;
		private LazyUiSizes uis;

		public CameraWidget(IEditorContext ctx) {
			this.Category = LazyWidgetCat.Misc;
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
			ctx.LazyExtras.camera.DrawNavControls();
			ctx.LazyExtras.camera.DrawGizmoConfigControls();
			ctx.LazyExtras.camera.DrawCameraList();
		}
    }
}
