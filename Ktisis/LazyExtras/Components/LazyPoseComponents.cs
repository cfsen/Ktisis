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

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	
	/// <summary>
	/// Sets the gaze of given ActorEntity to a supplied point in world space.
	/// </summary>
	/// <param name="ae">ActorEntity to pose</param>
	/// <param name="worldTarget">Point in world space for ActorEntity to look at</param>
	private void SetGaze(ActorEntity ae, Vector3 worldTarget) {
		/*
		Using the head bone as a reference orientation, perform target and rotation
		calculations in local space. Then transform the new orientation back into world
		space. Transformations are calculated here, and set by SetEyesTransform().
		 */
		if(this.GetEyesNeutral(ae) is not List<Transform> eyes) return;
		if(this.GetHeadOrientation(ae) is not Matrix4x4 orientation) return;

		// Iterate over both eyes.
		for(int i = 0; i < 2; i++) {
			if(this.CalcWorldMatrices(eyes[i].Rotation, eyes[i].Position, out var wtd)) {
				// Local space allows starting with an identity matrix
				var id = Matrix4x4.Identity;
				var target = Vector3.Transform(worldTarget, wtd.WorldToLocal);
				var rot = this.VectorAngles(target);

				// TODO this is where any offsets would be added to yaw/pitch
				// Yaw then pitch in local space. This prevents eyes rolling.
				id *= Matrix4x4.CreateRotationY(rot.X);
				id *= Matrix4x4.CreateRotationZ(rot.Z);

				// Transform into world space orientation
				id *= wtd.LocalToWorld_Rotation;
				eyes[i].Rotation = Quaternion.CreateFromRotationMatrix(id);
			}
		}

		// Set the new transform.
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
			if((s.Name == "Left Eye" || s.Name == "Left Iris") && s is ITransform tl)
				tl.SetTransform(left);
			else if((s.Name == "Right Eye" || s.Name == "Right Iris") && s is ITransform tr)
				tr.SetTransform(right);
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
		return [left, right];
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
		SceneEntity? x = ae.Recurse()
			.Where(x => x is BoneNode && x.Name == boneName)?
			.FirstOrDefault() ?? null;
		if (x == null) return null;
		if(x is not ITransform t) return null;

		return t.GetTransform();
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

	/// <summary>
	/// Generates world-local and inverse transformation matrices and populates `result`.
	/// </summary>
	/// <param name="q">Orientation of local space</param>
	/// <param name="pos">Position of local space in world space</param>
	/// <param name="result">Struct housing matrices</param>
	/// <returns>true on success, false if matrices cannot be inverted.</returns>
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

	/// <summary>
	/// Toggles overlay visibility of essential gesture bones
	/// </summary>
	public void ToggleGestureBones() {
		if (this.ResolveActorEntity() is not ActorEntity selected) return;

		var bones = new List<string>(){"Abdomen", "Waist", "Lumbar", "Thoracic", "Cervical", "Head", "Neck",
			"Left Shoulder (Twist)", "Left Arm", "Left Forearm", "Left Hand", "Left Clavicle",
			"Right Shoulder (Twist)", "Right Arm", "Right Forearm", "Right Hand", "Right Clavicle",
			"Left Leg", "Left Calf", "Left Knee", "Left Foot",
			"Right Leg", "Right Calf", "Right Knee", "Right Foot"};

		this.ToggleBones(selected, bones);
	}
	
	/// <summary>
	/// Toggles extra bones for gestures
	/// </summary>
	public void ToggleGestureDetailBones() {
		if (this.ResolveActorEntity() is not ActorEntity selected) return;

		var bones = new List<string>() { "Left Toes", "Right Toes", "Left Wrist", "Right Wrist" };

		this.ToggleBones(selected, bones);
	}

	/// <summary>
	/// Toggles the overlay visibility of bones in supplied list 
	/// </summary>
	/// <param name="selected">ActorEntity to toggle overlay for</param>
	/// <param name="bones">Names of bones to toggle</param>
	private void ToggleBones(ActorEntity selected, List<string> bones) {
		var nodes = selected.Recurse().Where(s => s.Type == EntityType.BoneNode);
		foreach (var x in nodes) {
			if (x is IVisibility vis && bones.Contains(x.Name) && x is not BoneNodeGroup) {
				if(x.Parent is EntityPose) continue; // Skip toggling weapon
				vis.Toggle();
			}
		}
	}

	/// <summary>
	/// Turns off visibility for all overlay bones for the currently selected actor
	/// </summary>
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

	/// <summary>
	/// Sets everything but the current facial expression of an actor to the reference pose.
	/// </summary>
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

	/// <summary>
	/// Backtracks current selection in order to find the parent ActorEntity. Max depth 10. 
	/// </summary>
	/// <returns>Selected ActorEntity on success, null on failure.</returns>
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

	/// <summary>
	/// Recursive function for ResolveActorEntity()
	/// </summary>
	/// <param name="node">Current node</param>
	/// <param name="depth">Current depth</param>
	/// <param name="maxdepth">Max depth</param>
	/// <returns>ActorEntity on success, null on failure.</returns>
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
	//public void SelectEyeBall() {
	//	var actor = this._ctx.Scene.Recurse().Where(x => x is ActorEntity).FirstOrDefault();
	//	if(actor == null) return;
	//	var eye = actor.Recurse().Where(x => x.Name == "Left Eye" && x is BoneNode).FirstOrDefault();
	//	if(eye == null) return;
	//	this._ctx.Selection.Select(eye);
	//}

	// M4 helpers

	// // TODO saved for future debug
	//public void dbgCsM4() {
	//	this.DrawM4Table(this._dbgM4, "FIRST");
	//	//ImGui.Text(this.dbgV3(this._dbgM4.Translation));
	//	this.DrawM4Table(this._dbgM4_2, "SECOND");
	//	//ImGui.Text(this.dbgV3(this._dbgM4_2.Translation));
	//	this.DrawM4Table(this._dbgM4_3, "THIRD");
	//	//ImGui.Text(this.dbgV3(this._dbgM4_3.Translation));
	//	this.DrawM4Table(this._dbgM4_2, "FOURTH");
	//}

	//// Saved for future debug
	//public void dbgMatrixInspector(string bone) {
	//	if(this.ResolveActorEntity() is not ActorEntity ae) return;
	//	if(ae.Recurse().Where(x => x.Name == bone && x is BoneNode).FirstOrDefault() is not BoneNode bn) return;
	//	var m = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(bn.GetTransform()?.Rotation ?? Quaternion.Identity));
	//	//dp(m[0,0].ToString());
	//	//dp(m.ToString());
	//	//dp("Okay");
	//	this.DrawM4Table(m, bn.Name);
	//	Matrix4x4 mi = new Matrix4x4();
	//	if(Matrix4x4.Invert(m, out mi)) {
	//		this.DrawM4Table(mi, bn.Name + "(inv)");
	//		this.DrawM4Table(Matrix4x4.Multiply(m, mi), "A*A^-1");
	//	}

	//}

	//// Saved for future debug
	//private void DrawM4Table(Matrix4x4 m, string lbl) {
	//	ImGui.Text(lbl);
	//	if(ImGui.BeginTable($"{lbl}###{lbl}", 4)) {
	//		int i = 0, r = 0, c = 0;
	//		for(i = 0; i < 16; i++) {
	//			ImGui.TableNextColumn();
	//			ImGui.Text(m[r,c].ToString());
	//			if(c == 3) { r++; c=0; }
	//			else c++;
	//		}
	//		ImGui.EndTable();
	//	}
	//}

	// Support functions

	/// <summary>
	/// Determines angles of a vector in 3 planes.
	/// </summary>
	/// <param name="u">Vector to deconstruct</param>
	/// <param name="degrees">Return result in degrees</param>
	/// <returns>Vector3 containing angles.</returns>
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
}
