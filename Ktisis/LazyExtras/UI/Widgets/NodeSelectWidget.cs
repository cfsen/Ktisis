using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Colors;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

using System;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class NodeSelectWidget :ILazyWidget {
	private readonly IEditorContext _ctx;

	// LazyWidget
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private LazyUi lui;
	private LazyUiSizes uis;
	private bool filterActors = true;
	private bool filterLights = true;

	public NodeSelectWidget(IEditorContext ctx) {

		this._ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;

		// LazyWidget
		this.Category = LazyWidgetCat.Selection;
		this.InToolbelt = false;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
	}
	private void dbg(string s) => Ktisis.Log.Debug(s);
	public void UpdateScaling() {
		// Doesn't need to :)
	}
	public void Draw() {
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.AddressBook, "Entity tree");
		DrawTreeButtons();
		DrawTree();
		lui.DrawFooter();
		ImGui.EndGroup();
	}

	private void DrawFilterControl() {
		ImGui.BeginGroup();
		ImGui.Checkbox("Actors", ref filterActors);
		ImGui.SameLine();
		ImGui.SetCursorPosX(uis.SidebarW/2);
		ImGui.Checkbox("Lights", ref filterLights);
		ImGui.Dummy(new(0,uis.Space));
		ImGui.EndGroup();
	}
	private void DrawTreeButtons() {
		ImGui.BeginGroup();
		if(lui.BtnIcon(FontAwesomeIcon.PersonCirclePlus, "WNodeTreeSpawnActor", uis.BtnSmall, "Spawn actor"))
			this._ctx.Scene.Factory.CreateActor().Spawn();
		ImGui.SameLine();
		if(lui.BtnIcon(FontAwesomeIcon.Lightbulb, "WNodeTreeSpawnLight", uis.BtnSmall, "Spawn light"))
			 this._ctx.Scene.Factory.CreateLight(Structs.Lights.LightType.SpotLight).Spawn();
		ImGui.SameLine();
		ImGui.EndGroup();

		ImGui.SameLine();

		ImGui.BeginGroup();
		DrawFilterControl();
		ImGui.EndGroup();

		ImGui.SameLine();
		ImGui.SetCursorPosX(uis.SidebarW-3*uis.BtnSmall.X);

		ImGui.BeginGroup();
		DrawTreeActorButtons();
		ImGui.Dummy(new(0,uis.Space));
		ImGui.EndGroup();
	}

	private void DrawTree() {
		var items = this._ctx.Scene.Children;
		if(!filterActors)
			items = items.Where(x => x is not ActorEntity);
		if(!filterLights)
			items = items.Where(x => x is not LightEntity);

		var treeFlags = ImGuiTreeNodeFlags.SpanAvailWidth ^ ImGuiTreeNodeFlags.OpenOnArrow;

		foreach(var node in items)
			Recurse(node);

		void Recurse(SceneEntity node) {
			var nodeFlags = treeFlags;
			bool children = node.Children.Any();
			bool selected = _ctx.Selection.GetSelected().Contains(node);

			if(selected)
				nodeFlags |= ImGuiTreeNodeFlags.Selected;
			if(!children)
				nodeFlags |= ImGuiTreeNodeFlags.Leaf;

			// Skip making a node for the the characters Pose element for UX
			if(node.Name == "Pose") {
				if(children)
					foreach(var child in node.Children)
						Recurse(child);
				return;
			}

			bool nodeOpen = ImGui.TreeNodeEx($"{node.Name}##SceneChildrenTree{node.GetHashCode():X}", nodeFlags);

			// Click handlers
			if (ImGui.IsItemClicked()) 
				_ctx.Selection.Select(node, ImGui.GetIO().KeyCtrl ? Editor.Selection.SelectMode.Multiple : Editor.Selection.SelectMode.Default);
			if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered())
				_ctx.Interface.OpenEditorFor(node);
			if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
				_ctx.Interface.OpenSceneEntityMenu(node);

			// Skeleton overlay toggle
			DrawButtonVis(node);

			if(nodeOpen){
				if (children) {
					foreach(var child in node.Children)
						Recurse(child);
				}
				ImGui.TreePop();
			}
		}

		void DrawButtonVis(SceneEntity node) {
			if (node is IVisibility vis) {
				var icon = (vis.Visible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash).ToIconString();
				var iconColor = vis.Visible ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey;

				ImGui.SameLine(uis.SidebarW-66.0f); // fix later
				var cpos = ImGui.GetCursorScreenPos();
				using(ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
					using(ImRaii.PushFont(UiBuilder.IconFont)) {
						ImGui.Text(icon);
					}
				}
				var iconStart = ImGui.GetItemRectMin();
				var iconEnd = iconStart + ImGui.GetItemRectSize();

				if (ImGui.IsMouseHoveringRect(iconStart, iconEnd) && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) 
					vis.Toggle();
			}
		}
	}
			
	private void DrawTreeActorButtons() {
		if(_ctx.LazyExtras.SelectedActor is not ActorEntity ae) return;
		using (ImRaii.Disabled(_ctx.LazyExtras.SelectedActor == null || !_ctx.Posing.IsEnabled)) {
			if(lui.BtnIcon(FontAwesomeIcon.Save, "WNodeTreeSavePose", uis.BtnSmall, "Save pose"))
				ExportPose(ae.Pose);		// TODO
		}
		ImGui.SameLine();
		if(lui.BtnIcon(FontAwesomeIcon.LocationCrosshairs, "WNodeTreeTarget", uis.BtnSmall, "Target actor"))
			ae.Actor.SetGPoseTarget();
	}

	private async void ExportPose(EntityPose? pose) {
		if (pose == null) return;
		await _ctx.Interface.OpenPoseExport(pose);
	}
}
