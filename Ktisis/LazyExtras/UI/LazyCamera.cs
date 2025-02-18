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
using Ktisis.Structs.Camera;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Overlay;
using System.ComponentModel.DataAnnotations;
using System;
using Ktisis.Common.Utility;
using System.Diagnostics;

namespace Ktisis.Interface.Windows;

public class LazyCamera :KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly LazyCameraComponents _lazyCameraComponents;

	private bool _DelimitMinFoV = false;
	private Gizmo2D _gizmo;
	private Stopwatch _stopwatch = new Stopwatch();
	private Vector3 _vel = Vector3.Zero;

	public LazyCamera(
		IEditorContext ctx,
		GuiManager gui,
		Gizmo2D giz
	) : base("Lazy camera") {
		this._ctx = ctx;
		this._gui = gui;
		this._lazyCameraComponents = new LazyCameraComponents(ctx, gui);
		this._gizmo = giz;
		this._stopwatch.Start();
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
		this.NavControls();
		this.SetCamFov();
		this.GetCamData();
		this.CameraList();
	}

	private unsafe void NavControls() {
		EditorCamera? ec = this._ctx.Cameras.Current;
		if(ec == null) return;
		float dt = this._stopwatch.ElapsedMilliseconds;
		this._stopwatch.Restart();

		var width = 300;
		var pos = ImGui.GetCursorScreenPos();
		var size = new Vector2(width, width);

		this._gizmo.Begin(size);
		this._gizmo.Mode = ImGuizmo.Mode.World;

		// Arbitrary matrix, since we can derive world orientation via CalcRotation() later
		var matrix = Matrix4x4.CreateFromAxisAngle(Vector3.Zero, 1.0f); 
		this._gizmo.SetLookAt(new Vector3{ X = 1.0f, Y = 1.0f, Z = 1.0f}, matrix.Translation, 0.5f);
		var result = this._gizmo.Manipulate(ref matrix, out _);

		this._gizmo.End();

		if (result)	{
			double angle = ec.Camera->CalcRotation().X + Math.PI;
			Vector3 delta = Vector3.Zero;
			float deltaDampening = 0.1f;
			// Forwards/backwards
			if(matrix.Translation.Y != 0.0f) {
				delta.X = (float)(Math.Sin(angle)*matrix.Translation.Y);
				delta.Z = (float)(Math.Cos(angle)*matrix.Translation.Y);
			}
			// sideways
			if(matrix.Translation.X != 0.0f)
			{
				delta.X += -(float)(Math.Sin(angle+(Math.PI/2))*matrix.Translation.X);
				delta.Z += -(float)(Math.Cos(angle+(Math.PI/2))*matrix.Translation.X);
			}

			this._vel += ec.RelativeOffset + delta*deltaDampening*dt;
			ec.RelativeOffset = Vector3.Lerp(ec.RelativeOffset, this._vel, 0.1f);
		}

		// Decelerate
		if(this._vel.X != 0.0f || this._vel.Y != 0.0f) {
			this._vel *= 0.01f;
		}
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
		ImGui.Text("Angle:" + ec.Camera->Angle);
		ImGui.Text("AngleXAdjusted" + (ec.Camera->Angle.X+Math.PI).ToString());
		ImGui.Text("FoV:" + ec.Camera->GameCamera.FoV.ToString());
		ImGui.Text("Min FoV:" + ec.Camera->GameCamera.MinFoV.ToString());
		ImGui.Text("Max FoV:" + ec.Camera->GameCamera.MaxFoV.ToString());
		ImGui.Text("Far plane:" + ec.Camera->RenderEx->RenderCamera.FarPlane.ToString());
		ImGui.Text("Near plane:" + ec.Camera->RenderEx->RenderCamera.NearPlane.ToString());
		ImGui.Text("Finite near plane:" + ec.Camera->RenderEx->RenderCamera.FiniteFarPlane.ToString());
		ImGui.Text("CalcPointDirection:");
		ImGui.Text(ec.Camera->CalcPointDirection().ToString());
		ImGui.Text("CalcRotation:");
		ImGui.Text(ec.Camera->CalcRotation().ToString()); // Vector3: {-pi, pi}, {-1.25, 1.4}, 0 | when rotating around character
		ImGui.Text("Origin:");
		ImGui.Text(ec.Camera->RenderEx->RenderCamera.Origin.ToString());
		ImGui.Text("Standard Z:");
		ImGui.Text(ec.Camera->RenderEx->RenderCamera.StandardZ.ToString());
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
