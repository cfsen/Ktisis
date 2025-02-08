using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;
using Ktisis.Structs.Lights;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazyPoseComponents {
	private readonly IEditorContext _ctx;
	public Vector3 TargetLookPosition = Vector3.Zero;
	public LazyPoseComponents(IEditorContext ctx) {
		this._ctx = ctx;
	}
	#region look at target
	public void SetTarget() {
		// targets the current selection for gazing
		if (this._ctx.Transform.Target == null)
			return;
		this.TargetLookPosition = this._ctx.Transform.Target.GetTransform()?.Position ?? Vector3.Zero;
	}
	#endregion
	#region working implementation
	private void OrientEyeToCam(Vector3? targetOverride = null) {
		if (this._ctx.Transform.Target == null || this._ctx.Cameras.Current == null)
			return;

		Vector3 currentCam;
		if (this._ctx.Cameras.Current?.FixedPosition != null)
		{
			currentCam = this._ctx.Cameras.Current?.FixedPosition ?? Vector3.Zero;
			currentCam += this._ctx.Cameras.Current?.RelativeOffset ?? Vector3.Zero;
		} else
			currentCam = this._ctx.Cameras.Current?.GetPosition() ?? Vector3.Zero;

		var target = this._ctx.Transform.Target;
		Transform selectedBone = target?.GetTransform() ?? new Transform();

		// for gazing at non-camera target
		if (targetOverride != null)
			currentCam = targetOverride.Value;

		// buffer changes 
		Transform tmp = selectedBone;
		tmp.Rotation = this.TransformEyeToCamera(selectedBone, currentCam);

		// update transform
		target?.SetTransform(tmp);
	}
	public void LookAtCamera(Vector3? targetOverride = null) {
		var lastSelected = this._ctx.Selection.GetSelected().FirstOrDefault();
		var selected = this.ResolveActorEntity();
		if (selected == null)
			return;

		// TODO eeeh I could just grab the relevant bones individually and not need to loop
		foreach (SceneEntity s in selected.Recurse().Where(s => s.Type == EntityType.BoneNode))
		{
			if (s.Name == "Left Eye" || s.Name == "Right Eye")
			{
				this._ctx.Selection.Select(s);
				this.OrientEyeToCam(targetOverride);
			}
		}

		if (lastSelected != null)
			this._ctx.Selection.Select(lastSelected);
	}
	private Quaternion TransformEyeToCamera(Transform org, Vector3 cameraPosition) {
		// Orients the eye towards the camera
		Matrix4x4 bill = Matrix4x4.CreateBillboard(org.Position, cameraPosition, Vector3.UnitY, Vector3.UnitX);
		// Orients the eye outwards again.
		Matrix4x4 yflip = Matrix4x4.CreateRotationY(this.DegToRad(90.0f));
		bill = Matrix4x4.Multiply(yflip, bill);
		// Reorient the eye X axis
		Matrix4x4 xflip = Matrix4x4.CreateRotationX(this.DegToRad(HkaEulerAngles.ToEuler(org.Rotation).X));
		bill = Matrix4x4.Multiply(xflip, bill);
		return Quaternion.CreateFromRotationMatrix(bill);
	}
	#endregion
	#region support functions
	private double RadToDeg(float rad) {
		return rad * 180 / Math.PI;
	}
	private float DegToRad(float deg) {
		return (float)(deg * Math.PI / 180);
	}
	#endregion
	#region work in progress
	private float EyeHeadOffset() {
		var selected = this._ctx.Selection.GetSelected().OfType<ActorEntity>().FirstOrDefault();
		var head = selected?.Pose?.FindBoneByName("j_kao")?.GetTransform() ?? null;
		if (head == null)
			return 0.0f;
		return 360.0f - HkaEulerAngles.ToEuler(head.Rotation).X;

	}
	private void DumpsterDive(bool reset = false) {
		// Must select an ActorEntity
		var selected = this.ResolveActorEntity();
		var lastSelected = this._ctx.Selection.GetSelected().OfType<ActorEntity>().FirstOrDefault();
		if (selected == null)
			return;
		// TODO lookup parent ActorEntity
		// camera check
		var cameraPosition = this._ctx.Cameras.Current?.GetPosition() ?? null;
		if (cameraPosition == null)
			return;

		// yoink relevant transforms
		var leftEye = selected.Pose?.FindBoneByName("j_f_eye_l") ?? null;
		var rightEye = selected.Pose?.FindBoneByName("j_f_eye_r") ?? null;
		var head = selected.Pose?.FindBoneByName("j_kao") ?? null;

		if (leftEye == null || rightEye == null || head == null)
			return;

		Transform? leftEyeTransform = leftEye.GetTransform() ?? null;
		Transform? rightEyeTransform = rightEye.GetTransform() ?? null;
		Transform? headTransform = head.GetTransform() ?? null;

		// Wow, it's even more null-checking!
		if (leftEyeTransform == null || rightEyeTransform == null || headTransform == null)
			return;
		// Preserve rotation around X axis for eyes, which is always: head.X + eye.X = 360
		float eyeOrientation = 360.0f - HkaEulerAngles.ToEuler(headTransform.Rotation).X;

		// Ktisis.Log.Debug("DD: eyeOrientation=" + eyeOrientation.ToString());

		foreach (SceneEntity s in selected.Recurse())
		{
			if (s.Type == EntityType.BoneNode)
			{
				if (s.Name == "Left Eye")
				{
					this._ctx.Selection.Select(s);
					this.OrientEyes("Left eye");
				}
				if (s.Name == "Right Eye")
				{
					this._ctx.Selection.Select(s);
					this.OrientEyes("Right eye");
				}
			}
		}
		if (lastSelected != null)
			this._ctx.Selection.Select(lastSelected);
	}
	private Quaternion AlternateEyeToCam(Transform org, Vector3 cameraPosition, string dbgLbl) {
		var meu = HkaEulerAngles.ToEuler(org.Rotation);

		/* What if you try to just move the difference?
		 * adj-meu type of thing?
		 * */
		float offsetY = this.DegToRad(2.0f);
		if (dbgLbl == "Left eye")
			offsetY *= -1;
		Vector3 v = cameraPosition - org.Position;
		float adjY = (float)Math.Atan2(v.X, v.Z)+(float)Math.PI/2+offsetY;
		float adjZ = (float)Math.Atan2(v.Y, v.Z)+(float)Math.PI;
		float adjX = 0.0f;

		Ktisis.Log.Debug("#### " + dbgLbl + " ####");
		Ktisis.Log.Debug("> v=" + v.ToString());
		Ktisis.Log.Debug("> adjY=" + adjY.ToString() + " | deg=" + this.RadToDeg(adjY));
		Ktisis.Log.Debug("> adjZ=" + adjZ.ToString() + " | deg=" + this.RadToDeg(adjZ));
		Ktisis.Log.Debug("> adjX=" + meu.X.ToString());

		// TODO this fails 
		bool limits = false;
		if (limits)
		{
			// valid boundaries: 35 to 0, 0 to 320
			float zmin = this.DegToRad(35.0f);
			float zmax = this.DegToRad(320.0f);

			// Check if outside bounds
			if (adjZ > zmin && adjZ < zmax)
			{

				Ktisis.Log.Debug("! Z out of bounds");

				// Reorient to closest maxium allowed angle
				if (adjZ > (zmax - zmin) / 2)
					adjZ = zmax;
				else
					adjZ = zmin;
			}

			float ymin = 0.0f;
			float ymax = 0.0f;

			if (dbgLbl == "Right eye")
			{
				ymin = this.DegToRad(52.0f);
				ymax = this.DegToRad(122.0f);
			} else
			{
				ymin = this.DegToRad(62.0f);
				ymax = this.DegToRad(140.0f);
			}


			if (adjY > ymin && adjY < ymax)
			{

				Ktisis.Log.Debug("! Y out of bounds");

				float secondSolution = adjY - (float)Math.PI / 2;
				if (!(secondSolution > ymin && secondSolution < ymax))
				{
					adjY = secondSolution;
				} else
				{
					if (adjY > (ymax - ymin))
						adjY = ymax;
					else
						adjY = ymin;
				}
			}
		}
		adjX = this.DegToRad(360.0f - meu.X);
		Matrix4x4 m4 = Matrix4x4.CreateFromYawPitchRoll(adjX, adjY, adjZ);
		return Quaternion.CreateFromRotationMatrix(m4);
	}

	private void ResetEyes() {

	}

	private void OrientEyes(string eye = "") {
		if (this._ctx.Transform.Target == null || this._ctx.Cameras.Current == null)
			return;
		Vector3 currentCam = this._ctx.Cameras.Current?.GetPosition() ?? new Vector3(0, 0, 0);
		var target = this._ctx.Transform.Target;
		Transform selectedBone = target?.GetTransform() ?? new Transform();

		//buffer changes 
		Transform tmp = new Transform();
		tmp = selectedBone;
		tmp.Rotation = this.AlternateEyeToCam(selectedBone, currentCam, eye);
		target.SetTransform(tmp);
	}
	#endregion
	#region visibility toggles
	public void ToggleFigureBones() {
		var selected = this.ResolveActorEntity();
		if (selected == null)
			return;

		var bones = new List<string>(){"Abdomen", "Waist", "Lumbar", "Thoracic", "Cervical", "Head", "Neck",
			"Left Shoulder (Twist)", "Left Arm", "Left Forearm", "Left Hand", "Left Clavicle",
			"Right Shoulder (Twist)", "Right Arm", "Right Forearm", "Right Hand", "Right Clavicle",
			"Left Leg", "Left Calf", "Left Knee", "Left Foot",
			"Right Leg", "Right Calf", "Right Knee", "Right Foot"};

		var nodes = selected.Recurse().Where(s => s.Type == EntityType.BoneNode);
		foreach (var x in nodes)
		{
			if (x is IVisibility vis && bones.Contains(x.Name) && x is not BoneNodeGroup)
			{
				vis.Toggle();
			}
		}
	}
	public void HideAllBones() {
		var selected = this.ResolveActorEntity();
		if (selected == null)
			return;
		var nodes = selected.Recurse();
		foreach (var x in nodes)
		{
			if (x is IVisibility vis)
			{
				if (vis.Visible)
					vis.Toggle();
			}
		}
	}

	#endregion
	#region parent actorentity resolver
	private ActorEntity? ResolveActorEntity() {
		// Resolves the parent actor entity of any bone. Recursion warning.
		var selected = this._ctx.Selection.GetSelected().FirstOrDefault();
		if (selected == null)
			return null;

		ActorEntity? actor = this.Backtrack(selected, 0, 10);
		if (actor != null)
			return actor;
		return null;
	}

	private ActorEntity? Backtrack(object node, int depth = 0, int maxdepth = 0) {
		// Recursion used in ResolveActorEntity.
		if (node is ActorEntity ae)
			return ae;
		if (depth >= maxdepth)
			return null;

		var parentProperty = node.GetType().GetProperty("Parent");
		if (parentProperty == null)
			return null;

		var parent = parentProperty.GetValue(node);
		if (parent != null)
		{
			var res = Backtrack(parent, depth+1, maxdepth);
			if (res != null)
				return res;
		}
		return null;
	}

	#endregion
	private void DrawContextInspector() {

	}
}
