using Dalamud.Interface;
using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets
{
	class PoseWidget :ILazyWidget {
		public LazyWidgetCat Category { get; }
		public int CustomOrder { get; set; }
		public bool InToolbelt { get; set; }
		public bool SupportsToolbelt { get; }
		public Vector2 SizeToolbelt { get; }

		private IEditorContext ctx;
		private LazyUi lui;
		private LazyUiSizes uis;

		public PoseWidget(IEditorContext ctx) {
			this.Category = LazyWidgetCat.Gesture;
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
			lui.DrawHeader(FontAwesomeIcon.PersonRays, "Pose utilities");
			
			ImGui.Text("Gaze control");
			if (ImGui.Button("Set neutral"))
				ctx.LazyExtras.pose.ResetGaze();
			ImGui.SameLine();
			if (ImGui.Button("Camera"))
				ctx.LazyExtras.pose.SetGazeAtCurrentCamera();
			ImGui.SameLine();
			if (ImGui.Button("Set target"))
				ctx.LazyExtras.pose.SetWorldGazeTarget();
			ImGui.SameLine();
			if (ImGui.Button("Look at"))
				ctx.LazyExtras.pose.SetGazeAtWorldTarget();
			ImGui.SameLine();
			ImGui.Text(ctx.LazyExtras.pose.TargetLookPosition.ToString());

			ImGui.Text("Bone overlay");
			if (ImGui.Button("Gesture bones"))
				ctx.LazyExtras.pose.ToggleGestureBones();
			ImGui.SameLine();
			if (ImGui.Button("Details"))
				ctx.LazyExtras.pose.ToggleGestureDetailBones();
			ImGui.SameLine();
			if (ImGui.Button("Hide all"))
				ctx.LazyExtras.pose.HideAllBones();
			ImGui.Spacing();
			ImGui.Text("Misc");
			if (ImGui.Button("Set expression export pose"))
				ctx.LazyExtras.pose.SetPartialReference();


			lui.DrawFooter();
			ImGui.EndGroup();
		}
    }
}
