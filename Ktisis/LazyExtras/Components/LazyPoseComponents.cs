using FFXIVClientStructs.FFXIV.Client.UI;

using ImGuiNET;

using JetBrains.Annotations;

using Ktisis.Common.Utility;
using Ktisis.Data.Json;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;
using Ktisis.Structs.Lights;

using Lumina.Excel.Sheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;

public struct WorldTransformData {
	public Matrix4x4 LocalToWorld { get; init; }
	public Matrix4x4 LocalToWorld_Position { get; init; }
	public Matrix4x4 LocalToWorld_Rotation { get; init; }

	public Matrix4x4 WorldToLocal { get; init; }
	public Matrix4x4 WorldToLocal_Position { get; init; }
	public Matrix4x4 WorldToLocal_Rotation { get; init; }
}


public class LazyPoseComponents {
	private readonly IEditorContext _ctx;
	public Vector3 TargetLookPosition = Vector3.Zero;

	private Matrix4x4 _dbgM4 = Matrix4x4.Identity;
	private Matrix4x4 _dbgM4_2 = Matrix4x4.Identity;
	private Matrix4x4 _dbgM4_3 = Matrix4x4.Identity;
	private Matrix4x4 _dbgM4_4 = Matrix4x4.Identity;

	// Pre-emptive implementation of an offset angle to apply when using head orientation for neutral gaze
	private float _neutralEyeHeadOffset = 0.0f;

	public LazyPoseComponents(IEditorContext ctx) {
		this._ctx = ctx;
	}

	// Gaze control

	/// <summary>
	/// Sets the position of the currently selected entity as a gaze target
	/// </summary>
	public void SetWorldGazeTarget() {
		// Grab postion from UI selection
		if(this._ctx.Transform.Target?.GetTransform()?.Position is not Vector3 target) return;
		this.TargetLookPosition = target;
	}

	/// <summary>
	/// Resets an actors gaze to a neutral position, determined by the orientation of the head.
	/// </summary>
	public void ResetGaze() {
		if(this.ResolveActorEntity() is not ActorEntity ae) return;
		if(this.GetEyesNeutral(ae) is not List<Transform> eyes) return;
		this.SetEyesTransform(ae, eyes[0], eyes[1]);
	}

	/// <summary>
	/// Sets the gaze of the current ActorEntity at the point in space set by SetWorldGazeTarget.
	/// Resolves the parent ActorEntity from any child node.
	/// </summary>
	public void SetGazeAtWorldTarget(){
		if(this.ResolveActorEntity() is not ActorEntity ae) return;	
		// Use set position from SetWorldGazeTarget as target
		this.SetGaze(ae, this.TargetLookPosition);
	}

	/// <summary>
	/// Sets the gaze of the current ActorEntity at the point in space occupied by the current camera.
	/// Resolves the parent ActorEntity from any child node.
	/// </summary>
	public void SetGazeAtCurrentCamera(){
		if(this.ResolveActorEntity() is not ActorEntity ae) return;	
		// Grab postion from current camera
		if(this._ctx.Cameras.Current?.GetPosition() is not Vector3 target) return;
		this.SetGaze(ae, target);
	}

	// Gaze logic 
	
	private void SetGaze(ActorEntity ae, Vector3 worldTarget) {
		if(this.GetEyesNeutral(ae) is not List<Transform> eyes) return;
		if(this.GetHeadOrientation(ae) is not Matrix4x4 orientation) return;

		for(int i = 0; i < 2; i++) {
			if(this.CalcWorldMatrices(eyes[i].Rotation, eyes[i].Position, out var wtd)) {
				var id = Matrix4x4.Identity;
				var target = Vector3.Transform(worldTarget, wtd.WorldToLocal);
				var rot = this.VectorAngles(target);
				id *= Matrix4x4.CreateFromYawPitchRoll(rot.X, 0.0f, rot.Z);
				id *= wtd.LocalToWorld_Rotation;
				eyes[i].Rotation = Quaternion.CreateFromRotationMatrix(id);
				// TODO this induces a slight roll around X on the eyes
				// I have a fix for this in the deprecated code
				// Might want to bake that into this, and expose it as an option
				// Since it does make the gaze less accurate due to an offset
			}
		}
		this.SetEyesTransform(ae, eyes[0], eyes[1]);
	} 

