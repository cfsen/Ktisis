using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Datastructures;
using Ktisis.LazyExtras.Interfaces;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class ActorOffsetWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;

	private bool dialogOpen = false;
	private LazyActorOffsetFile offsets;
	private string? offsetsString;
	private string? loadedOffsetFileName;
	private string? loadedOffsetPoseDir;
	private string? loadedOffsetDirFriendly;
	private bool previewImport = true;
	private bool offsetLoadModeLocal = true;

	public ActorOffsetWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Misc;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
		offsets = new();
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		if(dialogOpen)
			ctx.LazyExtras.io.DrawDialog();

		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.PeopleGroup, "Actor offsets");

		DrawImportControls();
		ImGui.SameLine();
		DrawFileOpenPicker();
		ImGui.SameLine();
		DrawExportControls();
		ImGui.SameLine();
		ImGui.Checkbox("Local mode", ref offsetLoadModeLocal);
		//ImGui.SameLine();
		//DrawPreviewSelect();
		//ImGui.NewLine();

		DrawLoadedFileInfo();

		//DrawPreview();

		lui.DrawFooter();
		ImGui.EndGroup();
	}
	private void DrawImportControls() {
		using(ImRaii.Disabled(offsetsString == null)) {
			if(lui.BtnIcon(FontAwesomeIcon.ExclamationTriangle, "WAOFSetOffsets", uis.BtnSmall, "Set offsets") && offsetsString != null) {
				ctx.LazyExtras.actors.ImportOffset(offsetsString);
				ctx.LazyExtras.actors.LoadScene(offsetLoadModeLocal);
			}
		}
	}
	private void DrawPreviewSelect() {
		ImGui.BeginGroup();
		if(ImGui.RadioButton("Import preview", previewImport == true))
			previewImport = true;
		if(ImGui.RadioButton("Export preview", previewImport == false))
			previewImport = false;
		ImGui.EndGroup();
	}
	private void DrawPreview() {
		ImGui.Dummy(new(0, uis.Space));
		if(previewImport) 
			DrawPreviewTable("Importing:", offsets.Actors);
		else 
			DrawPreviewTable("Exporting:", ctx.LazyExtras.actors.aof.Actors);
	}
	private void DrawPreviewTable(string tableHeader, List<LazyActorOffsetElement> actors) {
		ImGui.Text(tableHeader);
		ImGui.Text("Scene target:");
		ImGui.SameLine();
		ImGui.SetCursorPosX(uis.SidebarW/3);
		ImGui.Text("File target:");
		ImGui.SameLine();
		ImGui.SetCursorPosX(2*uis.SidebarW/3);

		ImGui.NewLine();
		if (actors.Any())
			foreach (var actor in ctx.LazyExtras.actors.aof.Actors)
				DrawActorLine(actor.Name, actor.Name, actor.Position);
	}
	private void DrawActorLine(string sceneActor, string fileActor, Vector3 actorPos) {
		ImGui.Text(sceneActor);
		ImGui.SameLine();
		ImGui.SetCursorPosX(uis.SidebarW/3);
		ImGui.Text(fileActor);
		ImGui.SameLine();
		ImGui.SetCursorPosX(2*uis.SidebarW/3);
		ImGui.Text(actorPos.ToString());
	}
	private void DrawExportControls() {
		ImGui.BeginGroup();
		//if(lui.BtnIcon(FontAwesomeIcon.ArrowsSpin, "WAOFRefreshExport", uis.BtnSmall, "Refresh export"))
		//	ctx.LazyExtras.actors.UpdateAOF();
		//ImGui.SameLine();
		if(lui.BtnIcon(FontAwesomeIcon.Save, "WAOFExportFile", uis.BtnSmall, "Export")) {
			dialogOpen = true;
			ctx.LazyExtras.actors.UpdateAOF();
			ctx.LazyExtras.io.SetSaveBuffer(ctx.LazyExtras.actors.ExportOffsets());
			ctx.LazyExtras.io.OpenOffsetSaveDialog((valid, res) => {
				if (valid) {
					dbg("Success: saving offsets.");
					//dbg(res);
				}
				dialogOpen = false;
				});
		}
		ImGui.EndGroup();
	}
	private void DrawLoadedFileInfo() {
		// show selected file
		if(loadedOffsetFileName != null) {
			ImGui.BeginGroup();
			ImGui.Text(loadedOffsetFileName);
			//ImGui.Text(loadedOffsetDirFriendly);
			ImGui.SameLine();
			ImGui.Text($"Actors: {ctx.LazyExtras.actors.aof.Actors.Count}");
			ImGui.Text(string.Join(" | ", ctx.LazyExtras.actors.aof.Actors.Select(a => a.Name)));
			ImGui.EndGroup();
		}
	}
	private void DrawFileOpenPicker() {
		if(lui.BtnIcon(FontAwesomeIcon.FolderOpen, "WAOFLoadActorOffsets", uis.BtnSmall, "Load offsets")) {
			dialogOpen = true;
			ctx.LazyExtras.io.OpenOffsetDialog((valid, res) => {
				if (valid) {
					dbg("Success: reading offsets.");
					var _ = ctx.LazyExtras.io.ReadFile(res[0]);

					if(_ != null) {
						offsetsString = _.Value.data;
						loadedOffsetFileName = _.Value.filename;
						loadedOffsetPoseDir = _.Value.dir;
						loadedOffsetDirFriendly = _.Value.dirname;

						ctx.LazyExtras.actors.ImportOffset(offsetsString);
						offsets = ctx.LazyExtras.actors.aof;
					}

				}
				dialogOpen = false;
				});
		}
	}
	private void dbg(string s) => Ktisis.Log.Debug($"ActorOffsetsWidget: {s}");
}
