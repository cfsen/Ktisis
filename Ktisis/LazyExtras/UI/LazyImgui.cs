using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Components.Workspace;

using Ktisis.LazyExtras;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.LazyExtras.UI;
using Ktisis.LazyExtras.UI.Widgets;

using System;
using System.Numerics;
using System.Collections.Generic;
using Ktisis.Core.Attributes;
using Dalamud.Plugin.Services;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using Ktisis.Services.Plugin;

namespace Ktisis.Interface.Windows;

[Singleton]
public class LazyImgui : KtisisWindow {
	private readonly IEditorContext ctx;
	private readonly ITextureProvider tex;

	private LazyUi lui;
	private Vector2 ScreenDimensions;

	private bool Pinned = true;
	private bool Hidden = false;

	private Vector2 UiPosition;
	private Vector2 UiSize;
	private LazyUiSizes uis;
	private List<ILazyWidget> Widgets;

	private LazyWidgetCat widgetFilter;

	public LazyImgui(IEditorContext ctx, ITextureProvider tex) 
		: base("LazyImGui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize) {
		this.ctx = ctx;
		this.tex = tex;

		this.lui = new();
		this.uis = lui.uis;

		this.SetScreenDimensionLimits();
		this.UpdateSidebarSize();
		this.uis = new LazyUiSizes();
		this.Widgets = [];
		this.Initialize();
		this.SetShowUi();
		
		//this.widgetFilter = LazyWidgetCat.Transformers ^ LazyWidgetCat.Pose ^ LazyWidgetCat.Light ^ LazyWidgetCat.Camera;
		this.widgetFilter = LazyWidgetCat.Transformers ^ LazyWidgetCat.Pose ^ LazyWidgetCat.Light ^ LazyWidgetCat.Camera ^ LazyWidgetCat.Misc;
	}

	// Initialize widgets

	private void Initialize() {
		this.Widgets = [
			//new DbgWidget(ctx),
			new SceneWidget(ctx),
			new PenumbraWidget(ctx),
			new ActorOffsetWidget(ctx),
			new PoseFaceWidget(ctx, tex),
			new PoseWidget(ctx),
			new LightsWidget(ctx),
			new CameraWidget(ctx),
			new TransformWidget(ctx),
			new PoseLoadWidget(ctx),
			new NodeSelectWidget(ctx),
			new WindowsWidget(ctx)
			];
	}

