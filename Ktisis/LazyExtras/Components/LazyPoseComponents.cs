using FFXIVClientStructs.FFXIV.Client.UI;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
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

	// Gaze control
	public void SetTarget() {
		// targets the current selection for gazing
		if (this._ctx.Transform.Target == null) 
			return;
		this.TargetLookPosition = this._ctx.Transform.Target.GetTransform()?.Position ?? Vector3.Zero;
	}

	private void SetGazeToPosition(Vector3? targetOverride = null) {
		// TODO refactor name
		if (this._ctx.Transform.Target == null || this._ctx.Cameras.Current == null)
			return;

		// TODO ViewMatrix for more accurate gaze
		Vector3 currentCam;
		if (this._ctx.Cameras.Current?.FixedPosition != null) {
			currentCam = this._ctx.Cameras.Current?.FixedPosition ?? Vector3.Zero;
			currentCam += this._ctx.Cameras.Current?.RelativeOffset ?? Vector3.Zero;
		} 
		else {
			currentCam = this._ctx.Cameras.Current?.GetPosition() ?? Vector3.Zero;
			currentCam += this._ctx.Cameras.Current?.RelativeOffset ?? Vector3.Zero;
		}

		// Override for non-camera target
		if (targetOverride != null)
			currentCam = targetOverride.Value;

		// Fetch current state of target
		var target = this._ctx.Transform.Target;
		Transform selectedBone = target?.GetTransform() ?? new Transform();
		
		// Buffer changes 
		Transform tmp = selectedBone;
		tmp.Rotation = this.CalcGazeToPosition(selectedBone, currentCam);

		// Update transform
		target?.SetTransform(tmp);
	}

	public void LookAtCamera(Vector3? targetOverride = null) {
		// Store state of UI selection
		var lastSelected = this._ctx.Selection.GetSelected().FirstOrDefault();

		// Recurse and find parent ActorEntity
		var selected = this.ResolveActorEntity();
		if (selected == null)
			return;

		// Recurses through the actor to find the eyes.
		// TODO This is localization dependant
		// TODO recursing through the entire actor is redundant
		foreach (SceneEntity s in selected.Recurse().Where(s => s.Type == EntityType.BoneNode))	{
			if (s.Name == "Left Eye" || s.Name == "Right Eye") {
				this._ctx.Selection.Select(s);
				this.SetGazeToPosition(targetOverride);
			}
		}

		// Return state of selected UI element
		if (lastSelected != null)
			this._ctx.Selection.Select(lastSelected);
	}

	private Quaternion CalcGazeToPosition(Transform eye, Vector3 targetPosition) {
		// TODO This kinda works most of the time, but it's hacky.

		// Create a billboard for initial orientation 
		Matrix4x4 billboard = Matrix4x4.CreateBillboard(eye.Position, targetPosition, Vector3.UnitY, Vector3.UnitX);

		// Correct rotation around Y axis
		Matrix4x4 yflip = Matrix4x4.CreateRotationY(this.DegToRad(90.0f));
		billboard = Matrix4x4.Multiply(yflip, billboard);

		// Reorients any X-rotation that might have happened. 
		Matrix4x4 xflip = Matrix4x4.CreateRotationX(this.DegToRad(HkaEulerAngles.ToEuler(eye.Rotation).X));
		billboard = Matrix4x4.Multiply(xflip, billboard);

		return Quaternion.CreateFromRotationMatrix(billboard);
	}

	private void ResetEyes() {

	}

	// Visibility
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
		if(this.ResolveActorEntity() is not ActorEntity selected) return;
		if(this.SelectBoneByName(selected, "Head") is not SceneEntity neck) return; // This selectes the GROUP called head, which is actually the neck
		if(selected.Recurse().Where(x => x.Name == "Head" && x is not BoneNodeGroup).FirstOrDefault() is not SceneEntity head) return;

		// Save current and load reference pose
		var epc = new EntityPoseConverter(selected.Pose!);
		var org = epc.Save();
		epc.LoadReferencePose();
		var fin = epc.Save();

		// Load the original expression and rotate+position neck to reference pose
		this._ctx.Selection.Select(neck);
		var gsb = epc.GetSelectedBones(false).ToList();
		epc.LoadSelectedBones(org, PoseTransforms.Position | PoseTransforms.Rotation);
		epc.LoadBones(fin, gsb, PoseTransforms.Position | PoseTransforms.Rotation);

		// Change selection to head bone and rotate+position it to reference pose
		this._ctx.Selection.Select(head);
		gsb = epc.GetSelectedBones(false).ToList();
		epc.LoadBones(fin, gsb, PoseTransforms.Rotation | PoseTransforms.Position);
	}
	
	private SceneEntity? SelectBoneByName(ActorEntity act, string name) {
		return act.Recurse().Where(x => x.Name == name).FirstOrDefault();
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

	// Support functions
	private double RadToDeg(float rad) {
		return rad * 180 / Math.PI;
	}
	private float DegToRad(float deg) {
		return (float)(deg * Math.PI / 180);
	}
}
