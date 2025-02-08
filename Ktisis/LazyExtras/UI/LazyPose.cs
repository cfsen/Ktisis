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
	public LazyPose(
		IEditorContext ctx
	) : base("Lazy pose") {
		this._ctx = ctx;
		this._components = new LazyPoseComponents(ctx);
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(280,120),
			MaximumSize = new(560,600)
		};
	}

	public override void Draw() {
		ImGui.Text("Gaze control");
		if (ImGui.Button("Camera"))
			this._components.LookAtCamera();
		ImGui.SameLine();
		if (ImGui.Button("Set target"))
			this._components.SetTarget();
		ImGui.SameLine();
		if (ImGui.Button("Look at"))
			this._components.LookAtCamera(this._components.TargetLookPosition);
		ImGui.SameLine();
		ImGui.Text(this._components.TargetLookPosition.ToString());

		ImGui.Separator();
		ImGui.Text("Bone overlay");
		if (ImGui.Button("Show gesture bones"))
			this._components.ToggleFigureBones();
		ImGui.SameLine();
		if (ImGui.Button("Hide all bones"))
			this._components.HideAllBones();
	}
}
