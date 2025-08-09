using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.LazyExtras.UI.Controls;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class PoseLoadWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	private FileNavigator fn;

	private bool dialogOpen = false;
	private PoseFile? loadedPose = null;
	private string? loadedPoseName = "";
	private string? loadedPoseDir = "";
	private string? loadedPoseDirFriendly = "";
	private int? selectedBones = null;


	public PoseLoadWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Selection;	// Currently UX testing
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
		this.fn = new(ctx.LazyExtras.io, lui, uis, ".pose");
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		if(dialogOpen)
			ctx.LazyExtras.io.DrawDialog();
		if(fn.StateChanged)
			HandleFileNavState();

		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.SquarePersonConfined, 
			$"Pose loading: {ctx.LazyExtras.SelectedActor?.Name ?? "No target"}");
		
		//DrawFilePicker();
		DrawImportExportControls();
		DrawImportToggles();
		ImGui.Dummy(new(0, uis.Space));
		DrawApplyBtn();

		lui.DrawFooter();
		ImGui.EndGroup();
	}

	// Dialog handling

	private (LazyIOFlag, string) IODataDispatcher() {
		return (LazyIOFlag.Save | LazyIOFlag.Pose, "");
	}
	private void IODataReceiver(bool success, List<string>? data) {
		if(!success) return;
		if(data == null) return;

		// 0: Data, 1: file name, 2: path to directory, 3: directory name
		JsonFileSerializer jfs = new();
		if(jfs.Deserialize<PoseFile>(data[0]) is PoseFile pf) {

			loadedPose = pf;
			loadedPoseName = data[1];
			loadedPoseDir = data[2];
			loadedPoseDirFriendly = data[3];

			fn.UpdateState(loadedPoseDir, loadedPoseName);
		}
	}

	// UI elements

	private void DrawImportExportControls() {
		using(ImRaii.Disabled(!ctx.Posing.IsEnabled || ctx.LazyExtras.SelectedActor == null)) {
			fn.Draw();
		}

		//lui.BtnSave(IODataDispatcher, "WPOSELOAD_Dispathcer", "Save", ctx.LazyExtras.io);
		ImGui.SameLine();
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Pose, IODataReceiver, "WPOSELOAD_Receiver", "Load", ctx.LazyExtras.io);

		if(loadedPoseName != null) {
			ImGui.SameLine();
			ImGui.Text(loadedPoseDirFriendly);
			ImGui.BeginGroup();
			fn.DrawCyclePosition();
			ImGui.SameLine();
			ImGui.Text(loadedPoseName);
			ImGui.EndGroup();
		}
	}
	private void DrawImportToggles() {
		int scc = 0;
		if(ctx.LazyExtras.SelectedActor is ActorEntity ae)  
			scc = ae.Recurse().Where(child => child is SkeletonNode && child.IsSelected).Count();

		using(ImRaii.Disabled(ctx.LazyExtras.SelectedActor is null)){

			PoseTransforms xfmflags = ctx.Config.File.ImportPoseTransforms;
			PoseMode mflags = ctx.Config.File.ImportPoseModes;

			bool rot = xfmflags.HasFlag(PoseTransforms.Rotation);
			bool pos = xfmflags.HasFlag(PoseTransforms.Position);
			bool sca = xfmflags.HasFlag(PoseTransforms.Scale);

			bool mbody = mflags.HasFlag(PoseMode.Body);
			bool mface = mflags.HasFlag(PoseMode.Face);

			if(ImGui.Checkbox("Rotation##WPoseLoadRotation",	ref rot)) xfmflags ^= PoseTransforms.Rotation;
			ImGui.SameLine();
			ImGui.SetCursorPosX(uis.SidebarW/3);
			if(ImGui.Checkbox("Position##WPoseLoadPosition",	ref pos)) xfmflags ^= PoseTransforms.Position;
			ImGui.SameLine();
			ImGui.SetCursorPosX(2*uis.SidebarW/3);
			if(ImGui.Checkbox("Scale##WPoseLoadScale",			ref sca)) xfmflags ^= PoseTransforms.Scale;

			using(ImRaii.Disabled(scc > 0)) {
				if(ImGui.Checkbox("Body##WPoseLoadBody",		ref mbody)) mflags ^= PoseMode.Body;
				ImGui.SameLine();
				ImGui.SetCursorPosX(uis.SidebarW/3);
				if(ImGui.Checkbox("Face##WPoseLoadFace",		ref mface)) mflags ^= PoseMode.Face;
			}

			using(ImRaii.Disabled(scc == 0 || !pos)) {
				ImGui.Checkbox("Anchor group##WPAnchorGrp",		ref ctx.Config.File.AnchorPoseSelectedBones);
				ImGui.SameLine();
				ImGui.SetCursorPosX(uis.SidebarW/3);
				ImGui.Checkbox("Restore rotation##WPARestRot",	ref ctx.Config.File.AnchorPoseSelectedBonesRotate);
			}

			ctx.Config.File.ImportPoseModes			= mflags;
			ctx.Config.File.ImportPoseTransforms	= xfmflags;
			selectedBones = scc;
			// For testing
		}
	}
	private void DrawApplyBtn() {
		using(ImRaii.Disabled(ctx.LazyExtras.SelectedActor == null || ctx.LazyExtras.SelectedActor.Pose == null || loadedPose == null)) {
			ImGui.SetCursorPosX(uis.SidebarW/3);
			if(lui.Btn("Apply", "WPLApplyPoseBtn", new(200, uis.BtnSmall.Y), "Apply the selected pose"))
				OnClickApplyBtn();
		}
	}
	private void HandleFileNavState() {
		fn.StateChanged = false;
		if(fn.FilePath == null) return;

		var jsondata = ctx.LazyExtras.io.ReadFile(fn.FilePath) ?? null;
		if(jsondata == null) return;

		JsonFileSerializer jfs = new();
		if (jfs.Deserialize<PoseFile>(jsondata.Value.data) is PoseFile pf) {
			loadedPose = pf;
			loadedPoseName = fn.FileNameFriendly;
		}

		OnClickApplyBtn();
	}
	private async void OnClickApplyBtn() {
		if(ctx.LazyExtras.SelectedActor?.Pose == null) return;
		if(loadedPose == null) return;

		await ctx.LazyExtras.fw.RunOnFrameworkThread( () => { 
			ctx.Posing.ApplyPoseFile(
				ctx.LazyExtras.SelectedActor.Pose,
				loadedPose,
				ctx.Config.File.ImportPoseModes,
				ctx.Config.File.ImportPoseTransforms,
				selectedBones > 0 ? true : false,
				ctx.Config.File.AnchorPoseSelectedBones,
				ctx.Config.File.AnchorPoseSelectedBonesRotate
			); 
		});
	}
	private void dbg(string s) => Ktisis.Log.Debug($"PoseLoadWidget: {s}");
}
