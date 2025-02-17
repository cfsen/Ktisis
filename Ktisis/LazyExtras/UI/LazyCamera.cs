using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;
using System.Runtime.CompilerServices;
using System.Linq;
using Ktisis.LazyExtras.Components;
using Ktisis.Editor.Camera.Types;

namespace Ktisis.Interface.Windows;

public class LazyCamera :KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly LazyCameraComponents _lazyCameraComponents;

	private bool _DelimitMinFoV = false;

	public LazyCamera(
		IEditorContext ctx,
		GuiManager gui
	) : base("Lazy camera") {
		this._ctx = ctx;
		this._gui = gui;
		this._lazyCameraComponents = new LazyCameraComponents(ctx, gui);
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(280,300),
			MaximumSize = new(560,600)
		};
	}

	public override void Draw() {
		// TODO Disable all interactions when work camera is enabled
		this.SetCamFov();
		this.GetCamData();
		this.CameraList();
	}

	private void NavControls() {

	}

	private void CameraList() {
		ImGui.Text("Cameras:");
		ImGui.Separator();
		var l = this._ctx.Cameras.GetCameras().ToList();
		int i = 0;
		foreach ( var cam in l ) {
			ImGui.Text(cam.Name);
			ImGui.SameLine();
			var xoffset = ImGui.GetWindowSize().X - 100.0f;
			ImGui.SetCursorPosX(xoffset);

			// Disable button for active camera 
			if(cam == this._ctx.Cameras.Current)
				ImGui.BeginDisabled();

			if(ImGui.Button(
				$"Delete##lazyCamDelBtn{i}", 
				new Vector2 {
					X=80.0f, 
					Y=0
				})
			) {
				Ktisis.Log.Debug("LazyCamera: Removing camera: " + cam.Name);
				this._ctx.Cameras.RemoveCamera(cam);
			}

			if(cam == this._ctx.Cameras.Current)
				ImGui.EndDisabled();

			i++;
		}
	}

	private unsafe void GetCamData() {
		EditorCamera? ec = this._ctx.Cameras.Current;
		if(ec == null) return;
		ImGui.Text("FoV:" + ec.Camera->GameCamera.FoV.ToString());
		ImGui.Text("Min FoV:" + ec.Camera->GameCamera.MinFoV.ToString());
		ImGui.Text("Max FoV:" + ec.Camera->GameCamera.MaxFoV.ToString());
		ImGui.Text("Far plane:" + ec.Camera->RenderEx->RenderCamera.FarPlane.ToString());
		ImGui.Text("Near plane:" + ec.Camera->RenderEx->RenderCamera.NearPlane.ToString());
		ImGui.Text("Finite near plane:" + ec.Camera->RenderEx->RenderCamera.FiniteFarPlane.ToString());
	}

	private unsafe void SetCamFov() {
		EditorCamera? ec = this._ctx.Cameras.Current;
		if(ec == null) return;
		// TODO !!! this isn't restored upon exiting gpose
		var minfov = 0.69f;
		if(this._DelimitMinFoV)
			minfov = 0.21f;

		ec.Camera->GameCamera.MinFoV = minfov;
		ImGui.SliderFloat("FOV", ref ec.Camera->GameCamera.FoV, minfov, 0.78f, "", ImGuiSliderFlags.AlwaysClamp);

		ImGui.Checkbox("Allow lower FOV", ref this._DelimitMinFoV);
	}
}
