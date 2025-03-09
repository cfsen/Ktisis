using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

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
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		if(dialogOpen)
			ctx.LazyExtras.io.DrawDialog();
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.SquarePersonConfined, 
			$"Pose loading: {ctx.LazyExtras.SelectedActor?.Name ?? "No target"}");
		
		DrawFilePicker();
		DrawImportToggles();
		ImGui.Dummy(new(0, uis.Space));
		DrawApplyBtn();

		lui.DrawFooter();
		ImGui.EndGroup();
	}
	private void DrawFilePicker() {
		// 1st milestone
		// show button to open dialog
		if(lui.BtnIcon(FontAwesomeIcon.FolderOpen, "WLPLoadPoseBtn", uis.BtnSmall, "Load pose")) {
			dialogOpen = true;
			ctx.LazyExtras.io.OpenPoseDialog((valid, res) => {
				if (valid) {
					dbg(res?.FirstOrDefault()?.ToString() ?? "no res :(");
					JsonFileSerializer jfs = new();
					if(jfs.Deserialize<PoseFile>(ctx.LazyExtras.io.LoadFileData()) is PoseFile pf) {
						loadedPose = pf;
						loadedPoseName = ctx.LazyExtras.io.LoadedFileName();
						loadedPoseDir = ctx.LazyExtras.io.LastLoadDirectory();
						loadedPoseDirFriendly = ctx.LazyExtras.io.LastLoadDirectory(true);
					}
				} 
				dialogOpen = false;
				});
		}
		ImGui.SameLine();
		// show selected file
		if(loadedPoseName != null) {
			ImGui.BeginGroup();
			ImGui.Text(loadedPoseName);
			ImGui.Text(loadedPoseDirFriendly);
			ImGui.EndGroup();
		}
			// 2nd milestone: move to advanced loading probably
			// list other files in directory?
			// list recent files?
			// next/previous file button?


	}
	private void DrawImportToggles() {
		if(ctx.LazyExtras.SelectedActor is not ActorEntity ae) { 
			ImGui.Text("No actor selected.");
			return;
		}

		var scc = ae.Recurse().Where(child => child is SkeletonNode && child.IsSelected).Count();
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
	private void DrawApplyBtn() {
		if(ctx.LazyExtras.SelectedActor != null && ctx.LazyExtras.SelectedActor.Pose != null && loadedPose != null) {
			ImGui.SetCursorPosX(uis.SidebarW/3);
			if(lui.Btn("Apply", "WPLApplyPoseBtn", new(200, uis.BtnSmall.Y), "Apply the selected pose")) {
				ctx.Posing.ApplyPoseFile(
					ctx.LazyExtras.SelectedActor.Pose,
					loadedPose,
					ctx.Config.File.ImportPoseModes,
					ctx.Config.File.ImportPoseTransforms,
					selectedBones > 0 ? true : false,
					ctx.Config.File.AnchorPoseSelectedBones,
					ctx.Config.File.AnchorPoseSelectedBonesRotate
					);
			}
		}
	}
	private void DrawAdvancedLoading() {
		// new features here
	}
	private void dbg(string s) => Ktisis.Log.Debug($"PoseLoadWidget: {s}");
}