	/// <summary>
	/// Sets the orientation of the eyes for an ActorEntity
	/// </summary>
	/// <param name="ae">ActorEntity to operate on</param>
	/// <param name="left">Transform for the left eye</param>
	/// <param name="right">Transform for the right eye</param>
	private void SetEyesTransform(ActorEntity ae, Transform left, Transform right) {
		if(ae.Recurse()
			.Where(x => x is BoneNode && (x.Name == "Left Eye" || x.Name == "Right Eye" || x.Name == "Left Iris" || x.Name == "Right Iris"))
			.ToList() is not List<SceneEntity> eyes || eyes.Count < 1
			) return;
		foreach(SceneEntity s in eyes) {
			this._ctx.Selection.Select(s);
			if(s.Name == "Left Eye" || s.Name == "Left Iris")
				this.SetTransform(this._ctx.Transform.Target!, left);	// null handled in function
			else if(s.Name == "Right Eye" || s.Name == "Right Iris")
				this.SetTransform(this._ctx.Transform.Target!, right);
		}
	}
	
	/// <summary>
	/// Retrives the neutral gaze Transform for eyes, given by the orientation of 'Head'
	/// </summary>
	/// <param name="ae">ActorEntity owner of 'Head'</param>
	/// <returns>List of Transform on success, null on failure.</returns>
	private List<Transform>? GetEyesNeutral(ActorEntity ae) {
		if(this.GetHeadOrientation(ae) is not Matrix4x4 head) return null;
		if(this.GetEyesCurrent(ae) is not List<Transform> eyes) return null;
		// This is the groundwork for orienting the eyes to other directions
		Quaternion neutral = Quaternion.CreateFromRotationMatrix(head);
		// The head bone orientation is almost perfectly oriented for a neutral gaze, but is rotated 180* around X.
		neutral *= Quaternion.CreateFromYawPitchRoll(0.0f, MathF.PI+this._neutralEyeHeadOffset, 0.0f);
		// The eyes are now neutral. Any orientation needs to happen after this.
		foreach(Transform eye in eyes) 
			eye.Rotation = neutral;
		return eyes;
	}

	/// <summary>
	/// Retrieves the current Transform for 'Left Eye' and 'Right Eye'
	/// </summary>
	/// <param name="ae">ActorEntity owner of eyes</param>
	/// <returns>List of Transform on success (Left at idx=0), null on failure.</returns>
	private List<Transform>? GetEyesCurrent(ActorEntity ae) {
		if(this.GetTransformByBoneName(ae, "Left Eye") is not Transform left) return null;
		if(this.GetTransformByBoneName(ae, "Right Eye") is not Transform right) return null;
		List<Transform> l = new List<Transform>();
		l.Add(left);
		l.Add(right);
		return l;
	}

	/// <summary>
	/// Retrives the rotation matrix for the head
	/// </summary>
	/// <param name="ae">ActorEntity owner of the head</param>
	/// <returns>Matrix4x4 on success, null on failure.</returns>
	private Matrix4x4? GetHeadOrientation(ActorEntity ae) { // 
		if(ae.Recurse().Where(x => x.Name == "Head" && x is BoneNode && x is not BoneNodeGroup).FirstOrDefault() is not BoneNode head) return null;
		if(head.GetTransform() is not Transform t) return null;
		return Matrix4x4.CreateFromQuaternion(t.Rotation);
	}

