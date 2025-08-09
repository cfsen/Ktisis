using Dalamud.Bindings.ImGui;

using Ktisis.Data.Files;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Overlay;

using System;
using System.Collections.Generic;
using System.Data.Common;
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

	private Vector3 cameraVelocity = Vector3.Zero;
	private Stopwatch delta = new();
	private float dt;

	public Vector3 JoystickBuffer;
	public float JoystickSensitivty = 5.0f;


	public LazyCameraComponents(IEditorContext ctx) {
		this.ctx = ctx;
		this.delta.Start();
	}
	// RFAC: Move to UI

	// RFAC START

	// Joystick widget

	public void HandleJoystick() {
		EditorCamera? ec = this.ctx.Cameras.Current;
		if(ec == null) return;
		// TODO be less lazy; this locks the camera from traversing vertically. make it more elegant.
		//float ypos = ec.RelativeOffset.Y;
		UpdateDeltaTime(ec);
		CalcCameraVelocity(ec, JoystickBuffer);
		CalcCameraDeceleration(ec);
		// TODO less lazy
		//ec.RelativeOffset.Y = ypos;
	}
	
	private void UpdateDeltaTime(EditorCamera ec) {
		// makeshift delta time
		dt = this.delta.ElapsedMilliseconds / 1000.0f;
		this.delta.Restart();

		// TODO EDGE CASE
			// Make a hard cutoff for high dt (t>1s?)
	}
	private unsafe void CalcCameraVelocity(EditorCamera ec, Vector3 input) {
		double angle = ec.Camera->CalcRotation().X + Math.PI;
		Vector3 delta = Vector3.Zero;

		// translates to camera longitudinal motion
		if(input.Z != 0.0f) {
			delta.X = -(float)(Math.Sin(angle)*input.Z);
			delta.Z = -(float)(Math.Cos(angle)*input.Z);
		}
		// translates to camera lateral motion
		if(input.X != 0.0f) {
			delta.X += -(float)(Math.Sin(angle+(Math.PI/2))*input.X);
			delta.Z += -(float)(Math.Cos(angle+(Math.PI/2))*input.X);
		}

		if(input.Y != 0.0f)
		{
			
		}

		// buffer changes
		this.cameraVelocity += delta*this.JoystickSensitivty;
	}
	private void CalcCameraDeceleration(EditorCamera ec) {
		// Decelerate
		if(this.cameraVelocity.X != 0.0f || this.cameraVelocity.Z != 0.0f) {
			ec.RelativeOffset = Vector3.Lerp(ec.RelativeOffset, (ec.RelativeOffset+cameraVelocity*dt), 0.1f);
			this.cameraVelocity *= (float)Math.Pow(1e-7f, dt);

			// Stop when movement becomes hard to discern.
			if(this.cameraVelocity.Length() < 1e-2f)
				this.cameraVelocity = Vector3.Zero;
		}
	}
	// RFAC END

	// TODO this is mostly UI stuff, move out of components?
	public void DrawCameraList() {
		//ImGui.Text("Camera manager:");
		//ImGui.Separator();
		var l = this.ctx.Cameras.GetCameras().ToList();
		int i = 0;
		foreach ( var cam in l ) {
			ImGui.Text(cam.Name);
			ImGui.SameLine();
			var xoffset = ImGui.GetWindowSize().X - 190.0f;
			ImGui.SetCursorPosX(xoffset);

			// Disable button for active camera 

			if(cam == this.ctx.Cameras.Current)
				ImGui.BeginDisabled();
			//if(cam != this.ctx.Cameras.Current)
			//	ImGui.BeginDisabled();
			if(ImGui.Button($"Select##lazyCamSelBtn{i}", new(80.0f, 0))) 
				this.ctx.Cameras.SetCurrent(cam);
			//if(cam != this.ctx.Cameras.Current)
			//	ImGui.EndDisabled();
			ImGui.SameLine();

			if(ImGui.Button( $"Delete##lazyCamDelBtn{i}", new Vector2 { X=80.0f, Y=0 }) ) 
				this.ctx.Cameras.RemoveCamera(cam);
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
