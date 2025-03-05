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

		public void Draw() {
			BufferRegenerate();

			using (ImRaii.Disabled(ctx.Transform.Target == null || !ctx.Posing.IsEnabled)) {
				if (lui.SliderTableRow("LazyPositionSlider", ref bufferPos, SliderFormatFlag.Position, ref xfmEnded))
					TableUpdate(SliderFormatFlag.Position);
				if (lui.SliderTableRow("LazyRotationSlider", ref bufferRot, SliderFormatFlag.Rotation, ref xfmEnded))
					TableUpdate(SliderFormatFlag.Rotation);
				if (lui.SliderTableRow("LazyScaleSlider", ref bufferScale, SliderFormatFlag.Scale, ref xfmEnded))
					TableUpdate(SliderFormatFlag.Scale);
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

		private static void dp(string s) =>	Ktisis.Log.Debug(s); 
    }
}
