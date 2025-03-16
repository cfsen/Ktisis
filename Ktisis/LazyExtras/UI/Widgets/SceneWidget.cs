using Dalamud.Interface;
using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class SceneWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	private bool dialogOpen;

	public SceneWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Scene;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		if(dialogOpen)
			ctx.LazyExtras.io.DrawDialog();
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.Ad, "Scene");
		
		BtnSave();
		ImGui.SameLine();
		BtnLoad();

		lui.DrawFooter();
		ImGui.EndGroup();
	}
	public void BtnSave() {
		if(lui.BtnIcon(FontAwesomeIcon.Save, "WDG_SceneSave", uis.BtnSmall, "Export")) {
			dialogOpen = true;
			ctx.LazyExtras.io.SetSaveBuffer(ctx.LazyExtras.scene.ExportSceneFile());
			ctx.LazyExtras.io.OpenSceneSaveDialog((valid, res) => {
				if (valid) {
					ctx.LazyExtras.scene.ExportSceneFile();
				}
				dialogOpen = false;
				});
		}
	}
	public void BtnLoad() {
		if(lui.BtnIcon(FontAwesomeIcon.FolderOpen, "WDG_SceneLoad", uis.BtnSmall, "Import scene")) {
			dialogOpen = true;
			ctx.LazyExtras.io.OpenSceneDialog((valid, res) => {
				if (valid) {
					var _ = ctx.LazyExtras.io.ReadFile(res[0]);
					if(_ != null) {
						ctx.LazyExtras.scene.Import(_.Value.data);
					}

				}
				dialogOpen = false;
				});
		}
	}
	private void dbg(string s) => Ktisis.Log.Debug($"SceneWidget: {s}");
}
