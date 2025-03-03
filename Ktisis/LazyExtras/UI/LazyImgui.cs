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

using System;
using System.Numerics;
using System.Collections.Generic;
using Ktisis.LazyExtras.UI.Widgets;

namespace Ktisis.Interface.Windows;

public class LazyImgui : KtisisWindow {
	private readonly IEditorContext ctx;
	private LazyBase lb;
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
		this.lb = ctx.LazyExtras;

		this.uis = ctx.LazyExtras.Sizes;
		this.gs = ctx.LazyExtras.Sizes.Scale;

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
		using var _f = ImRaii.PushFont(UiBuilder.IconFont);
		ImGui.SetCursorPos(new(5,5));
		if(ImGui.Button(FontAwesomeIcon.Camera.ToIconString() + "###LazyShowUi", uis.BtnBig)) {
			this.Flags ^= ImGuiWindowFlags.NoScrollbar;
			if(this.Hidden) this.SetShowUi();
			else this.SetHideUi();
		}
	}
	private void DrawHeader() {
		Vector2 curp = new((uis.BtnBig.X+5*uis.Space.X), uis.Space.Y);
		ImGui.SetCursorPos(curp);
		if(ImGui.Button("Shift+Click to pose", new(ScreenDimensions.X/7-uis.BtnBig.X, uis.BtnSmall.Y)))
			dp("Pose mode");

		curp.Y += uis.BtnSmall.Y + uis.Space.Y;
		ImGui.SetCursorPos(curp);
		if(lb.BtnIcon(FontAwesomeIcon.CloudSunRain, "EnvSettings", uis.BtnSmall))
			dp("env settings");

		curp.X += uis.BtnSmall.X+uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lb.BtnIcon(FontAwesomeIcon.Cog, "Settings", uis.BtnSmall))
			dp("Settings");

		curp.X += uis.BtnSmall.X+5*uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lb.BtnIcon(FontAwesomeIcon.SearchPlus, "IncreaseUiScaling", uis.BtnSmall))
			dp("Increase ui scaling");

		curp.X += uis.BtnSmall.X+uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lb.BtnIcon(FontAwesomeIcon.SearchMinus, "DecreaseUiScaling", uis.BtnSmall))
			dp("decrease ui scaling");

		curp.X += uis.BtnSmall.X+5*uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lb.BtnIcon(FontAwesomeIcon.Undo, "Undo", uis.BtnSmall))
			this.ctx.Actions.History.Undo();
		
		curp.X += uis.BtnSmall.X+uis.Space.X;
		ImGui.SetCursorPos(curp);
		if (lb.BtnIcon(FontAwesomeIcon.Redo, "Redo", uis.BtnSmall))
			this.ctx.Actions.History.Redo();

	}

	// Utilities

	private static void dp(string s) {
		Ktisis.Log.Debug(s);
	}
}
