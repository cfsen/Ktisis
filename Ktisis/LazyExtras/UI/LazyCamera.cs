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

	private float _dLastDt = 0.0f;
	private float _dLowDt = float.MaxValue;
	private float _dHighDt = float.MinValue;
	private float _dAvgDt = 0.0f;

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
		//ImGui.Text(this._dAvgDt.ToString());
		this.NavControls();
		this.SetCamFov();
		//this.GetCamData();
		this.CameraList();
	}

	private void dUpdateDt(float dt) {
		if(dt < this._dLowDt) {
			this._dLowDt = dt;
			Ktisis.Log.Debug("New low dt: " + dt.ToString());
		}
		if(dt > this._dHighDt) {
			this._dHighDt = dt; 
			Ktisis.Log.Debug("New high dt: " + dt.ToString());
		}
		float t = (this._dLastDt+dt)/2;
		this._dAvgDt = t;
		this._dLastDt = dt;
	}

	private unsafe void NavControls() {

		EditorCamera? ec = this._ctx.Cameras.Current;
		if(ec == null) return;
		// TODO be less lazy
		float ypos = ec.RelativeOffset.Y;

		// makeshift delta time
		float dt = this._stopwatch.ElapsedMilliseconds / 1000.0f;
		this._stopwatch.Restart();
	
		//this.dUpdateDt(dt);

		this._gizmo.Begin(new Vector2(300, 300));
		this._gizmo.Mode = ImGuizmo.Mode.World;

		// Arbitrary matrix, since we can derive world orientation via CalcRotation() later
		var matrix = Matrix4x4.CreateFromAxisAngle(Vector3.Zero, 1.0f); 
		this._gizmo.SetLookAt(new Vector3{ X = 0.0f, Y = 0.0f, Z = 1.0f}, matrix.Translation, 0.5f);
		var result = this._gizmo.Manipulate(ref matrix, out _);

		this._gizmo.End();

		if (result)	{
			/*
			 * This cursed abomination maps input from the translation gizmo's XY handles
			 * to movement along the XZ plane, relative to where the camera is facing.
			 * Y: Moves the camera forward
			 * X: Moves the camera laterally
			 * */
			double angle = ec.Camera->CalcRotation().X + Math.PI;
			Vector3 delta = Vector3.Zero;
			float deltaDampening = 10.0f;
			// Forwards/backwards
			if(matrix.Translation.Y != 0.0f) {
				delta.X = (float)(Math.Sin(angle)*matrix.Translation.Y);
				delta.Z = (float)(Math.Cos(angle)*matrix.Translation.Y);
			}
			// lateral
			if(matrix.Translation.X != 0.0f) {
				delta.X += -(float)(Math.Sin(angle+(Math.PI/2))*matrix.Translation.X);
				delta.Z += -(float)(Math.Cos(angle+(Math.PI/2))*matrix.Translation.X);
			}

			// buffer changes
			this._vel += delta*deltaDampening;
			//Ktisis.Log.Debug("vel:" + this._vel.ToString());
		}

		// Decelerate
		if(this._vel.X != 0.0f || this._vel.Z != 0.0f) {
			//Vector3 dbgPrev = ec.RelativeOffset;
			//Vector3 dbgTarget = ec.RelativeOffset*this._vel*dt;
			ec.RelativeOffset = Vector3.Lerp(ec.RelativeOffset, (ec.RelativeOffset+this._vel*dt), 0.1f);
			//Vector3 dbgAfter = ec.RelativeOffset;
			this._vel *= (float)Math.Pow(1e-7f, dt);

			// Stop when movement becomes hard to discern.
			if(this._vel.Length() < 1e-2f)
				this._vel = Vector3.Zero;

			//Ktisis.Log.Debug("dbg:" + dbgPrev.ToString() + "|" + dbgTarget.ToString() + "|" + dbgAfter.ToString());
		}

		// TODO less lazy
		ec.RelativeOffset.Y = ypos;
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
