using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

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

	//private bool dialogOpen = false;
	private LazyActorOffsetFile offsets;
	private string? offsetsString;
	private string? loadedOffsetFileName;
	private string? loadedOffsetPoseDir;
	private string? loadedOffsetDirFriendly;
	//private bool previewImport = true;
	private bool offsetLoadModeLocal = true;

	public ActorOffsetWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Scene;
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
		//if(dialogOpen)
		//	ctx.LazyExtras.io.DrawDialog();

		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.PeopleGroup, "Actor offsets");

		DrawImportControls();
		ImGui.SameLine();
		DrawImportExportControls();
		ImGui.SameLine();
		ImGui.Checkbox("Local mode", ref offsetLoadModeLocal);
		DrawLoadedFileInfo();

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
	private void DrawLoadedFileInfo() {
		if(loadedOffsetFileName == null) return;

		ImGui.BeginGroup();
		ImGui.Text(loadedOffsetFileName);
		ImGui.SameLine();
		ImGui.Text($"Actors: {ctx.LazyExtras.actors.aof.Actors.Count}");
		ImGui.Text(string.Join(" | ", ctx.LazyExtras.actors.aof.Actors.Select(a => a.Name)));
		ImGui.EndGroup();
	}

	// Dialog handling

	private (LazyIOFlag, string) IODataDispatcher() {
		ctx.LazyExtras.actors.UpdateAOF();
		return (LazyIOFlag.Save | LazyIOFlag.Offset, ctx.LazyExtras.actors.ExportOffsets());
	}
	private void IODataReceiver(bool success, List<string>? data) {
		if(!success) return;
		if(data == null) return;

		// 0: Data, 1: file name, 2: path to directory, 3: directory name
		offsetsString = data[0];
		loadedOffsetFileName = data[1];
		loadedOffsetPoseDir = data[2];
		loadedOffsetDirFriendly = data[3];

		ctx.LazyExtras.actors.ImportOffset(offsetsString);
		offsets = ctx.LazyExtras.actors.aof;
	}
	private void DrawImportExportControls() {
		lui.BtnSave(IODataDispatcher, "WACTOFF_Dispathcer", "Save", ctx.LazyExtras.io);
		ImGui.SameLine();
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Offset, IODataReceiver, "WACTOFF_Receiver", "Load", ctx.LazyExtras.io);
	}
	private void dbg(string s) => Ktisis.Log.Debug($"ActorOffsetsWidget: {s}");
}
