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

namespace Ktisis.Interface.Windows;

public class LazyImgui : KtisisWindow {
	private readonly IEditorContext ctx;
	private LazyUi lui;
	private Vector2 ScreenDimensions;

	private bool Pinned = true;
	private bool Hidden = false;

	private float gs;
	private Vector2 UiPosition;
	private Vector2 UiSize;
	private LazyUiSizes uis;
	private List<ILazyWidget> Widgets;

	public LazyImgui(IEditorContext ctx) : base("LazyImGui", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize) {
		this.ctx = ctx;

		this.lui = new();
		this.uis = lui.uis;
		this.gs = lui.uis.Scale;

		this.SetScreenDimensionLimits();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = uis.BtnBig*gs,
			MaximumSize = new(this.ScreenDimensions.X*gs*1/7,this.ScreenDimensions.Y)
		};
		//this.Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
		this.uis = new LazyUiSizes();
		this.Widgets = new();
		this.Initialize(this.ctx);
		this.SetShowUi();
	}

	// Initialize widgets

	private void Initialize(IEditorContext ctx) {
		this.Widgets = [
			//new DemoWidget(),
			new TransformWidget(ctx),
			new ActorSelectWidget(ctx),
			new WindowsWidget(ctx)
			];
	}

	// Draw imgui

	public override void PreOpenCheck() {
		if (this.ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}
	public override void PreDraw() {
		using var _ = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 0.0f);
		this.Position = this.UiPosition;
		this.Size = this.UiSize;
	}
	public override void Draw() {
		switch(this.Hidden) {
			case false:
				this.DrawWorkspace();
				this.DrawWidgets();
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
		this.UiSize = new Vector2(ScreenDimensions.X*gs*1/7, ScreenDimensions.Y);
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
		Vector2 curp = new((uis.BtnBig.X+5*uis.Space.X), uis.Space.Y);
		ImGui.SetCursorPos(curp);
		// TODO doesn't color as expected
		//using (ImRaii.PushColor(ImGuiCol.Button, ctx.Posing.IsEnabled ? 0x00591414 : 0xFF7070C0)) { 
		if (ImGui.Button(ctx.Posing.IsEnabled ? "End posing" : "Start posing", new((ScreenDimensions.X/7)-uis.BtnBig.X+5*uis.Space.X, uis.BtnSmall.Y)))
			ctx.Posing.SetEnabled(!ctx.Posing.IsEnabled);
		//}

		curp.Y += uis.BtnSmall.Y + uis.Space.Y;
		ImGui.SetCursorPos(curp);
		if(lui.BtnIcon(FontAwesomeIcon.CloudSunRain, "EnvSettings", uis.BtnSmall, "Time and day settings"))
			dp("env settings");

		curp.X += uis.BtnSmall.X+uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.Cog, "Settings", uis.BtnSmall, "Settings"))
			dp("Settings");

		curp.X += uis.BtnSmall.X+5*uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.SearchPlus, "IncreaseUiScaling", uis.BtnSmall, "Increase UI scale"))
			dp("Increase ui scaling");

		curp.X += uis.BtnSmall.X+uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.SearchMinus, "DecreaseUiScaling", uis.BtnSmall, "Decrease UI scale"))
			dp("decrease ui scaling");

		curp.X += uis.BtnSmall.X+5*uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.Undo, "Undo", uis.BtnSmall, "Undo"))
			this.ctx.Actions.History.Undo();
		
		curp.X += uis.BtnSmall.X+uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lui.BtnIcon(FontAwesomeIcon.Redo, "Redo", uis.BtnSmall, "Redo"))
			this.ctx.Actions.History.Redo();

	}

	// Utilities

	private static void dp(string s) {
		Ktisis.Log.Debug(s);
	}
}
