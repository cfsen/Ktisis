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
		
	}

	// Initialize widgets

	private void Initialize() {
		this.Widgets = [
			//new DemoWidget(),
			new PoseFaceWidget(ctx, tex),
			new LightsWidget(ctx),
			new CameraWidget(ctx),
			new TransformWidget(ctx),
			new ActorSelectWidget(ctx),
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
				ImGui.BeginChild("ScrollingRegion", new(uis.SidebarW-2*uis.Space, uis.ScreenDimensions.Y-uis.BtnBig.Y-uis.BtnSmall.Y-4*uis.Space), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
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
		foreach(ILazyWidget w in this.Widgets) {
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
		//using var _f = ImRaii.PushFont(UiBuilder.IconFont);
		ImGui.SetCursorPos(new(5,5));
		//if(ImGui.Button(FontAwesomeIcon.Camera.ToIconString() + "###LazyShowUi", uis.BtnBig)) {
		if(lui.BtnIcon(FontAwesomeIcon.Camera, "LazyShowUi", uis.BtnBig, Hidden ? "Show ktisis" : "Hide ktisis")) {
			this.Flags ^= ImGuiWindowFlags.NoScrollbar;
			if(this.Hidden) this.SetShowUi();
			else this.SetHideUi();
		}
	}
	private void DrawHeader() {
		Vector2 curp = new((uis.BtnBig.X+2*uis.Space), uis.Space);
		ImGui.SetCursorPos(curp);
		// TODO doesn't color as expected
		//using (ImRaii.PushColor(ImGuiCol.Button, ctx.Posing.IsEnabled ? 0x00591414 : 0xFF7070C0)) { 
		if (ImGui.Button(ctx.Posing.IsEnabled ? "End posing" : "Start posing", new((uis.SidebarW)-uis.BtnBig.X-4*uis.Space, uis.BtnSmall.Y)))
			ctx.Posing.SetEnabled(!ctx.Posing.IsEnabled);
		//}

		curp.Y += uis.BtnSmall.Y + uis.Space;
		ImGui.SetCursorPos(curp);
		if(lui.BtnIcon(FontAwesomeIcon.CloudSunRain, "EnvSettings", uis.BtnSmall, "Time and day settings"))
			dbg("env settings");

		curp.X += uis.BtnSmall.X+uis.Space;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.Cog, "Settings", uis.BtnSmall, "Settings"))
			dbg("Settings");

		curp.X += uis.BtnSmall.X+3*uis.Space;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.SearchPlus, "IncreaseUiScaling", uis.BtnSmall, "Increase UI scale"))
			dbg("Increase ui scaling");

		curp.X += uis.BtnSmall.X+uis.Space;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.SearchMinus, "DecreaseUiScaling", uis.BtnSmall, "Decrease UI scale"))
			dbg("decrease ui scaling");

		curp.X += uis.BtnSmall.X+3*uis.Space;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.Undo, "Undo", uis.BtnSmall, "Undo"))
			this.ctx.Actions.History.Undo();
		
		curp.X += uis.BtnSmall.X+uis.Space;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.Redo, "Redo", uis.BtnSmall, "Redo"))
			this.ctx.Actions.History.Redo();

	}
	private void DrawWidgetSelector() {
		if (lui.BtnIcon(FontAwesomeIcon.Brain, "WMContext", uis.BtnSmall, "Smart widgets"))
			dbg("Contextual");
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.PersonRays, "WMGesture", uis.BtnSmall, "Gesture widgets"))
			dbg("Gesture");
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.UserCircle, "WMPortrait", uis.BtnSmall, "Portrait widgets"))
			dbg("Portrait");
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.CameraRetro, "WMCamera", uis.BtnSmall, "Camera widgets"))
			dbg("Camera");
		ImGui.SameLine();
		if (lui.BtnIcon(FontAwesomeIcon.Lightbulb, "WMLights", uis.BtnSmall, "Lights widgets"))
			dbg("Lights");
	}

	// Utilities

	private void dbg(string s) => Ktisis.Log.Debug(s);
}
