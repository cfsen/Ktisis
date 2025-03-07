﻿using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System;
using System.Numerics;
using System.Xml.Serialization;

namespace Ktisis.LazyExtras.UI.Widgets;
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
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.CameraRetro, "Cameras");
		ctx.LazyExtras.camera.DrawNavControls();
		ctx.LazyExtras.camera.DrawGizmoConfigControls();
		DrawTranslationControls();
		DrawOrbitTarget();
		DrawAnglePan();
		DrawSliders();
		ImGui.Spacing();
		DrawCameraManagerControls();
		ctx.LazyExtras.camera.DrawCameraList();
		//lui.DrawFooter();
		ImGui.EndGroup();
	}

	private void DrawCameraManagerControls() {
		if(lui.BtnIcon(FontAwesomeIcon.Plus, "WCameraSpawn", uis.BtnSmaller, "Spawn camera"))
			ctx.Cameras.Create();
		ImGui.SameLine();
		ImGui.Text("Camera manager:");
		//ImGui.Separator();
	}

	// Ported from CameraWindow
	// TODO cleanup
	public void DrawCameraModeControls() {
		EditorCamera? camera = this.ctx.Cameras.Current;
		if(camera == null) return;

		var collide = !camera.Flags.HasFlag(CameraFlags.NoCollide);
		if (ImGui.Checkbox(this.ctx.Locale.Translate("camera_edit.toggles.collide"), ref collide))
			camera.Flags ^= CameraFlags.NoCollide;
		
		ImGui.SameLine();
		
		var delimit = camera.Flags.HasFlag(CameraFlags.Delimit);
		if (ImGui.Checkbox(this.ctx.Locale.Translate("camera_edit.toggles.delimit"), ref delimit))
			camera.SetDelimited(delimit);
	}
	public unsafe void DrawTranslationControls() {
		EditorCamera? camera = this.ctx.Cameras.Current;
		if(camera == null) return;
		var posVec = camera?.GetPosition() ?? null;
		if (posVec == null) return;

		var pos = posVec.Value;
		var isFixed = camera!.FixedPosition != null;

		if (!isFixed)
			pos -= camera.RelativeOffset;
		
		var lockIcon = isFixed ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
		var lockHint = isFixed
			? this.ctx.Locale.Translate("camera_edit.position.unlock")
			: this.ctx.Locale.Translate("camera_edit.position.lock");
		if (lui.BtnIcon(lockIcon, "WFixedCam", uis.BtnSmall, "Fixed cam"))
			camera.FixedPosition = isFixed ? null : pos;
		ImGui.SameLine();
		DrawCameraModeControls();

		bool _ = false;
		using (ImRaii.Disabled(!isFixed)) {
		if (lui.SliderTableRow("LWCamPos", ref pos, SliderFormatFlag.Position, ref _))
			camera.FixedPosition = pos;
		}

		bool __ = false;
		lui.SliderTableRow("LWCamOffset", ref camera.RelativeOffset, SliderFormatFlag.Position, ref __);
	}
	private unsafe void DrawOrbitTarget() {
		EditorCamera? camera = this.ctx.Cameras.Current;
		if(camera == null) return;
		using var _ = ImRaii.PushId("CameraOrbitTarget");
		
		var target = this.ctx.Cameras.ResolveOrbitTarget(camera);
		if (target == null) return;

		var isFixed = camera.OrbitTarget != null;
		var lockIcon = isFixed ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
		var lockHint = isFixed
			? this.ctx.Locale.Translate("camera_edit.orbit.unlock")
			: this.ctx.Locale.Translate("camera_edit.orbit.lock");
		if (lui.BtnIcon(lockIcon, "WOrbit", uis.BtnSmall, lockHint))
			camera.OrbitTarget = isFixed ? null : target.ObjectIndex;

		ImGui.SameLine();

		var text = $"Orbiting: {target.Name.TextValue}";
		if (isFixed)
			ImGui.Text(text);
		else
			ImGui.TextDisabled(text);
		
		ImGui.SameLine();
		//ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Buttons.CalcSize());
		if (lui.BtnIcon(FontAwesomeIcon.Sync, "WOffsetToTarget", uis.BtnSmall, this.ctx.Locale.Translate("camera_edit.offset.to_target"))) {
			var gameObject = (GameObject*)target.Address;
			var drawObject = gameObject->DrawObject;
			if (drawObject != null)
				camera.RelativeOffset = drawObject->Object.Position - gameObject->Position;
		}
	}

	// Everything below here is raw import from CameraWindow
		// TODO see what you can do to just get it running at least

	private unsafe void DrawAnglePan() {
		EditorCamera? camera = this.ctx.Cameras.Current;
		if(camera == null) return;
		var ptr = camera.Camera;
		if (ptr == null) return;

		// Camera angle
		var angleHint = this.ctx.Locale.Translate("camera_edit.angle");
		this.DrawIconAlign(FontAwesomeIcon.ArrowsSpin, out var spacing, angleHint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

		var angleDeg = ptr->Angle * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraAngle", ref angleDeg, 0.25f))
			ptr->Angle = angleDeg * MathHelpers.Deg2Rad;

		// Camera pan
		
		var panHint = this.ctx.Locale.Translate("camera_edit.pan");
		this.DrawIconAlign(FontAwesomeIcon.ArrowsAlt, out spacing, panHint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		
		var panDeg = ptr->Pan * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraPan", ref panDeg, 0.25f)) {
			panDeg.X %= 360.0f;
			panDeg.Y %= 360.0f;
			ptr->Pan = panDeg * MathHelpers.Deg2Rad;
		}
	}
	
	// Sliders

	private unsafe void DrawSliders() {
		EditorCamera? camera = this.ctx.Cameras.Current;
		if(camera == null) return;
		var ptr = camera.Camera;
		if (ptr == null) return;

		var rotateHint = this.ctx.Locale.Translate("camera_edit.sliders.rotation");
		var zoomHint = this.ctx.Locale.Translate("camera_edit.sliders.zoom");
		var distanceHint = this.ctx.Locale.Translate("camera_edit.sliders.distance");
		this.DrawSliderAngle("##CameraRotate", FontAwesomeIcon.CameraRotate, ref ptr->Rotation, -180.0f, 180.0f, 0.5f, rotateHint);
		this.DrawSliderAngle("##CameraZoom", FontAwesomeIcon.VectorSquare, ref ptr->Zoom, -40.0f, 100.0f, 0.5f, zoomHint);
		this.DrawSliderFloat("##CameraDistance", FontAwesomeIcon.Moon, ref ptr->Distance, ptr->DistanceMin, ptr->DistanceMax, 0.05f, distanceHint);
		if (camera.IsOrthographic) {
			var orthoHint = this.ctx.Locale.Translate("camera_edit.sliders.ortho_zoom");
			this.DrawSliderFloat("##OrthographicZoom", FontAwesomeIcon.LocationCrosshairs, ref camera.OrthographicZoom, 0.1f, 10.0f, 0.01f, orthoHint);
		}
	}

	private void DrawSliderAngle(string label, FontAwesomeIcon icon, ref float value, float min, float max, float drag, string hint = "") {
		this.DrawSliderIcon(icon, hint);
		ImGui.SliderAngle(label, ref value, min, max, "", ImGuiSliderFlags.AlwaysClamp);
		var deg = value * MathHelpers.Rad2Deg;
		if (this.DrawSliderDrag(label, ref deg, min, max, drag, true))
			value = deg * MathHelpers.Deg2Rad;
	}

	private void DrawSliderFloat(string label, FontAwesomeIcon icon, ref float value, float min, float max, float drag, string hint = "") {
		this.DrawSliderIcon(icon, hint);
		ImGui.SliderFloat(label, ref value, min, max, "");
		this.DrawSliderDrag(label, ref value, min, max, drag, false);
	}

	private void DrawSliderIcon(FontAwesomeIcon icon, string hint = "") {
		this.DrawIconAlign(icon, out var spacing, hint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - ImGui.GetCursorStartPos().X));
	}
	
	private bool DrawSliderDrag(string label, ref float value, float min, float max, float drag, bool angle) {
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		return ImGui.DragFloat($"{label}##Drag", ref value, drag, min, max, angle ? "%.0f°" : "%.3f");
	}

	// Alignment helpers

	private void DrawIconAlign(FontAwesomeIcon icon, out float spacing, string hint = "") {
		var padding = ImGui.GetStyle().CellPadding.X;
		var iconSpace = (UiBuilder.IconFont.FontSize - Icons.CalcIconSize(icon).X) / 2; // TODO glib dependency

		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding + iconSpace);
		Icons.DrawIcon(icon);
		if (!string.IsNullOrEmpty(hint) && ImGui.IsItemHovered())
		{
			using var _ = ImRaii.Tooltip();
			ImGui.Text(hint);
		}
		spacing = padding + iconSpace + ImGui.GetStyle().ItemInnerSpacing.X;
	}

	// New controller for camera :)

	public bool Joystick(string label, ref Vector2 output, float radius = 50.0f) {
		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		Vector2 center = cursorPos + new Vector2(radius, radius);

		ImGui.InvisibleButton(label, new Vector2(radius * 2, radius * 2));

		bool isActive = ImGui.IsItemActive();
		bool isHovered = ImGui.IsItemHovered();
		Vector2 dragOffset = ImGui.GetMouseDragDelta(0, 0.0f);
		float dragLength = dragOffset.Length();

		if (isActive) {
			if (dragLength > radius) { // Clamp to joystick boundary
				dragOffset = Vector2.Normalize(dragOffset) * radius;
			}
			output = dragOffset / radius;
		} 
		else if (ImGui.IsItemDeactivated()) {
			output = Vector2.Zero; // Reset when released
		}

		// Draw background circle
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f)), 32);
		drawList.AddCircle(center, radius, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1.0f)), 32);

		// Draw moving joystick
		Vector2 knobPos = center + output * radius;
		drawList.AddCircleFilled(knobPos, 10.0f, ImGui.GetColorU32(new Vector4(0.7f, 0.5f, 0.5f, 1.0f)), 16);

		return isActive;
	}
}
