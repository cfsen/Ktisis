using Dalamud.Plugin.Ipc.Exceptions;

using Ktisis.Common.Utility;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Data.Serialization;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.LazyExtras.Datastructures;
using Ktisis.LazyExtras.Helpers;
using Ktisis.LazyExtras.UI.Widgets;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazyPoseLerp {
	private IEditorContext ctx;

	private ActorEntity? actor;
	private PoseContainer? origin;
	private PoseContainer? between;
	private PoseContainer? target;
	private EntityPoseConverter? epc;
	private List<string> lerpTargets;
	private WorldTransformData wtd_origin;
	private WorldTransformData wtd_target;
	
	public float lerpFactor = 0.0f;

	private Dictionary<string, Dictionary<string, float>> faceFilters = [];

	public LazyPoseLerp(IEditorContext ctx) { 
		this.ctx = ctx;
		this.lerpTargets = [];
		wtd_origin = new WorldTransformData();
		wtd_target = new WorldTransformData();
	}
	public void SetupLerp(ActorEntity? ae, string poseDataJson) {
		if(ae == null) return;
		if(ae.Pose == null) return;

		BuildFilterDicts();

		actor = ae;

		JsonFileSerializer jfs = new();
		if(jfs.Deserialize<PoseFile>(poseDataJson) is not PoseFile pf) return;
		if(pf.Bones == null) return;

		/*
		 
		The following is a workaround to calculate a target pose for lerping.

		Ideally, this would be done by loading the target into memory, and:
			1.	Calculate matricies for w2l and l2w for both the origin and the target
				based on an anchor bone, in whose local space the bones to LERP can be 
				accurately positioned.
			2.	Transform the target to origin using it's own l2w: position, then rotation.
				At this point, the anchor bone should be at pos 0,0,0 with an identity 
				quaternion for orientation.
			3.	Transform the target to origin world space, using origins w2l: rotation,
				then position.

		This would shift the target pose into world space, with the anchor bone of
		both origin and target in the same position and orientation. From there, 
		lerping between the two without further transformations is possible. 

		I may revisit this later, though that would mean interacting directly with HavokPosing.cs
		 
		 */

		epc = new EntityPoseConverter(ae.Pose);
		origin = epc.Save();
		between = epc.Save();
		target = pf.Bones;

		// This crime against humanity is a port of the code from loading poses with anchor/rotation preservation
		var pbi = epc.GetBones();
		List<PartialBoneInfo> pbiHead = new List<PartialBoneInfo>();
		foreach(var p in pbi) {
			if(headBones.Contains(p.Name))
				pbiHead.Add(p);
		}

		// Load the head of the target pose up, and restore neck and face to origin
		epc.LoadBones(target, pbiHead, PoseTransforms.Rotation | PoseTransforms.Position | PoseTransforms.Scale);
		List<PartialBoneInfo> restore = new List<PartialBoneInfo>();
		restore.Add(pbiHead.Where(x => x.Name == "j_kubi").First());
		epc.LoadBones(origin, restore, PoseTransforms.Position);
		restore.Add(pbiHead.Where(x => x.Name == "j_kao").First());
		epc.LoadBones(origin, restore, PoseTransforms.Rotation);
		target = epc.Save();

		// now load origin back up
		epc!.Load(origin, PoseMode.All, PoseTransforms.Rotation | PoseTransforms.Position | PoseTransforms.Scale);
	
		foreach (var x in pbiHead) {
			lerpTargets.Add(x.Name);
		}
	}
	public void ToggleLerp() {
		if(ctx.LazyExtras.SelectedActor is not ActorEntity ae) return;
		if(ae.Pose == null) return;

		LazyHelperMemento lhm = new(ctx, ae.Pose, PoseTransforms.Rotation | PoseTransforms.Position | PoseTransforms.Scale);
		lhm.Save();
		return;
	}
	public void RefreshActor(ActorEntity? ae) {
		if(ae == null) return;
		if(ae.Pose == null) return;
		actor = ae;
		epc = new EntityPoseConverter(ae.Pose);
	}
	public void Slide() {
		if(!CanLerp()) return;

		float boneLerpFactor = 1.0f;
		foreach(var trg in lerpTargets) {
			if (!(between!.ContainsKey(trg) && origin!.ContainsKey(trg)))
				continue;
			
			boneLerpFactor = CheckFilter(trg);

			between[trg].Rotation	= Quaternion.Slerp(origin![trg].Rotation, target![trg].Rotation, boneLerpFactor);
			between[trg].Position	= Vector3.Lerp(origin[trg].Position, target![trg].Position, boneLerpFactor);
			between[trg].Scale		= Vector3.Lerp(origin[trg].Scale, target![trg].Scale, boneLerpFactor);
		}

		epc!.Load(between!, PoseMode.All, PoseTransforms.Rotation | PoseTransforms.Position | PoseTransforms.Scale);
	}

	private bool CanLerp() {
		if(actor == null)			return false;
		if(origin == null)			return false;
		if(between == null)			return false;
		if(target == null)			return false;
		if(epc == null)				return false;
		if(lerpTargets.Count == 0)	return false;
		return true;
	}
	public float CheckFilter(string bone) {
		foreach(var d in faceFilters)
			if(d.Value.TryGetValue(bone, out float val))
				return val > 0.0f ? val : lerpFactor;
		return lerpFactor;
	}
	public void SetFilterGroupValue(string group, float val) {
		if(faceFilters.TryGetValue(group, out var dict))
			foreach(var x in dict.Keys.ToList())
				dict[x] = val;	
	}
	private void BuildFilterDicts() {
		Dictionary<string, float> fDictHair		= [];
		Dictionary<string, float> fDictEars		= [];
		Dictionary<string, float> fDictEyes		= [];
		Dictionary<string, float> fDictLids		= [];
		Dictionary<string, float> fDictBrow		= [];
		Dictionary<string, float> fDictCheek	= [];
		Dictionary<string, float> fDictMouth	= [];
		Dictionary<string, float> fDictTongue	= [];
		faceFilters.Clear();

		foreach(var b in headBones) {
			string m7 = b.Length >= 7 ? b[..7] : b;
			string m6 = b.Length >= 6 ? m7[..6] : b;
			string m5 = b.Length >= 5 ? m6[..5] : b;
			string m4 = b.Length >= 4 ? m5[..4] : b;

			if		(IsTongue(m7))		fDictTongue.Add(b, 0.0f);
			else if (IsHair(m4, m6))	fDictHair.Add(b, 0.0f);
			else if (IsEars(m4, m6))	fDictEars.Add(b, 0.0f);
			else if (IsEyes(m6, m7))	fDictEyes.Add(b, 0.0f);
			else if (IsBrow(m6, m7))	fDictBrow.Add(b, 0.0f);	
			else if (IsLids(m7))		fDictLids.Add(b, 0.0f);
			else if (IsCheek(m6, m7))	fDictCheek.Add(b, 0.0f);	
			else if (IsMouth(m5, m7))	fDictMouth.Add(b, 0.0f);
		}

		faceFilters.Add("Tongue", fDictTongue);
		faceFilters.Add("Hair", fDictHair);
		faceFilters.Add("Ears", fDictEars);
		faceFilters.Add("Eyes", fDictEyes);
		faceFilters.Add("Brow", fDictBrow);
		faceFilters.Add("Lids", fDictLids);
		faceFilters.Add("Cheeks", fDictCheek);
		faceFilters.Add("Mouth", fDictMouth);

		//int i = 0;
		//foreach(var d in faceFilters)
		//{
		//	foreach(var f in d.Value)
		//	{
		//		dbg($"{f.Key} -> {f.Value}");
		//		i++;
		//	}
		//}
		//dbg($"Filtering: {i}/{headBones.Count}");

		bool IsHair(string m4, string m6) 
			=> (m4 == "j_ex"	|| m6 == "j_kami");
		bool IsEars(string m4, string m6)
			=> (m6 == "j_mimi"	|| m6 == "j_zera"	|| m4 == "j_ea");
		bool IsEyes(string m6, string m7)
			=> (m6 == "j_f_ey"	|| m6 == "j_f_ir"	|| m6 == "j_f_no");
		bool IsLids(string m7)
			=> (m7 == "j_f_uma"	|| m7 == "j_f_mab"	|| m7 == "j_f_dma");
		bool IsBrow(string m6, string m7)
			=> (m6 == "j_f_mi"	|| m7 == "j_f_may"	|| m6 == "j_f_mm");
		bool IsCheek(string m6, string m7)
			=> (m6 == "j_f_ho"	|| m7 == "j_f_dho"	|| m7 == "j_f_sho" ||
				m7 == "j_f_dme");
		bool IsMouth(string m5, string m7)
			=> (m7 == "j_f_ago"	|| m7 == "j_f_dag"	|| m7 == "j_f_hag" || 
				m7 == "j_f_dli" || m7 == "j_f_dml"	|| m7 == "j_f_dsl" ||
				m7 == "j_f_uli" || m7 == "j_f_uml"	|| m7 == "j_f_usl" ||
				m5 == "j_ago");
		bool IsTongue(string m7)
			=> (m7 == "j_f_ber");
	}

	// don't look
	private List<string> headBones = [
		"j_ex_h0150_ke_b",
		"j_ex_h0150_ke_f",
		"j_ex_h0150_ke_l",
		"j_ex_h0150_ke_u",
		"j_kami_a",
		"j_kami_f_l",
		"j_kami_f_r",
		"j_kami_b",
		"j_mimi_l",
		"j_mimi_r",
		"j_f_mayu_l",
		"j_f_miken_01_l",
		"j_f_mmayu_l",
		"j_f_miken_02_l",
		"j_f_mayu_r",
		"j_f_miken_01_r",
		"j_f_mmayu_r",
		"j_f_miken_02_r",
		"j_f_mabdn_01_l",
		"j_f_mabup_01_l",
		"j_f_mabdn_02out_l",
		"j_f_mabdn_03in_l",
		"j_f_mabup_02out_l",
		"j_f_mabup_03in_l",
		"j_f_eye_l",
		"j_f_mab_l",
		"j_f_eyepuru_l",
		"j_f_mabdn_01_r",
		"j_f_mabup_01_r",
		"j_f_mabdn_02out_r",
		"j_f_mabdn_03in_r",
		"j_f_mabup_02out_r",
		"j_f_mabup_03in_r",
		"j_f_eye_r",
		"j_f_mab_r",
		"j_f_eyepuru_r",
		"j_f_eyeprm_01_l",
		"j_f_eyeprm_01_r",
		"j_f_eyeprmroll_l",
		"j_f_eyeprmroll_r",
		"j_f_eyeprm_02_l",
		"j_f_eyeprm_02_r",
		"j_f_irisprm_l",
		"j_f_irisprm_r",
		"j_f_noanim_eyesize_l",
		"j_f_noanim_eyesize_r",
		"j_f_hana_l",
		"j_f_hana_r",
		"j_f_uhana",
		"j_f_dhoho_l",
		"j_f_dmemoto_l",
		"j_f_hoho_l",
		"j_f_shoho_l",
		"j_f_dhoho_r",
		"j_f_dmemoto_r",
		"j_f_hoho_r",
		"j_f_shoho_r",
		"j_f_ulip_01_l",
		"j_f_ulip_01_r",
		"j_f_umlip_01_l",
		"j_f_umlip_01_r",
		"j_f_uslip_l",
		"j_f_uslip_r",
		"j_f_ulip_02_l",
		"j_f_ulip_02_r",
		"j_f_umlip_02_l",
		"j_f_umlip_02_r",
		"j_f_hagukiup",
		"j_f_dslip_l",
		"j_f_dslip_r",
		"j_f_dlip_01_l",
		"j_f_dlip_01_r",
		"j_f_dmlip_01_l",
		"j_f_dmlip_01_r",
		"j_f_dlip_02_l",
		"j_f_dlip_02_r",
		"j_f_dmlip_02_l",
		"j_f_dmlip_02_r",
		"j_f_ago",
		"j_f_dago",
		"j_f_hagukidn",
		"j_f_bero_01",
		"j_f_bero_02",
		"j_f_bero_03",
		"j_f_noanim_ago",
		"j_f_face",
		"j_f_dmiken_l",
		"j_f_dmiken_r",
		"j_kubi",
		"j_kao",
		"j_head"
		];

	private void dbg(string s) => Ktisis.Log.Debug($"LazyPoseLerp: {s}");

}