	private void UpdateSidebarSize() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = uis.BtnBig*uis.Scale,
			MaximumSize = new(uis.SidebarW,uis.ScreenDimensions.Y)
		};
	}
	// Draw imgui

	public override void PreOpenCheck() {
		if (this.ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}
	public override void PreDraw() {
		using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 0.0f); // TODO doesnt work
		this.Position = this.UiPosition;
		this.Size = this.UiSize;
		if(this.uis.RefreshScale())
			this.UpdateSidebarSize();
	}
	public override void Draw() {
		switch(this.Hidden) {
			case false:
				this.DrawWorkspace();
				this.DrawWidgetSelector();
				ImGui.BeginChild("ScrollingRegion", new(uis.SidebarW-2*uis.Space, uis.ScreenDimensions.Y-uis.BtnBig.Y-1*uis.BtnSmall.Y-5*uis.Space), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
				this.DrawWidgets();
				ImGui.EndChild();
				break;
			case true: {
				using var _wp = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
				using var _fp = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
				using var _ip = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
				
				this.DrawBtnToggleUi();
				break;
			}
		}
	}
	private void DrawWidgets() {
		var filter = this.Widgets.Where(x => (x.Category & this.widgetFilter) == 0).ToList();
		if(filter == null) return;
		if(filter.Count == 0) return;
		foreach(ILazyWidget w in filter) {
			ImGui.Spacing();
			ImGui.Separator();
			w.UpdateScaling();
			w.Draw();
		}
	}

	// Logic

	private void SetHideUi() {
		this.Hidden = true;
		this.UiSize = new Vector2(85, 85);
		this.UiPosition = new Vector2(0, 0);
	}
	private void SetShowUi() {
		this.SetScreenDimensionLimits();
		this.Hidden = false;
		this.UiSize = new Vector2(ScreenDimensions.X*uis.SidebarFactor, ScreenDimensions.Y);
		this.UiPosition = new Vector2(0, 0);
	}
	private unsafe void SetScreenDimensionLimits() {
		Device* dev = Device.Instance();
		this.ScreenDimensions.X = dev->Width;
		this.ScreenDimensions.Y = dev->Height;
	}

	// Draw components

	private void DrawWorkspace() {
		this.DrawBtnToggleUi();
		this.DrawHeader();
	}
	private void DrawBtnToggleUi() {
		if(lui.BtnIcon(FontAwesomeIcon.Camera, "LazyShowUi", uis.BtnBig, Hidden ? "Show ktisis" : "Hide ktisis")) {
			this.Flags ^= ImGuiWindowFlags.NoScrollbar;
			if(this.Hidden) this.SetShowUi();
			else this.SetHideUi();
		}
	}
	private void DrawHeader() {

		ImGui.SameLine();
		ImGui.SetCursorPosX(uis.BtnBig.X+21);
		ImGui.BeginGroup();
		// TODO color for pose button
		if (ImGui.Button(ctx.Posing.IsEnabled ? "End posing" : "Start posing", new((uis.SidebarW)-uis.BtnBig.X-5*uis.Space-10, uis.BtnSmall.Y)))
			ctx.Posing.SetEnabled(!ctx.Posing.IsEnabled);

		ImGui.BeginGroup();
		if(lui.BtnIcon(FontAwesomeIcon.CloudSunRain, "EnvSettings", uis.BtnSmall, "Time and day settings"))
			this.ctx.Interface.ToggleEnvironmentWindow();

		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Cog, "Settings", uis.BtnSmall, "Settings"))
			this.ctx.Interface.ToggleConfigWindow();

		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.SearchPlus, "IncreaseUiScaling", uis.BtnSmall, "Increase UI scale"))
			dbg("Increase ui scaling");

		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.SearchMinus, "DecreaseUiScaling", uis.BtnSmall, "Decrease UI scale"))
			dbg("decrease ui scaling");

		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Undo, "Undo", uis.BtnSmall, "Undo"))
			this.ctx.Actions.History.Undo();
		
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Redo, "Redo", uis.BtnSmall, "Redo"))
			this.ctx.Actions.History.Redo();
		ImGui.EndGroup();
		ImGui.EndGroup();
		ImGui.Dummy(new(0, uis.Space));

	}
	private void DrawWidgetSelector() {
		// TODO pending UX collection
		//if (lui.BtnIcon(FontAwesomeIcon.Brain, "WMContext", uis.BtnSmall, "Smart widgets"))
		//	dbg("Contextual");
		//ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.ArrowsAlt, "WMTransformers", uis.BtnSmall, "Transform widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Transformers), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Transformers;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.UserCircle, "WMPortrait", uis.BtnSmall, "Portrait widgets", 
			widgetFilter.HasFlag(LazyWidgetCat.Pose), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Pose;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.PersonRays, "WMGesture", uis.BtnSmall, "Gesture widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Gesture), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Gesture;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.CameraRetro, "WMCamera", uis.BtnSmall, "Camera widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Camera), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Camera;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.Lightbulb, "WMLights", uis.BtnSmall, "Lights widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Light), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Light;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.ObjectUngroup, "WMSelection", uis.BtnSmall, "Selection widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Selection), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Selection;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.ObjectGroup, "WMAPI", uis.BtnSmall, "Scene widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Scene), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Scene;
		ImGui.SameLine();
		if (lui.BtnIconState(FontAwesomeIcon.EllipsisH, "WMMisc", uis.BtnSmall, "Misc widgets",
			widgetFilter.HasFlag(LazyWidgetCat.Misc), 0xFF545253, 0xFF003300))
			widgetFilter ^= LazyWidgetCat.Misc;
		//lui.DrawHeader(FontAwesomeIcon.Crosshairs, (ctx.LazyExtras.SelectedActor?.Name ?? "No target"));
	}

	// Utilities

	private void dbg(string s) => Ktisis.Log.Debug($"LazyImgui: {s}");
}