	/// <summary>
	/// Retrives the Transform of an ActorEntity's child BoneNode
	/// </summary>
	/// <param name="ae">ActorEntity to search</param>
	/// <param name="boneName">BoneNode.Name to search for</param>
	/// <returns>Transform on success, null on failure.</returns>
	private Transform? GetTransformByBoneName(ActorEntity ae, string boneName) {
		SceneEntity? x = ae.Recurse().Where(x => x is BoneNode && x.Name == boneName)?.FirstOrDefault() ?? null;
		if (x == null) return null;

		this._ctx.Selection.Select(x);

		if (this._ctx.Transform.Target?.GetTransform() is not Transform t) return null;
		return t;
	}

	/// <summary>
	/// Sets the transform of the currently selected target. This is cursed, but hey. It works.
	/// </summary>
	/// <param name="target">ctx.Transform.Target</param>
	/// <param name="transform">Transform to set.</param>
	private void SetTransform(ITransformTarget target, Transform transform) {
		if (target == null) return;
		target.SetTransform(transform);
	}

	/// <summary>
	/// Calculates the current postion of the camera in world space.
	/// </summary>
	/// <returns>Vector3 on success, null on failure.</returns>
	private Vector3? CalcCameraPosition() {
		if (this._ctx.Cameras.Current is not EditorCamera ec) return null;
		Vector3 target = Vector3.Zero;
		if (ec.FixedPosition != null)
			target += ec.FixedPosition ?? Vector3.Zero;
		else
			target += ec.GetPosition() ?? Vector3.Zero;
		//target += ec.RelativeOffset; // Verify if this should be included.
		return target;
	}

	// Local/World space logic

	private bool CalcWorldMatrices(Quaternion q, Vector3 pos, out WorldTransformData result) {
		Matrix4x4 m = Matrix4x4.CreateFromQuaternion(q);
		Matrix4x4 mp = Matrix4x4.CreateTranslation(pos);
		if(!Matrix4x4.Invert(m, out Matrix4x4 m_inv)) {
			dp("Could not compute m_inv!");
			result = default;
			return false;
		}
		if(!Matrix4x4.Invert(mp, out Matrix4x4 mp_inv)) {
			dp("Could not compute mp_inv!");
			result = default;
			return false;
		}
		// mp * m // Local to world
		// m_inv * mp_inv // World to local

		result = new WorldTransformData {
			LocalToWorld = mp * m,
			LocalToWorld_Position = mp,
			LocalToWorld_Rotation = m,

			//WorldToLocal = m_inv * mp_inv,
			WorldToLocal = mp_inv * m_inv,
			WorldToLocal_Position = mp_inv,
			WorldToLocal_Rotation = m_inv
		};

		//return m_inv *= mp_inv;
		return true;
	}

	// Overlay visibility 

	public void ToggleGestureBones() {
		if (this.ResolveActorEntity() is not ActorEntity selected) return;

		var bones = new List<string>(){"Abdomen", "Waist", "Lumbar", "Thoracic", "Cervical", "Head", "Neck",
			"Left Shoulder (Twist)", "Left Arm", "Left Forearm", "Left Hand", "Left Clavicle",
			"Right Shoulder (Twist)", "Right Arm", "Right Forearm", "Right Hand", "Right Clavicle",
			"Left Leg", "Left Calf", "Left Knee", "Left Foot",
			"Right Leg", "Right Calf", "Right Knee", "Right Foot"};

		this.ToggleBones(selected, bones);
	}

	public void ToggleGestureDetailBones() {
		if (this.ResolveActorEntity() is not ActorEntity selected) return;

		var bones = new List<string>() { "Left Toes", "Right Toes", "Left Wrist", "Right Wrist" };

		this.ToggleBones(selected, bones);
	}

	private void ToggleBones(ActorEntity selected, List<string> bones) {
		var nodes = selected.Recurse().Where(s => s.Type == EntityType.BoneNode);
		foreach (var x in nodes) {
			if (x is IVisibility vis && bones.Contains(x.Name) && x is not BoneNodeGroup) {
				if(x.Parent is EntityPose) continue; // Skip toggling weapon
				vis.Toggle();
			}
		}
	}

