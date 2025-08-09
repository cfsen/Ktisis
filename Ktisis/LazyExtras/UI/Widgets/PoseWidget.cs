using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;
using System.Runtime.InteropServices.Marshalling;

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
			using(ImRaii.Disabled(ctx.LazyExtras.SelectedActor == null))
			{
				DrawGazeBasicControls();
				ImGui.Spacing();
				DrawGazeTargetControls();
				ImGui.Spacing();
				DrawOverlayControls();
				ImGui.Spacing();

				// TODO move this somewhere else
				//ImGui.Text("Misc");
				if(lui.BtnIcon(FontAwesomeIcon.SquarePersonConfined, "WDG_SetPartialReference", uis.BtnSmall, "Set partial reference pose"))
					ctx.LazyExtras.pose.SetPartialReference();

				//DrawPoseMirrorControls();

			}

			lui.DrawFooter();
			ImGui.EndGroup();
		}

		private void DrawPoseMirrorControls() {
			if (lui.BtnIcon(FontAwesomeIcon.ArrowsLeftRight, "WDG_PoseMirror", uis.BtnSmall, "Gesture overlay"))
				ctx.LazyExtras.pose.mirror.Flip();
		}
		private void DrawOverlayControls() {
			ImGui.Text("Bone overlay");
			if (lui.BtnIcon(FontAwesomeIcon.PersonFalling, "WDG_PoseGestureOverlay", uis.BtnSmall, "Gesture overlay"))
				ctx.LazyExtras.overlay.ToggleGestureBones(ctx.LazyExtras.SelectedActor!);

			ImGui.SameLine();
			if (lui.BtnIcon(FontAwesomeIcon.Plus, "WDG_PoseGestureOverlayExtra", uis.BtnSmall, "Gesture details"))
				ctx.LazyExtras.overlay.ToggleGestureDetailBones(ctx.LazyExtras.SelectedActor!);

			ImGui.SameLine();
			if (lui.BtnIcon(FontAwesomeIcon.HandHolding, "WDG_PoseGestureOverlayHands", uis.BtnSmall, "Gesture details"))
				ctx.LazyExtras.overlay.ToggleHandBones(ctx.LazyExtras.SelectedActor!);

			ImGui.SameLine();
			if (lui.BtnIcon(FontAwesomeIcon.UserSlash, "WDG_PoseGestureOverlayHideAll", uis.BtnSmall, "Hide overlay"))
				ctx.LazyExtras.overlay.HideAllBones();
		}

		private void DrawGazeBasicControls() {
			ImGui.Text("Gaze control");
			if (lui.BtnIcon(FontAwesomeIcon.EyeSlash, "WDG_PoseSetNeutral", uis.BtnSmall, "Neutral gaze"))
				ctx.LazyExtras.pose.ResetGaze(ctx.LazyExtras.SelectedActor!);

			ImGui.SameLine();
			if (lui.BtnIcon(FontAwesomeIcon.Eye, "WDG_PoseSetGazeToCam", uis.BtnSmall, "Look at camera"))
				ctx.LazyExtras.pose.SetGazeAtCurrentCamera(ctx.LazyExtras.SelectedActor!);

			ImGui.SameLine();
			if (lui.BtnIcon(FontAwesomeIcon.Recycle, "WDG_Pose_ResetFocalPoint", uis.BtnSmaller, "Reset"))
				ctx.LazyExtras.pose.GazeFocalPointScalar = 0.0f;

			ImGui.SameLine();
			using (ImRaii.ItemWidth(uis.SidebarW/3))
			{
				ImGui.DragFloat("Focal depth", ref ctx.LazyExtras.pose.GazeFocalPointScalar, 0.001f, -0.5f, 0.5f);
			}
		}

		private void DrawGazeTargetControls() {
			if (lui.BtnIcon(FontAwesomeIcon.EyeDropper, "WDG_PoseSetGazeTarget", uis.BtnSmall, "Set gaze target"))
				ctx.LazyExtras.pose.SetWorldGazeTarget();
			ImGui.SameLine();
			if (lui.BtnIcon(FontAwesomeIcon.Bullseye, "WDG_PoseSetGazeToTarget", uis.BtnSmall, "Gaze at target"))
				ctx.LazyExtras.pose.SetGazeAtWorldTarget(ctx.LazyExtras.SelectedActor!);
			ImGui.SameLine();
			ImGui.Text(ctx.LazyExtras.pose.TargetLookPosition.ToString());
		}
	}
}
