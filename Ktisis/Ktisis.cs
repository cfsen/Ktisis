﻿using System.Numerics;

using ImGuiNET;

using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;

using Ktisis.Overlay;
using Ktisis.Interface;
using Ktisis.Localization;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public Configuration Configuration { get; init; }

		internal KtisisGui Gui { get; init; }
		internal ConfigGui ConfigGui { get; init; }
		internal CustomizeGui CustomizeGui { get; init; }
		internal SkeletonEditor SkeletonEditor { get; init; }

		internal Locale Locale { get; init; }

		internal DalamudPluginInterface PluginInterface { get; init; }
		internal CommandManager CommandManager { get; init; }
		internal DataManager DataManager { get; init; }
		internal ClientState ClientState { get; init; }
		internal ObjectTable ObjectTable { get; init; }
		internal SigScanner SigScanner { get; init; }
		internal GameGui GameGui { get; init; }

		public Ktisis(
			DalamudPluginInterface pluginInterface,
			CommandManager cmdManager,
			DataManager dataManager,
			ClientState clientState,
			ObjectTable objTable,
			SigScanner sigScanner,
			GameGui gameGui
		) {
			// TODO: Streamline this.
			PluginInterface = pluginInterface;
			CommandManager = cmdManager;
			DataManager = dataManager;
			ClientState = clientState;
			ObjectTable = objTable;
			SigScanner = sigScanner;
			GameGui = gameGui;

			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			// Register command

			CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "/ktisis - Show the Ktisis interface."
			});

			// i18n

			Locale = new Locale(this);

			// Overlays & UI

			Gui = new KtisisGui(this);
			ConfigGui = new ConfigGui(this);
			CustomizeGui = new CustomizeGui(this);
			SkeletonEditor = new SkeletonEditor(this, null);

			Gui.Show();

			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += Draw;
		}

		public void Dispose() {
			// TODO
			CommandManager.RemoveHandler(CommandName);
		}

		private void OnCommand(string command, string arguments) {
			Gui.Show();
		}

		public unsafe void Draw() {
			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			var draw = ImGui.GetWindowDrawList();

			Gui.Draw();
			ConfigGui.Draw();
			CustomizeGui.Draw();

			SkeletonEditor.Draw(draw);

			ImGui.End();
			ImGui.PopStyleVar();
		}

		public bool IsInGpose() {
			return PluginInterface.UiBuilder.GposeActive;
		}
	}
}
