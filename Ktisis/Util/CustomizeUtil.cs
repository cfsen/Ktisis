﻿using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Enums;

using Lumina.Excel.GeneratedSheets;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Data;

namespace Ktisis.Util {
	internal class CustomizeUtil {
		public Ktisis Plugin;
		public DataManager Data;

		public CharaMakeType? Cached;
		public Dictionary<MenuType, List<MenuOption>>? CachedMenu;

		public CustomizeUtil(Ktisis plugin) {
			Plugin = plugin;
			Data = plugin.DataManager;
		}

		// Calculate row index

		public uint GetMakeIndex(Customize custom) {
			var r = (uint)custom.Race;
			var t = (uint)custom.Tribe;
			var g = (uint)custom.Gender;
			var i = Customize.GetRaceTribeIndex(custom.Race);
			return ((r - 1) * 4) + ((t - i) * 2) + g; // Thanks cait
		}

		// Fetch char creator data from cache or sheet

		public CharaMakeType? GetMakeData(uint index) {
			if (Cached != null && Cached.RowId == index) {
				return Cached;
			} else {
				var lang = Plugin.Configuration.SheetLocale;
				var sheet = Data.GetExcelSheet<CharaMakeType>(lang);
				var row = sheet == null ? null : sheet.GetRow(index);
				Cached = row;
				return row;
			}
		}

		public CharaMakeType? GetMakeData(Customize custom) {
			var index = GetMakeIndex(custom);
			return GetMakeData(index);
		}

		// Build char creator options

		public Dictionary<MenuType, List<MenuOption>> GetMenuOptions(Customize custom) {
			var options = new Dictionary<MenuType, List<MenuOption>>();

			var index = GetMakeIndex(custom);
			if (Cached != null && CachedMenu != null && index == Cached.RowId)
				return CachedMenu;

			var data = GetMakeData(index);
			if (data != null) {
				var menu = new CharaMakeIterator(data);
				if (menu != null) {
					for (int i = 0; i < CharaMakeIterator.Count; i++) {
						var val = menu[i];
						if (val.Index == 0)
							break;

						var type = val.Type;
						if (type == MenuType.Unknown1)
							type = MenuType.Color;
						if (type == MenuType.Color)
							continue;

						if (!options.ContainsKey(type))
							options[type] = new();

						var opt = new MenuOption(val);
						options[type].Add(opt);

						var next = menu[i + 1];
						if (next.Type == MenuType.Color)
							opt.Color = next;
					}
				}
			}

			CachedMenu = options;
			return options;
		}
	}

	public class CharaMakeIterator : IEnumerable {
		public const int Count = 28;

		public CharaMakeType Make;

		public CharaMakeIterator(CharaMakeType make) {
			Make = make;
		}

		public CharaMakeOption GetMakeOption(int i) {
			return new CharaMakeOption() {
				Name = Make.Menu[i].Value!.Text,
				Default = Make.InitVal[i],
				Type = (MenuType)Make.SubMenuType[i],
				Index = (CustomizeIndex)Make.Customize[i],
				Count = Make.SubMenuNum[i]
			};
		}

		public CharaMakeOption this[int index] {
			get => GetMakeOption(index);
			set => new NotImplementedException();
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < 28; i++)
				yield return this[i];
		}
	}

	internal struct MenuOption {
		internal CharaMakeOption Option;
		internal CharaMakeOption? Color;

		public MenuOption(CharaMakeOption option) {
			Option = option;
			Color = null;
		}
	}
}