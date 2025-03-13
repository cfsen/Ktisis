using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Types;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Ktisis.LazyExtras.Datastructures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Diagnostics;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Actions.Types;

namespace Ktisis.LazyExtras.Components;
/*
 * REFAC TODO
 * - Get rid of direct checking for valid actor; use lazyextras.selectedactor instead.
 *		- Meaning this class should simply accept an ActorEntity as input, nothing more.
 * - There's a solid bit of redundant Linq in how transforms are done.
 *		- Streamline this and minimize resource usage
 *		- Do you even want to use linq vs. passing a list of needles and iterating? HashSet should be faster?
 * */
public class LazyPoseComponents {
	private IEditorContext ctx;

	private LazyMaths mathx;

	public Vector3 TargetLookPosition = Vector3.Zero;

	// TODO Pre-emptive implementation of an offset angle to apply when using head orientation for neutral gaze
	public float neutralEyeHeadOffset = 0.0f;

	public LazyPoseComponents(IEditorContext _ctx, LazyMaths _math) {
		this.ctx = _ctx;

		this.mathx = _math;
	}

	// Gaze control

	/// <summary>
	/// Sets the position of the currently selected entity as a gaze target
	/// </summary>
	public void SetWorldGazeTarget() {
		// Grab postion from UI selection
		if(this.ctx.Transform.Target?.GetTransform()?.Position is not Vector3 target) return;
		this.TargetLookPosition = target;
	}

	/// <summary>
	/// Resets an actors gaze to a neutral position, determined by the orientation of the head.
	/// </summary>
	public void ResetGaze(ActorEntity ae) {
		if(ae == null || ae.Pose == null) return;
		// capture state
		var epc = new EntityPoseConverter(ae.Pose);
		var initial = epc.Save();
		
		// xfm
		var children = ae.Recurse().ToList();
		if(this.GetEyesNeutral(children) is not List<Transform> eyes) return;

		this.SetEyesTransform(children, eyes[0], eyes[1]);

		// push history
		var final = epc.Save();
		HistoryAdd(MementoCreate(epc, initial, final, PoseTransforms.Rotation));
	}

	/// <summary>
	/// Sets the gaze of the current ActorEntity at the point in space set by SetWorldGazeTarget.
	/// Resolves the parent ActorEntity from any child node.
	/// </summary>
	public void SetGazeAtWorldTarget(ActorEntity ae){
		if(ae == null || ae.Pose == null) return;
		// capture state
		var epc = new EntityPoseConverter(ae.Pose);
		var initial = epc.Save();

		// xfm
		var children = ae.Recurse().ToList();
		// Use set position from SetWorldGazeTarget as target
		this.SetGaze(children, this.TargetLookPosition);

		// push history
		var final = epc.Save();
		HistoryAdd(MementoCreate(epc, initial, final, PoseTransforms.Rotation));
	}

	/// <summary>
	/// Sets the gaze of the current ActorEntity at the point in space occupied by the current camera.
	/// Resolves the parent ActorEntity from any child node.
	/// </summary>
	public void SetGazeAtCurrentCamera(ActorEntity ae){
		if(ae == null || ae.Pose == null) return;
		// capture state
		var epc = new EntityPoseConverter(ae.Pose);
		var initial = epc.Save();

		// xfm
		// Grab postion from current camera
		if(this.ctx.Cameras.Current?.GetPosition() is not Vector3 target) return;
		var children = ae.Recurse().ToList();
		this.SetGaze(children, target);

		// push history
		var final = epc.Save();
		HistoryAdd(MementoCreate(epc, initial, final, PoseTransforms.Rotation));
	}

	// Memento helpers

	private void HistoryAdd(PoseMemento pm) {
		this.ctx.Actions.History.Add(pm);
	}
	private PoseMemento MementoCreate(EntityPoseConverter epc, PoseContainer initial, PoseContainer final, PoseTransforms flags) {
		return new PoseMemento(epc) {
			Modes = PoseMode.All,
			Transforms = flags,
			Bones = null,
			Initial = initial,
			Final = final
		};
	}

	// Gaze logic 
	
