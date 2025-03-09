using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;
using Ktisis.LazyExtras.Components;

namespace Ktisis.Interface.Windows;

public class LazyPose :KtisisWindow {
	private readonly IEditorContext _ctx;
	private LazyPoseComponents _components;
	private Vector2 v2;
	public LazyPose(
		IEditorContext ctx
	) : base("Lazy pose") {
		this._ctx = ctx;
		this.v2 = Vector2.Zero;
		this._components = this._ctx.LazyExtras.pose;
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(280,120),
			MaximumSize = new(560*2,600*2)
		};
	}

	public override void Draw() {
		ImGui.Text("Gaze control");
		if (ImGui.Button("Camera"))
			this._components.SetGazeAtCurrentCamera();
		ImGui.SameLine();
		if (ImGui.Button("Set target"))
			this._components.SetWorldGazeTarget();
		ImGui.SameLine();
		if (ImGui.Button("Look at"))
			this._components.SetGazeAtWorldTarget();
		ImGui.SameLine();
		if (ImGui.Button("Reset gaze"))
			this._components.ResetGaze();
		ImGui.Text(this._components.TargetLookPosition.ToString());

		ImGui.Separator();
		ImGui.Text("Bone overlay");
		if (ImGui.Button("Gesture bones"))
			this._components.ToggleGestureBones();
		ImGui.SameLine();
		if (ImGui.Button("Details"))
			this._components.ToggleGestureDetailBones();
		ImGui.SameLine();
		if (ImGui.Button("Hide all"))
			this._components.HideAllBones();

		//ImGui.Separator();
		//ImGui.SameLine();
		//if (ImGui.Button("Debug")) {
		//	this._components.SetGazeAtCurrentCamera();
		//	//this._components.dbgCsM4();
		//}

		ImGui.Separator();
		if (ImGui.Button("Set expression export pose"))
			this._components.SetPartialReference();
		//ImGui.SameLine();
		//if (ImGui.Button("Eye"))
		//	this._components.SelectEyeBall();
		Joystick("joy", ref v2);
		ImGui.Text(v2.ToString());
		ImGui.Separator();
		//this._components.dbgMatrixInspector("Left Eye");
		//this._components.dbgMatrixInspector("Right Eye");
		//this._components.dbgCsM4();
	}

	public static bool Joystick(string label, ref Vector2 output, float radius = 50.0f) {
		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		Vector2 center = cursorPos + new Vector2(radius, radius);

		ImGui.InvisibleButton(label, new Vector2(radius * 2, radius * 2));

		bool isActive = ImGui.IsItemActive();
		bool isHovered = ImGui.IsItemHovered();
		Vector2 dragOffset = ImGui.GetMouseDragDelta(0, 0.0f);
		float dragLength = dragOffset.Length();

		if (isActive)
		{
			if (dragLength > radius) // Clamp to joystick boundary
			{
				dragOffset = Vector2.Normalize(dragOffset) * radius;
			}
			output = dragOffset / radius;
		} else if (ImGui.IsItemDeactivated())
		{
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
