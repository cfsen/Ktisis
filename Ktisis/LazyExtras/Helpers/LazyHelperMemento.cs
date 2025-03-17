using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Scene.Entities.Skeleton;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Helpers;
public class LazyHelperMemento {
	private IEditorContext ctx;
	private PoseTransforms xfms;
	private EntityPoseConverter epc;
	private PoseContainer initial;
	private PoseContainer? final;

	public LazyHelperMemento(IEditorContext ctx, EntityPose ep, PoseTransforms xfms = PoseTransforms.Rotation) {
		this.ctx = ctx;
		this.xfms = xfms;
		this.epc = new EntityPoseConverter(ep);
		this.initial = epc.Save();
	}
	public void Save() {
		this.final = this.epc.Save();
		this.ctx.Actions.History.Add(MementoCreate(epc, initial, final));
	}
	private PoseMemento MementoCreate(EntityPoseConverter epc, PoseContainer initial, PoseContainer final) {
		return new PoseMemento(epc) {
			Modes = PoseMode.All,
			Transforms = xfms,
			Bones = null,
			Initial = initial,
			Final = final
		};
	}
}