	public void HideAllBones() {
		var selected = this.ResolveActorEntity();
		if (selected == null)
			return;
		var nodes = selected.Recurse();
		foreach (var x in nodes) {
			if (x is IVisibility vis) {
				if (vis.Visible)
					vis.Toggle();
			}
		}
	}

	// Partial reference pose loading

	public void SetPartialReference() {
		// Early return if no ActorEntity or head/neck can't be selected.
		if(this.ResolveActorEntity() is not ActorEntity selected) return;
		if(selected.Recurse()
			.Where(x => x.Name == "Head" && x is BoneNodeGroup)
			.FirstOrDefault() is not SceneEntity neck) return;
		if(selected.Recurse()
			.Where(x => x.Name == "Head" && x is not BoneNodeGroup)
			.FirstOrDefault() is not SceneEntity head) return;

		// Save current and set reference pose
		var epc = new EntityPoseConverter(selected.Pose!);
		var org = epc.Save();
		epc.LoadReferencePose();
		var fin = epc.Save();

		// Load the original expression by loading bones from the head BoneNodeGroup 
		this._ctx.Selection.Select(neck);
		var gsb = epc.GetSelectedBones(false).ToList();
		epc.LoadSelectedBones(org, PoseTransforms.Position | PoseTransforms.Rotation);

		// Set the the neck bones back to reference pose 
		// Note: Passing both flags at once does not produce the same result
		epc.LoadBones(fin, gsb, PoseTransforms.Position);
		epc.LoadBones(fin, gsb, PoseTransforms.Rotation);

		// Rotate the head back into position
		// Change selection to head bone and rotate it to reference pose
		this._ctx.Selection.Select(head);
		gsb = epc.GetSelectedBones(false).ToList();
		epc.LoadBones(fin, gsb, PoseTransforms.Rotation);
	}

	// Target resolving

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
	
	// Debug

	private static void dp(string s) => Ktisis.Log.Debug(s); 
	public void SelectEyeBall() {
		var actor = this._ctx.Scene.Recurse().Where(x => x is ActorEntity).FirstOrDefault();
		if(actor == null) return;
		var eye = actor.Recurse().Where(x => x.Name == "Left Eye" && x is BoneNode).FirstOrDefault();
		if(eye == null) return;
		this._ctx.Selection.Select(eye);
	}

	// M4 helpers

	public void dbgCsM4() {
		this.DrawM4Table(this._dbgM4, "FIRST");
		//ImGui.Text(this.dbgV3(this._dbgM4.Translation));
		this.DrawM4Table(this._dbgM4_2, "SECOND");
		//ImGui.Text(this.dbgV3(this._dbgM4_2.Translation));
		this.DrawM4Table(this._dbgM4_3, "THIRD");
		//ImGui.Text(this.dbgV3(this._dbgM4_3.Translation));
		this.DrawM4Table(this._dbgM4_2, "FOURTH");
	}

