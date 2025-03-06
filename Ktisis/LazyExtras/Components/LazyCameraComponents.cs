using ImGuiNET;

using Ktisis.Data.Files;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Overlay;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazyCameraSettings : JsonFile {
	public string Name { get; set; } = "";
	public Vector3 Position { get; set; } = Vector3.Zero;
	public Quaternion Rotation { get; set; } = Quaternion.Identity;
	public float fov { get; set; } = 0.0f;
	public float zoom { get; set; } = 1.0f;
}
public class LazyCameraComponents {
	private readonly IEditorContext ctx;

	private bool DelimitMinFoV = false;
	private Gizmo2D? gizmo;
	private Stopwatch stopwatch = new Stopwatch();
	private Vector3 vel = Vector3.Zero;
	private float gizmoSensitivity = 10.0f;

	private float dLastDt = 0.0f;
	private float dLowDt = float.MaxValue;
	private float dHighDt = float.MinValue;
	private float dAvgDt = 0.0f;

	private GizmoManager gm;

	public LazyCameraComponents(IEditorContext ctx) {
		this.ctx = ctx;
		this.gm = new(ctx.Config);
		gm.Initialize();
		//this.gizmo = gizmo;
		this.stopwatch.Start();
	}
	public void DrawGizmoConfigControls() {
		ImGui.SliderFloat("Sensitivity", ref this.gizmoSensitivity, 1.0f, 100.0f);
	}

	public unsafe void DrawNavControls() {

		EditorCamera? ec = this.ctx.Cameras.Current;
		if(ec == null) return;
		// TODO be less lazy
		float ypos = ec.RelativeOffset.Y;

		// makeshift delta time
		float dt = this.stopwatch.ElapsedMilliseconds / 1000.0f;
		this.stopwatch.Restart();
	
		//this.dUpdateDt(dt);
		this.gizmo ??= new(gm.Create(GizmoId.LazyGizmo), true, 0.8f);

		this.gizmo.Begin(new Vector2(310, 310));
		this.gizmo.Mode = ImGuizmo.Mode.World;

		// Arbitrary matrix, since we can derive world orientation via CalcRotation() later
		var matrix = Matrix4x4.CreateFromAxisAngle(Vector3.Zero, 1.0f); 
		this.gizmo.SetLookAt(new Vector3{ X = 0.0f, Y = 0.0f, Z = 1.0f}, matrix.Translation, 0.5f);
		var result = this.gizmo.Manipulate(ref matrix, out _);

		this.gizmo.End();

		if (result)	{
			/*
			 * This cursed abomination maps input from the translation gizmo's XY handles
			 * to movement along the XZ plane, relative to where the camera is facing.
			 * Y: Moves the camera forward
			 * X: Moves the camera laterally
			 * */
			double angle = ec.Camera->CalcRotation().X + Math.PI;
			Vector3 delta = Vector3.Zero;
			//float deltaDampening = 10.0f;
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
			this.vel += delta*this.gizmoSensitivity;
			//Ktisis.Log.Debug("vel:" + this._vel.ToString());
		}

		// Decelerate
		if(this.vel.X != 0.0f || this.vel.Z != 0.0f) {
			//Vector3 dbgPrev = ec.RelativeOffset;
			//Vector3 dbgTarget = ec.RelativeOffset*this._vel*dt;
			ec.RelativeOffset = Vector3.Lerp(ec.RelativeOffset, (ec.RelativeOffset+this.vel*dt), 0.1f);
			//Vector3 dbgAfter = ec.RelativeOffset;
			this.vel *= (float)Math.Pow(1e-7f, dt);

			// Stop when movement becomes hard to discern.
			if(this.vel.Length() < 1e-2f)
				this.vel = Vector3.Zero;

			//Ktisis.Log.Debug("dbg:" + dbgPrev.ToString() + "|" + dbgTarget.ToString() + "|" + dbgAfter.ToString());
		}

		// TODO less lazy
		ec.RelativeOffset.Y = ypos;
	}

	public void DrawCameraList() {
		ImGui.Text("Cameras:");
		ImGui.Separator();
		var l = this.ctx.Cameras.GetCameras().ToList();
		int i = 0;
		foreach ( var cam in l ) {
			ImGui.Text(cam.Name);
			ImGui.SameLine();
			var xoffset = ImGui.GetWindowSize().X - 100.0f;
			ImGui.SetCursorPosX(xoffset);

			// Disable button for active camera 
			if(cam == this.ctx.Cameras.Current)
				ImGui.BeginDisabled();

			if(ImGui.Button(
				$"Delete##lazyCamDelBtn{i}", 
				new Vector2 {
					X=80.0f, 
					Y=0
				})
			) {
				Ktisis.Log.Debug("LazyCamera: Removing camera: " + cam.Name);
				this.ctx.Cameras.RemoveCamera(cam);
			}

			if(cam == this.ctx.Cameras.Current)
				ImGui.EndDisabled();

			i++;
		}
	}

	private unsafe void GetCamData() {
		EditorCamera? ec = this.ctx.Cameras.Current;
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
		EditorCamera? ec = this.ctx.Cameras.Current;
		if(ec == null) return;
		// TODO !!! this isn't restored upon exiting gpose
		var minfov = 0.69f;
		if(this.DelimitMinFoV)
			minfov = 0.21f;

		if(!this.DelimitMinFoV)
			ImGui.BeginDisabled();

		ec.Camera->GameCamera.MinFoV = minfov;
		ImGui.SliderFloat("FOV", ref ec.Camera->GameCamera.FoV, minfov, 0.78f, "", ImGuiSliderFlags.AlwaysClamp);

		if(!this.DelimitMinFoV)
			ImGui.EndDisabled();

		ImGui.Checkbox("Allow lower FOV", ref this.DelimitMinFoV);
	}
}
