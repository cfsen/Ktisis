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

namespace Ktisis.LazyExtras.Components;

public class LazyOverlayComponents {
	private IEditorContext ctx;

	public LazyOverlayComponents(IEditorContext _ctx) {
		this.ctx = _ctx;
	}
	
	// Overlay visibility 

	/// <summary>
	/// Toggles overlay visibility of essential gesture bones
	/// </summary>
	public void ToggleGestureBones(ActorEntity selected) {
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
	public void ToggleGestureDetailBones(ActorEntity selected) {
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
		var selected = this.ctx.LazyExtras.ResolveActorEntity();
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

	// Debug

	private static void dbg(string s) => Ktisis.Log.Debug($"LazyOverlayComp: {s}"); 

}
