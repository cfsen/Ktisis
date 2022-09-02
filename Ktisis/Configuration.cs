﻿using System;

using Dalamud;
using Dalamud.Configuration;

using Ktisis.Localization;

namespace Ktisis {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;

		// Interface

		public bool AutoOpen { get; set; } = true;

		public bool DisplayCharName = true;

		// Overlay

		public bool DrawLinesOnSkeleton { get; set; } = true;

		// Gizmo

		public bool AllowAxisFlip { get; set; } = true;

		// Language

		public UserLocale Localization { get; set; } = UserLocale.English;
		public ClientLanguage SheetLocale { get; set; } = ClientLanguage.English;

		public bool TranslateBones = true;

		// UI memory

		public bool ShowSkeleton { get; set; } = false;

		// save

		public void Save(Ktisis plugin) {
			plugin.PluginInterface.SavePluginConfig(this);
		}
	}
}