	/// <summary>
	/// Sets the gaze of given ActorEntity to a supplied point in world space.
	/// </summary>
	/// <param name="ae">ActorEntity to pose</param>
	/// <param name="worldTarget">Point in world space for ActorEntity to look at</param>
	private void SetGaze(List<SceneEntity> ae, Vector3 worldTarget) {
		/*
		Using the head bone as a reference orientation, perform target and rotation
		calculations in local space. Then transform the new orientation back into world
		space. Transformations are calculated here, and set by SetEyesTransform().
		 */
		if(this.GetEyesNeutral(ae) is not List<Transform> eyes) return;
		if(this.GetHeadOrientation(ae) is not Matrix4x4 orientation) return;

		// Iterate over both eyes.
		for(int i = 0; i < 2; i++) {
			if(this.mathx.CalcWorldMatrices(eyes[i].Rotation, eyes[i].Position, out var wtd)) {
				// Local space allows starting with an identity matrix
				var id = Matrix4x4.Identity;
				var target = Vector3.Transform(worldTarget, wtd.WorldToLocal);
				var rot = this.mathx.VectorAngles(target);

				// Limit eye rotation
				this.EyesAngleClamp(ref rot);

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
	/// Limits rotation of eyes
	/// </summary>
	/// <param name="rot">Result from VectorAngles()</param>
	private void EyesAngleClamp(ref Vector3 rot) {
		//dbg($"{rot.X*180/MathF.PI} / {rot.Y*180/MathF.PI} / {rot.Z*180/MathF.PI}");
		float eyeMaxYaw = this.mathx.DegToRad(42.0f);
		float eyeMinYaw = this.mathx.DegToRad(-36.0f);
		float eyeMaxPitch = this.mathx.DegToRad(24.0f);
		float eyeMinPitch = this.mathx.DegToRad(-42.0f);
		rot.X = Math.Clamp(rot.X, eyeMinYaw, eyeMaxYaw);
		rot.Z = Math.Clamp(rot.Z, eyeMinPitch, eyeMaxPitch);
	}

	/// <summary>
	/// Sets the orientation of the eyes for an ActorEntity
	/// </summary>
	/// <param name="ae">ActorEntity to operate on</param>
	/// <param name="left">Transform for the left eye</param>
	/// <param name="right">Transform for the right eye</param>
	private void SetEyesTransform(List<SceneEntity> ae, Transform left, Transform right) {
		this.ctx.LazyExtras.fw.RunOnFrameworkThread(() => {
			// TODO memento
			if(ae.Where(x => x is BoneNode 
				&& (x.Name == "Left Eye" || x.Name == "Right Eye" || x.Name == "Left Iris" || x.Name == "Right Iris"))
				.ToList() 
				is not List<SceneEntity> eyes || eyes.Count < 1
				) return;

			foreach(SceneEntity s in eyes) {
				if((s.Name == "Left Eye" || s.Name == "Left Iris") && s is ITransform tl)
					tl.SetTransform(left);
				else if((s.Name == "Right Eye" || s.Name == "Right Iris") && s is ITransform tr)
					tr.SetTransform(right);
			}
		});
	}
	
	/// <summary>
	/// Retrives the neutral gaze Transform for eyes, given by the orientation of 'Head'
	/// </summary>
	/// <param name="ae">ActorEntity owner of 'Head'</param>
	/// <returns>List of Transform on success, null on failure.</returns>
	private List<Transform>? GetEyesNeutral(List<SceneEntity> ae) {
		if(this.GetHeadOrientation(ae) is not Matrix4x4 head) return null;
		if(this.GetEyesCurrent(ae) is not List<Transform> eyes) return null;
		// This is the groundwork for orienting the eyes to other directions
		Quaternion neutral = Quaternion.CreateFromRotationMatrix(head);
		// The head bone orientation is almost perfectly oriented for a neutral gaze, but is rotated 180* around X.
		neutral *= Quaternion.CreateFromYawPitchRoll(0.0f, MathF.PI+this.neutralEyeHeadOffset, 0.0f);
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
	private List<Transform>? GetEyesCurrent(List<SceneEntity> ae) {
		if(this.GetTransformByBoneName(ae, "Left Eye") is not Transform left) return null;
		if(this.GetTransformByBoneName(ae, "Right Eye") is not Transform right) return null;
		return [left, right];
	}

	/// <summary>
	/// Retrives the rotation matrix for the head
	/// </summary>
	/// <param name="ae">ActorEntity owner of the head</param>
	/// <returns>Matrix4x4 on success, null on failure.</returns>
	private Matrix4x4? GetHeadOrientation(List<SceneEntity> ae) { // 
		if(ae.FirstOrDefault(x => x.Name == "Head" && x is BoneNode && x is not BoneNodeGroup) is not BoneNode head) return null;
		if(head.GetTransform() is not Transform t) return null;
		return Matrix4x4.CreateFromQuaternion(t.Rotation);
	}

	/// <summary>
	/// Retrives the Transform of an ActorEntity's child BoneNode
	/// </summary>
	/// <param name="ae">ActorEntity to search</param>
	/// <param name="boneName">BoneNode.Name to search for</param>
	/// <returns>Transform on success, null on failure.</returns>
	private Transform? GetTransformByBoneName(List<SceneEntity> ae, string boneName) {
		SceneEntity? x = ae.FirstOrDefault(x => x is BoneNode && x.Name == boneName) ?? null;
		if (x == null) return null;
		if(x is not ITransform t) return null;

		return t.GetTransform();
	}

	/// <summary>
	/// Calculates the current postion of the camera in world space.
	/// </summary>
	/// <returns>Vector3 on success, null on failure.</returns>
	private Vector3? CalcCameraPosition() {
		if (this.ctx.Cameras.Current is not EditorCamera ec) return null;
		Vector3 target = Vector3.Zero;
		if (ec.FixedPosition != null)
			target += ec.FixedPosition ?? Vector3.Zero;
		else
			target += ec.GetPosition() ?? Vector3.Zero;
		//target += ec.RelativeOffset; // Verify if this should be included.
		return target;
	}

	// TODO test
	public void dbgTestGAT(ActorEntity ae) {
		HashSet<string> needles = new HashSet<string>{"Abdomen", "Waist"};
		Stopwatch dwatch = Stopwatch.StartNew();
		GetActorTransforms(ae, needles);
		dwatch.Stop();
		dbg($"time: {dwatch.ElapsedTicks} ticks");
	}
	public List<(string name, ITransform xfm)>? GetActorTransforms(ActorEntity ae, HashSet<string> needles) {
		int found = 0;
		List<(string name, ITransform xfm)> results = [];

		foreach(var child in ae.Recurse()) {
			if(needles.Contains(child.Name) && child is ITransform xfmc) {
				results.Add((child.Name, xfmc));
				found++;
			}
			if(found == needles.Count) break;
		}

		if(found != needles.Count) return null;

		return results;
	}

	// Partial reference pose loading

	/// <summary>
	/// Sets everything but the current facial expression of an actor to the reference pose.
	/// </summary>
	public void SetPartialReference() {
		// Early return if no ActorEntity or head/neck can't be selected.
		if(this.ctx.LazyExtras.ResolveActorEntity() is not ActorEntity selected) return;
		if(selected.Recurse()
			.FirstOrDefault(x => x.Name == "Head" && x is BoneNodeGroup)
			is not SceneEntity neck) return;
		if(selected.Recurse()
			.FirstOrDefault(x => x.Name == "Head" && x is not BoneNodeGroup)
			is not SceneEntity head) return;

		this.ctx.LazyExtras.fw.RunOnFrameworkThread(() => {
			// Save current and set reference pose
			var epc = new EntityPoseConverter(selected.Pose!);
			var org = epc.Save();
			epc.LoadReferencePose();
			var fin = epc.Save();

			// Load the original expression by loading bones from the head BoneNodeGroup 
			this.ctx.Selection.Select(neck);
			var gsb = epc.GetSelectedBones(false).ToList();
			epc.LoadSelectedBones(org, PoseTransforms.Position | PoseTransforms.Rotation);

			// Set the the neck bones back to reference pose 
			// Note: Passing both flags at once does not produce the same result
			epc.LoadBones(fin, gsb, PoseTransforms.Position);
			epc.LoadBones(fin, gsb, PoseTransforms.Rotation);

			// Rotate the head back into position
			// Change selection to head bone and rotate it to reference pose
			this.ctx.Selection.Select(head);
			gsb = epc.GetSelectedBones(false).ToList();
			epc.LoadBones(fin, gsb, PoseTransforms.Rotation);
		});
	}

	// Debug

	private static void dbg(string s) => Ktisis.Log.Debug($"LazyPoseComp: {s}"); 

}
