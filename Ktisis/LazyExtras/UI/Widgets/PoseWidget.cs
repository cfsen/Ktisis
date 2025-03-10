using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

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
			lui.DrawHeader(FontAwesomeIcon.PersonRays, $"Pose utilities: {ctx.LazyExtras.SelectedActor?.Name ?? "No target."}");
			
			// TODO selectmanager is not notified if the current selection is deleted.
			using(ImRaii.Disabled(ctx.LazyExtras.SelectedActor == null)) {

				ImGui.Text("Gaze control");
				if (ImGui.Button("Set neutral") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.pose.ResetGaze(ctx.LazyExtras.SelectedActor);
				ImGui.SameLine();
				if (ImGui.Button("Camera") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.pose.SetGazeAtCurrentCamera(ctx.LazyExtras.SelectedActor);
				ImGui.SameLine();
				if (ImGui.Button("Set target") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.pose.SetWorldGazeTarget();
				ImGui.SameLine();
				if (ImGui.Button("Look at") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.pose.SetGazeAtWorldTarget(ctx.LazyExtras.SelectedActor);
				ImGui.SameLine();
				ImGui.Text(ctx.LazyExtras.pose.TargetLookPosition.ToString());

				ImGui.Text("Bone overlay");
				if (ImGui.Button("Gesture bones") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.overlay.ToggleGestureBones(ctx.LazyExtras.SelectedActor);

				ImGui.SameLine();
				if (ImGui.Button("Details") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.overlay.ToggleGestureDetailBones(ctx.LazyExtras.SelectedActor!);
				ImGui.SameLine();
				if (ImGui.Button("Hide all") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.overlay.HideAllBones();
				ImGui.Spacing();
				ImGui.Text("Misc");
				if (ImGui.Button("Set expression export pose") && ctx.LazyExtras.SelectedActor != null)
					ctx.LazyExtras.pose.SetPartialReference();
				if (ImGui.Button("dbg hash lookup"))
					ctx.LazyExtras.pose.dbgTestGAT(ctx.LazyExtras.SelectedActor!);

			}

			lui.DrawFooter();
			ImGui.EndGroup();
		}
    }
}