	public void dbgMatrixInspector(string bone) {
		if(this.ResolveActorEntity() is not ActorEntity ae) return;
		if(ae.Recurse().Where(x => x.Name == bone && x is BoneNode).FirstOrDefault() is not BoneNode bn) return;
		var m = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(bn.GetTransform()?.Rotation ?? Quaternion.Identity));
		//dp(m[0,0].ToString());
		//dp(m.ToString());
		//dp("Okay");
		this.DrawM4Table(m, bn.Name);
		Matrix4x4 mi = new Matrix4x4();
		if(Matrix4x4.Invert(m, out mi)) {
			this.DrawM4Table(mi, bn.Name + "(inv)");
			this.DrawM4Table(Matrix4x4.Multiply(m, mi), "A*A^-1");
		}

	}

	private void DrawM4Table(Matrix4x4 m, string lbl) {
		ImGui.Text(lbl);
		if(ImGui.BeginTable($"{lbl}###{lbl}", 4)) {
			int i = 0, r = 0, c = 0;
			for(i = 0; i < 16; i++) {
				ImGui.TableNextColumn();
				ImGui.Text(m[r,c].ToString());
				if(c == 3) { r++; c=0; }
				else c++;
			}
			ImGui.EndTable();
		}
	}

	// Support functions

	private Vector3 VectorAngles(Vector3 u, bool degrees = false) {
		Vector3 len = new() {
			X = MathF.Max(MathF.Sqrt(u.X * u.X + u.Z * u.Z), float.Epsilon),
			Y = MathF.Max(MathF.Sqrt(u.Y * u.Y + u.Z * u.Z), float.Epsilon),
			Z = MathF.Max(MathF.Sqrt(u.X * u.X + u.Y * u.Y), float.Epsilon)
		};

		Vector3 s = new() {X=MathF.Sign(u.X), Y=MathF.Sign(u.Y), Z=MathF.Sign(u.Z)};

		//dp("u=" + u.ToString());
		//dp("s= " + s.ToString());

		// Determine which quadrant is being targeted
		int[] quad = [0,0,0,0];	// Quad 1,2,3,4

		if(s.Y >= 0 && s.Z >= 0)		quad[0] = quad[0] ^ 1;	// Quad 1
		else if(s.Y >= 0 && s.Z < 0)	quad[1] = quad[1] ^ 1;	// Quad 2
		else if(s.Y < 0 && s.Z < 0)		quad[2] = quad[2] ^ 1;	// Quad 3
		else if(s.Y < 0 && s.Z >= 0)	quad[3] = quad[3] ^ 1;	// Quad 4

		// All X<0 are invalid, TODO this should be handled better
		if(s.X < 0) quad = [0,0,0,0];

		//dp($"quad: [{string.Join(", ", quad)}]");

		// Initial angle calc
		Vector3 o = new() {
			X = MathF.Acos(u.X / len.X),	// XZ-plane maps to rotation around Y axis, yaw = cos(x) || sin(z)
			Y = MathF.Acos(u.Z / len.Y),	// YZ-plane maps to rotation around X axis, pitch = cos(z) || sin(y)
			Z = MathF.Acos(u.X / len.Z)		// XY-plane mapts to rotation around Z axis, roll = cos(x) || sin(y)
		};

		// Adjust for quad
		if(quad[0] == 1 || quad[3] == 1) {
			o.X = -o.X;
		}
		if(quad[3] == 1 || quad[2] == 1) {
			o.Z = -o.Z;
		}

		// TODO angle clamping here

		// Conversion
		if (degrees) {
			o.X = o.X * 180 / MathF.PI;
			o.Y = o.Y * 180 / MathF.PI;
			o.Z = o.Z * 180 / MathF.PI;
		}

		return o;
	}
	private double RadToDeg(double rad) => this.RadToDeg((float)rad);
	private double DegToRad(double deg) => this.DegToRad((float)deg);
	private double RadToDeg(float rad) {
		return rad * 180 / Math.PI;
	}
	private float DegToRad(float deg) {
		return (float)(deg * Math.PI / 180);
	}

	// #############
	// # Deprecated begins
	// #############
	//private void SetGazeToPosition(Vector3? targetOverride = null) {
	//	if (this._ctx.Transform.Target == null || this._ctx.Cameras.Current == null)
	//		return;

	//	if (this.CalcCameraPosition() is not Vector3 currentCam) return;
	//	currentCam += this._ctx.Cameras.Current?.RelativeOffset ?? Vector3.Zero;

	//	// Override for non-camera target
	//	if (targetOverride != null)
	//		currentCam = targetOverride.Value;

	//	// Fetch current state of target
	//	var target = this._ctx.Transform.Target;
	//	Transform selectedBone = target?.GetTransform() ?? new Transform();

	//	// Buffer changes 
	//	Transform tmp = selectedBone;
	//	tmp.Rotation = this.CalcGazeToPosition(selectedBone, currentCam, targetOverride != null);

	//	// Update transform
	//	target?.SetTransform(tmp);
	//}
	//public void LookAtCamera(Vector3? targetOverride = null) {
	//	// Store state of UI selection
	//	var lastSelected = this._ctx.Selection.GetSelected().FirstOrDefault();

	//	// Recurse and find parent ActorEntity
	//	var selected = this.ResolveActorEntity();
	//	if (selected == null)
	//		return;

	//	// Recurses through the actor to find the eyes.
	//	// TODO This is localization dependant
	//	// TODO recursing through the entire actor is redundant
	//	foreach (SceneEntity s in selected.Recurse().Where(s => s.Type == EntityType.BoneNode))	{
	//		if (s.Name == "Left Eye" || s.Name == "Right Eye") {
	//			this._ctx.Selection.Select(s);
	//			this.SetGazeToPosition(targetOverride);
	//		}
	//	}

	//	// Return state of selected UI element
	//	if (lastSelected != null)
	//		this._ctx.Selection.Select(lastSelected);
	//}
	//private Quaternion CalcGazeToPosition(Transform eye, Vector3 targetPosition, bool targetOverride) {
	//	//return this.CalcGazeToPosition2(eye, targetPosition, targetOverride) ?? Quaternion.Identity;
	//	// TODO This kinda works most of the time, but it's hacky.
		
	//	// Create a billboard for initial orientation 
	//	Matrix4x4 billboard = Matrix4x4.CreateBillboard(eye.Position, targetPosition, Vector3.UnitY, Vector3.UnitX);
	//	// Correct rotation around Y axis
	//	Matrix4x4 yflip = Matrix4x4.CreateRotationY(this.DegToRad(90.0f));
	//	billboard = Matrix4x4.Multiply(yflip, billboard);

	//	// Reorients any X-rotation that might have happened. 
	//	Matrix4x4 xflip = Matrix4x4.CreateRotationX(this.DegToRad(HkaEulerAngles.ToEuler(eye.Rotation).X));
	//	billboard = Matrix4x4.Multiply(xflip, billboard);

	//	return Quaternion.CreateFromRotationMatrix(billboard);
	//}
	//private Quaternion? CalcGazeToPosition2(Transform eye, Vector3 targetPosition, bool targetOverride, float maxRotationAngle = 99.0f) {
	//	/*
		 
	//	You need to be able to transform the eye quaternion back to an identity state
	//	Then back to it's intended location with a new rotation

	//	Meaning:
	//	p = position vector
	//	r = rotation matrix from quaternion
	//	Pmi = Position matrix inverse
	//	Pm = Position matrix
	//	Rmi = Rotation matrix inverse
	//	Rm = Rotationmatrix

	//	--- Set eyes to neutral, forward facing ---
	//	Means needing some way of determining what a neutral rotation (looking straight forward) looks like.
	//	You need to look at the orientation of a bone which has a static position in the eyes local space
	//	The head seems like a good candidate
	//		>>> Its orientation appears to be transferrable without any additional work
		

	//	Steps:
	//	p * Pmi -> Brings p to origin
	//	r * Rmi -> Brings r to identity
	//	--- you can do local space rotations now, on anything that's been brought into local space ---
	//		r * (arbitrary rotation) || (r*Rm for target)
	//	p * Pm -> reverses move to origin


	//	 */
	//	dp("##########");
	//	// Build matrix for world->local space
	//	Matrix4x4 m = Matrix4x4.CreateFromQuaternion(eye.Rotation);
	//	this._dbgM4 = m;
	//	//Matrix4x4 m = Matrix4x4.Identity;
	//	m *= Matrix4x4.CreateTranslation(eye.Position);
	//	//m *= Matrix4x4.CreateTranslation(-eye.Position);
	//	//m *= Matrix4x4.CreateFromQuaternion(eye.Rotation);
	//	this._dbgM4_2 = m;

	//	dp("eye.Pos: " + eye.Position.ToString());
	//	dp("targetPosition: " + targetPosition.ToString());
	//	dp("targetPosition*m: " + Vector3.Transform(targetPosition, m).ToString());
	//	//dp(Vector3.Transform(targetPosition, m).ToString());

	//	Matrix4x4 eyeWorldM4 = Matrix4x4.CreateFromQuaternion(eye.Rotation);

	//	if (!Matrix4x4.Invert(eyeWorldM4, out Matrix4x4 eyeWorldM4Inv)) return null;
	//	if (!Matrix4x4.Invert(m, out Matrix4x4 mi)) return null;

	//	dp("targetPosition*m*mi: " + Vector3.Transform(Vector3.Transform(targetPosition, m), mi).ToString());
	//	dp("eye.Position*mi: " + Vector3.Transform(eye.Position, mi).ToString()); // <<< THIS IS THE SANITY CHECK, you must be able to "return" the eye to origo

	//	Vector3 u = Vector3.Transform(targetPosition, m);
	//	float yaw = MathF.Atan2(u.X, u.Z);
	//	float pitch = MathF.Atan2(u.Z, u.Y);

	//	dp("yaw: " + yaw.ToString() + "|" + this.RadToDeg(yaw).ToString());
	//	dp("pitch: " + pitch.ToString() +" | "+ this.RadToDeg(pitch).ToString());



	//	//Vector3 v = targetPosition - eye.Position;
	//	//Vector3 vl = Vector3.Transform(v, eyeWorldM4);
	//	//dp(Vector3.Transform(vl, m).ToString());


	//	/*
		 
	//	Starting from an identity works as intended, the rotation done on localTransform is as desired.
	//	Bringing it back into world space works as intended.

	//	The problem is getting the angles to the point in world space in the first place. 

	//	This implementation doesn't really work.

	//	Need to verify that:
	//	- The point in local space is as intended
	//	- The approach to calculating the angle is correct.
		 
	//	 */

	//	float xzlen = u.X * u.X + u.Z * u.Z;
	//	float yzlen = u.Y * u.Y + u.Z * u.Z;
	//	float xylen = u.X * u.X + u.Y * u.Y;
	//	//float yaw = u.X / xzlen; // XZ-plane maps to rotation around Y axis, yaw = cos(x) || sin(z)
	//	//float pitch = u.Z / yzlen; // YZ-plane maps to rotation around X axis, pitch = cos(z) || sin(y)
	//	//float roll = u.Y / xylen; // XY-plane mapts to rotation around Z axis, roll = cos(x) || sin(y)

	//	//dp("Yaw: " + yaw.ToString() + " | deg:" + this.RadToDeg(yaw).ToString());
	//	//dp("Pitch: " + pitch.ToString() + " | deg:" + this.RadToDeg(pitch).ToString());
	//	dp("^^^^^^^^^");

	//	return eye.Rotation;
	//	Matrix4x4 localTransform = Matrix4x4.Identity;
	//	//localTransform = Matrix4x4.Multiply(localTransform, Matrix4x4.CreateFromYawPitchRoll(MathF.PI/16, 0.0f, MathF.PI/16));
	//	localTransform = Matrix4x4.Multiply(localTransform, Matrix4x4.CreateFromYawPitchRoll(yaw, 0.0f, pitch)); // Intentional placement of yaw/pitch
	//	//this._dbgM4 = localTransform;
	//	Quaternion qr = Quaternion.CreateFromRotationMatrix(Matrix4x4.Multiply(localTransform, eyeWorldM4)); // Bring local transforms into world space
	//	return Quaternion.Normalize(qr);
	//}
	// #############
	// # Deprecated ends
	// #############
}
