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

	//private Dictionary<string, xfmBLF> TargetBlacklist { get; set; }

	public LazyPoseLerp(IEditorContext ctx) { 
		this.ctx = ctx;
		this.lerpTargets = [];
		wtd_origin = new WorldTransformData();
		wtd_target = new WorldTransformData();
	}
	
	public void SetupLerp(ActorEntity? ae, string poseDataJson) {
		if(ae == null) return;
		if(ae.Pose == null) return;
		actor = ae;


		JsonFileSerializer jfs = new();
		if(jfs.Deserialize<PoseFile>(poseDataJson) is not PoseFile pf) return;
		if(pf.Bones == null) return;

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

		foreach(var trg in lerpTargets) {
			if (between!.ContainsKey(trg) && origin!.ContainsKey(trg)) {
				between[trg].Rotation = Quaternion.Slerp(origin![trg].Rotation, target![trg].Rotation, lerpFactor);
				between[trg].Position = Vector3.Lerp(origin[trg].Position, target![trg].Position, lerpFactor);
				between[trg].Scale = Vector3.Lerp(origin[trg].Scale, target![trg].Scale, lerpFactor);
			} 
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
