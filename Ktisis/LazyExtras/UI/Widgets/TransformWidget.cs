using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Interface.Components.Transforms;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

using System;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

namespace Ktisis.LazyExtras.UI.Widgets
{
	class TransformWidget :ILazyWidget {
		public LazyWidgetCat Category { get; }
		public int CustomOrder { get; set; }
		public bool InToolbelt { get; set; }
		public bool SupportsToolbelt { get; }
		public Vector2 SizeToolbelt { get; }

		private IEditorContext ctx;
		private LazyUi lui;
		private LazyUiSizes uis;
		private float gs;

		private Vector3 bufferPos;
		private Vector3 bufferRot;
		private Vector3 bufferRotLast;
		private Vector3 bufferScale;
		
		private ITransformMemento? xfmState;
		private bool xfmEnded;

		public TransformWidget(IEditorContext ctx) {
			this.Category = LazyWidgetCat.Misc;
			this.SupportsToolbelt = false;
			this.SizeToolbelt = Vector2.Zero;
			this.InToolbelt = false;

			this.ctx = ctx;
			this.lui = new();
			this.uis = lui.uis;
			this.gs = lui.uis.Scale;

			this.bufferPos = Vector3.Zero;
			this.bufferRot = Vector3.Zero;
			this.bufferRotLast = Vector3.Zero;
			this.bufferScale = Vector3.Zero;
			
			this.xfmState = null;
			this.xfmEnded = false;
		}
		public void UpdateScaling() {
			this.uis.RefreshScale();
		}
		public void Draw() {
			BufferRegenerate();
			if (ImGui.BeginChild("LazyTransformWidgetContainer", new(uis.SidebarW, 200))) {
				DrawHeader();

				using (ImRaii.Disabled(ctx.Transform.Target == null || !ctx.Posing.IsEnabled)) {
					if (lui.SliderTableRow("LazyPositionSlider", ref bufferPos, SliderFormatFlag.Position, ref xfmEnded))
						TableUpdate(SliderFormatFlag.Position);
					if (lui.SliderTableRow("LazyRotationSlider", ref bufferRot, SliderFormatFlag.Rotation, ref xfmEnded))
						TableUpdate(SliderFormatFlag.Rotation);
					if (lui.SliderTableRow("LazyScaleSlider", ref bufferScale, SliderFormatFlag.Scale, ref xfmEnded))
						TableUpdate(SliderFormatFlag.Scale);
				}

				DrawFooter();
				ImGui.EndChild();
			}

			if(xfmEnded && xfmState != null) {
				xfmEnded = false;
				xfmState.Dispatch();
				xfmState = null;
			} 
		}
		private void TableUpdate(SliderFormatFlag type) {
			if(ctx.Transform.Target is not ITransformTarget xfm) return;

			Transform xfmb = ctx.Transform.Target?.GetTransform() ?? new Transform();
			xfmState ??= ctx.Transform.Begin(xfm);

			switch (type) {
				case SliderFormatFlag.Position:
					xfmb.Position = bufferPos;
					break;
				case SliderFormatFlag.Rotation:
					HandleRotation(ref bufferRot, ref bufferRotLast, ref xfmb.Rotation);
					break;
				case SliderFormatFlag.Scale:
					xfmb.Scale = bufferScale;
					break;
				default: break;
			}
			xfmState?.SetTransform(xfmb);
		}
		private void BufferRegenerate() {
			if(ctx.Transform.Target is not ITransformTarget xfm) return;
			var xfmb = xfm.GetTransform() ?? new Transform();
			bufferPos = xfmb.Position;
			bufferScale = xfmb.Scale;
		}

		private void HandleRotation(ref Vector3 state, ref Vector3 last, ref Quaternion rot) {
			if(state.X != last.X)
				rot *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, (last.X-state.X)*MathF.PI/180.0f);
			if(state.Y != last.Y)
				rot *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (last.Y-state.Y)*MathF.PI/180.0f);
			if(state.Z != last.Z)
				rot *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (last.Z-state.Z)*MathF.PI/180.0f);

			for(int i = 0; i < 3; i++) {
				switch(bufferRot[i]) {
					case > 360.0f:
						bufferRot[i] -= 360;
						break;
					case < 0:
						bufferRot[i] += 360;
						break;
					default: break;
				}
			}

			bufferRotLast = bufferRot;
		}

		private void DrawHeader(){ 
		}

		private void DrawFooter() {
			ImGui.Checkbox("Parenting", ref ctx.Config.Gizmo.ParentBones);
			ImGui.SameLine();
			ImGui.Checkbox("Relative", ref ctx.Config.Gizmo.RelativeBones);
			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			// World/Local transform
			if(lui.BtnIcon((ctx.Config.Gizmo.Mode == ImGuizmo.Mode.World ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home), 
				"LazyWorldLocalToggle", uis.BtnSmall, (ctx.Config.Gizmo.Mode == ImGuizmo.Mode.World ? "Global" : "Local")))
				ctx.Config.Gizmo.Mode = ctx.Config.Gizmo.Mode ==  ImGuizmo.Mode.World? ImGuizmo.Mode.Local : ImGuizmo.Mode.World;
			ImGui.SameLine();
			// Gizmo toggle
			if(lui.BtnIcon((ctx.Config.Gizmo.Visible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash),
				"LazyGizmoVisToggle", uis.BtnSmall, (ctx.Config.Gizmo.Visible ? "Visible" : "Hidden")))
				ctx.Config.Gizmo.Visible ^= true;
			ImGui.SameLine();
			// Mirror mode
			if(lui.BtnIcon((ctx.Config.Gizmo.MirrorRotation ? FontAwesomeIcon.ArrowDownUpAcrossLine : FontAwesomeIcon.GripLines), 
				"LazyMirrorRotationToggle", uis.BtnSmall, (ctx.Config.Gizmo.MirrorRotation ? "Symmetrical" : "Asymmetrical")))
				ctx.Config.Gizmo.MirrorRotation ^= true;
		}
		private static void dp(string s) =>	Ktisis.Log.Debug(s); 
    }
}
