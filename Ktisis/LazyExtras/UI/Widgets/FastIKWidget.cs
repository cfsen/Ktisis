using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class FastIKWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;

	private List<IIkNodeState> nodeStates;
	private ActorEntity? fetchedActor;

	public FastIKWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Gesture;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
		this.nodeStates = [];
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.PersonCircleCheck, "Fast IK");
		
		ActorCheck();
		DrawIKControls();

		lui.DrawFooter();
		ImGui.EndGroup();
	}
	private void DrawIKControls() {
		if(nodeStates.Count < 1) {
			ImGui.Text("No IK nodes");
			return;
		}
		using (ImRaii.Disabled(fetchedActor == null)) {
			ImGui.BeginTable("iktable", 2);

			foreach(var node in nodeStates) {
				if(node.SceneEntity is not (ITwoJointsNode or ICcdNode))
					continue;
				ImGui.TableNextColumn();
				DrawIKNodeSwitch(node);
			}

			ImGui.EndTable();
		}
	}
	private void DrawIKNodeSwitch(IIkNodeState node) {
		ImGui.BeginGroup();

		ImGui.BeginDisabled(!ctx.Posing.IsEnabled);
		if(ImGui.Checkbox($"{node.Name}##WFASTIK_Enable{node.Name}", ref node.Enabled))
			IkToggleHandle((IIkNode)node.SceneEntity);
		ImGui.EndDisabled();

		ImGui.BeginDisabled(!node.Enabled);
		if(node.SceneEntity is ITwoJointsNode tjn) 
			ImGui.Checkbox($"Lock rotation##WFASTIK_RotLock{node.Name}", ref tjn.Group.EnforceRotation);
		ImGui.EndDisabled();

		if(node.SceneEntity is ICcdNode ccd) {
			ImGui.Dummy(new (0, ImGui.GetFontSize()));
		}

		ImGui.EndGroup();
	}
	private void IkToggleHandle(IIkNode se) {
		if(se.IsEnabled)
			se.Disable();
		else 
			se.Enable();
	}
	private void ActorCheck() {
		if(ctx.LazyExtras.SelectedActor == null) return;
		if(fetchedActor == null || ctx.LazyExtras.SelectedActor != fetchedActor) {
			fetchedActor = ctx.LazyExtras.SelectedActor;
			GetIKNodes();
		}
	}
	private void GetIKNodes() {
		if(ctx.LazyExtras.SelectedActor == null) return;
		nodeStates.Clear();
		var ents = ctx.LazyExtras.SelectedActor.Recurse().OfType<SceneEntity>();

		foreach(var ent in ents) {

			if(ent is not IIkNode ikn)
				continue;
			// TODO look for the flag
			if(!ctx.Config.Categories.ShowNsfwBones && "IVP".Contains(ent.Name[0]))
				continue;


			nodeStates.Add(new IIkNodeState {
				Name = ent.Name,
				Enabled = ikn.IsEnabled,
				SceneEntity = ent
			});
		}
	}

	// Debug

	private void dbg(string s) => Ktisis.Log.Debug($"FastIKWidget: {s}");

	private class IIkNodeState {
		public required string Name { get; set; }
		public bool Enabled;
		public required SceneEntity SceneEntity { get; set; }
	}
}
