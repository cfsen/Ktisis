using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Data.Config.Pose2D;
using Ktisis.Data.Serialization;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Posing;
using Ktisis.Interface.Components.Posing.Types;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class PoseFaceWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private readonly PoseViewRenderer _render;
	private PoseViewSchema? Schema;
	private ITextureProvider tex;

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;

	private SceneEntity? lastSelected = null;
	private ActorEntity? selectedActor;

	public PoseFaceWidget(IEditorContext ctx, ITextureProvider tex) {
		this.Category = LazyWidgetCat.Pose;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.tex = tex;
		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
		
		this._render = new PoseViewRenderer(ctx.Config, tex);
		this.Schema = SchemaReader.ReadPoseView();
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.Portrait, "Expression posing");
		using var _ = ImRaii.TabBar("##pose_tabs");

		var actors = this.ctx.Scene.Children
			.Where(entity => entity is ActorEntity)
			.Cast<ActorEntity>();

		foreach (var actor in actors) {
			using var tab = ImRaii.TabItem(actor.Name);
			if (!tab.Success) continue;
			
			ImGui.Spacing();
			
			DrawView(actor, new(uis.SidebarW,600));
		}
		lui.DrawFooter();
		ImGui.EndGroup();
	}
	private void DrawView(ActorEntity target, Vector2 region) {
		using var _ = ImRaii.Child("##viewFrame", region, false, ImGuiWindowFlags.NoScrollbar);

		var frame = this._render.StartFrame();
		
			this.DrawView(frame, "Face", 0.9f);
			using (var _group = ImRaii.Group()) {
				this.DrawView(frame, "Lips", 0.55f, 0.50f);
				ImGui.SameLine();
				this.DrawView(frame, "Mouth", 0.45f, 0.50f);
			}
		
		if (target.Pose != null)
			frame.DrawBones(target.Pose);
	}

	private void DrawView(
		IViewFrame frame,
		string name,
		float width = 1.0f,
		float height = 1.0f,
		IDictionary<string, string>? template = null
	) {
		if (this.Schema == null) return;

		if (!this.Schema.Views.TryGetValue(name, out var view))
			return;

		frame.DrawView(view, width, height, template);
	}
}
