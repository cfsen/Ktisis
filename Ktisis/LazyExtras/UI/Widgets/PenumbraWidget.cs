using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Components;
using Ktisis.LazyExtras.Datastructures;
using Ktisis.LazyExtras.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.UI.Widgets;
class PenumbraWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyIpcIntegrator ipc;
	private LazyUi lui;
	private LazyUiSizes uis;
	internal static readonly string[] tableHeaders = new string[]{"Actor", "Collection", "Favorite", "Set"};

	public PenumbraWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Scene;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.ipc = ctx.LazyExtras.ipc;
		this.lui = new();
		this.uis = lui.uis;
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.WandMagicSparkles, "Glamourer & Penumbra");

		ImGui.SetCursorPosX(uis.SidebarW-2*uis.BtnSmall.X-2*uis.Space);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY()-0.6f*uis.BtnSmall.Y);
		if(lui.BtnIcon(FontAwesomeIcon.CircleNodes, "WPNB_ScanActors", uis.BtnSmall, "Scan"))	
			ctx.LazyExtras.ipc.ScanSceneActors();
		//ImGui.SameLine();
		//if(ImGui.Button("Debug"))
		//	ctx.LazyExtras.ipc.DumpDbg();
		ImGui.NewLine();

		DrawActorsTable(ipc.bufferFavs, true);

		ImGui.NewLine();
		DrawInactiveFavs();

		lui.DrawFooter();
		ImGui.EndGroup();
	}
	private void DrawInactiveFavs() {
		var favs = ipc.InactiveFavs();

		ImGui.Text("Favorites:");
		ImGui.BeginTable("WPNB_FavTable", 3);

		int i = 0;
		foreach(var fav in favs){
			if((i % 3) == 0)
				ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(i % 3);

			if(lui.BtnIcon(FontAwesomeIcon.Plus, $"WPNB_SpawnAct_LineNo{i}", uis.BtnSmaller, "Spawn")) {
				_ = ipc.SpawnFavorite(fav);
			}

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			ImGui.Text(fav.Name.Length < 10 ? fav.Name : string.Concat(fav.Name.AsSpan(0, 10), "..."));

			i++;
		}
		ImGui.EndTable();
	}
	private void DrawActorsTable(List<LazyIpcFavorite> actors, bool allowDesignChange = false) {
		if(actors == null || actors.Count < 1) return;

		lui.DrawPseudoTable(tableHeaders);
		ImGui.NewLine();
		int i = 0;
		foreach (var act in actors) {
			ImGui.Text(act.Name.Length < 10 ? act.Name : string.Concat(act.Name.AsSpan(0, 10), "..."));
			ImGui.SameLine();
			ImGui.SetCursorPosX(uis.SidebarW/4);

			ImGui.Text(act.PenumbraName);
			ImGui.SameLine();
			ImGui.SetCursorPosX(2*uis.SidebarW/4);

			using (ImRaii.Disabled(act.Persistent)) {
				if(lui.BtnIcon(FontAwesomeIcon.Plus, $"WPNB_AddFav_LineNo{i}", uis.BtnSmaller, "Add to favorites"))
					ipc.AddToFavorites(act);
				}
			ImGui.SameLine();
			using (ImRaii.Disabled(!act.Persistent)) {
				if(lui.BtnIcon(FontAwesomeIcon.Minus, $"WPNB_DelFav_LineNo{i}", uis.BtnSmaller, "Remove from favorites"))
					ipc.ActorRemove(act);
			}
			ImGui.SameLine();
			using (ImRaii.Disabled(!act.Persistent || allowDesignChange == false)) {
				ImGui.SetCursorPosX(3*uis.SidebarW/4);
				if(lui.BtnIcon(FontAwesomeIcon.Star, $"WPNB_ApplyFav_Lineno{i}", uis.BtnSmaller, "Set saved values"))
					ipc.ApplyAssociatedDesign(act);
			}
			i++;
		}
	}
	private void dbg(string s) => Ktisis.Log.Debug($"PenumbraWidget: {s}");
}
