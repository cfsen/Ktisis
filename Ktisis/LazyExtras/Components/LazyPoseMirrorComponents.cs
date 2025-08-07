using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazyPoseMirrorComponents {
	private IEditorContext ctx;
	private Dictionary<string, BoneMirrorAxis> axisMap;
	public LazyPoseMirrorComponents(IEditorContext ctx) { 
		this.ctx = ctx;
		this.axisMap = [];
		BuildAxisMap();
	}

	public async void Flip() {
		if(ctx.LazyExtras.ResolveActorEntity() is not ActorEntity ae) return;
		if(ae.Pose == null) return;

		// store pose to be flipped, get reference pose
		EntityPoseConverter epc = new EntityPoseConverter(ae.Pose);
		var org = epc.Save();
		var target = epc.Save();
		await ctx.LazyExtras.fw.RunOnFrameworkThread(() => {
			epc.LoadReferencePose();
		});
		var reference = epc.Save();
		foreach(var bone in org) {
			if(axisMap.TryGetValue(bone.Key, out BoneMirrorAxis bma)) {
				dbg(bone.Key);
				// angleZ.Y = Z axis
				var localVectorX = Vector3.Transform(Vector3.UnitX, bone.Value.Rotation);
				var angleX = ctx.LazyExtras.math.VectorAngles(localVectorX);

				// angleZ.Z = Y axis, angleZ.X = X axis
				var localVectorZ = Vector3.Transform(Vector3.UnitZ, bone.Value.Rotation);
				var angleZ = ctx.LazyExtras.math.VectorAngles(localVectorZ);

				dbg($"XYZ: {angleZ.X}, {angleZ.Z}, {angleX.Y}");
				// TODO save these angles*(-1), apply that rotation via the other system for interacting with bones


				//if(target.ContainsKey(bone.Key)) {
				//	target[bone.Key].Rotation *= Quaternion.CreateFromYawPitchRoll(-angleZ.Z, -angleZ.X, 0.0f);
				//}
			}
		}
		await ctx.LazyExtras.fw.RunOnFrameworkThread(() => {
			epc.Load(target, PoseMode.Body, PoseTransforms.Rotation | PoseTransforms.Position | PoseTransforms.Scale);
		});
	}


	private void BuildAxisMap() {
		foreach(var bone in flip_xy) {
			axisMap.Add(bone, BoneMirrorAxis.X | BoneMirrorAxis.Y);
		}
	}



	[Flags]
	private enum BoneMirrorAxis { X = 1, Y = 2, Z = 4 };

		//"j_asi_a_l": "Left Leg",
		//"j_asi_a_r": "Right Leg",
		//"j_asi_c_l": "Left Calf",
		//"j_asi_c_r": "Right Calf",
		//"j_asi_d_l": "Left Foot",
		//"j_asi_d_r": "Right Foot",
		//"j_asi_e_l": "Left Toes",
		//"j_asi_e_r": "Right Toes",
		//"j_ude_b_l": "Left Forearm",
		//"j_ude_b_r": "Right Forearm",
		//"n_hkata_l": "Left Shoulder (Twist)",
		//"n_hkata_r": "Right Shoulder (Twist)",
		//"j_te_l": "Left Hand",
		//"j_te_r": "Right Hand",
		//"n_hhiji_l": "Left Elbow (Twist)",
		//"n_hhiji_r": "Right Elbow (Twist)",

		//"n_hte_l": "Left Wrist",
		//"n_hte_r": "Right Wrist",
		//"j_hito_a_l": "Left Index A",
		//"j_hito_a_r": "Right Index A",
		//"j_ko_a_l": "Left Pinky A",
		//"j_ko_a_r": "Right Pinky A",
		//"j_kusu_a_l": "Left Ring A",
		//"j_kusu_a_r": "Right Ring A",
		//"j_naka_a_l": "Left Middle A",
		//"j_naka_a_r": "Right Middle A",
		//"j_oya_a_l": "Left Thumb A",
		//"j_oya_a_r": "Right Thumb A",
		//"n_buki_l": "Left Weapon",
		//"n_buki_r": "Right Weapon",
		//"n_ear_b_l": "Left Earring B",
		//"n_ear_b_r": "Right Earring B",
		//"j_hito_b_l": "Left Index B",
		//"j_hito_b_r": "Right Index B",
		//"j_ko_b_l": "Left Pinky B",
		//"j_ko_b_r": "Right Pinky B",
		//"j_kusu_b_l": "Left Ring B",
		//"j_kusu_b_r": "Right Ring B",
		//"j_naka_b_l": "Left Middle B",
		//"j_naka_b_r": "Right Middle B",
		//"j_oya_b_l": "Left Thumb B",
		//"j_oya_b_r": "Right Thumb B",

		//"j_asi_b_l", // "Left Knee",
		//"j_asi_b_r", // "Right Knee",
		//"j_mune_l", // "Left Breast",
		//"j_mune_r", // "Right Breast",
		//"j_sako_l", // "Left Clavicle",
		//"j_sako_r", // "Right Clavicle",
		//"j_ude_a_l", // "Left Arm",
		//"j_ude_a_r", // "Right Arm",

		//"n_hara", // "Abdomen",
		//"n_throw", // "Throw",
	private List<string> flip_xy = [
		"j_kosi", // "Waist",
		"j_sebo_a", // "Lumbar",
		"j_sebo_b", // "Thoracic",
		"j_sebo_c", // "Cervical",
		"j_kubi", // "Neck",
		"j_kao", // "Head",
		"n_sippo_a", // "Tail A",
		"n_sippo_b", // "Tail B",
		"n_sippo_c", // "Tail C",
		"n_sippo_d", // "Tail D",
		"n_sippo_e", // "Tail E",
	];
	private void dbg(string s) => Ktisis.Log.Debug($"LazyPoseMirror: {s}");
}
