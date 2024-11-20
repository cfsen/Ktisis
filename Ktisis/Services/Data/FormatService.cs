﻿using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;

using Lumina.Excel.Sheets;

namespace Ktisis.Services.Data;

[Singleton]
public class FormatService {
	private readonly IClientState _client;
	private readonly IDataManager _data;
	
	public FormatService(
		IClientState client,
		IDataManager data
	) {
		this._client = client;
		this._data = data;
	}

	public string Replace(string path) {
		foreach (var key in this.ReplacerKeys.Where(path.Contains)) {
			var value = this.GetKeyReplacement(key);
			do {
				path = path.Replace(key, value, true, null);
			} while (path.Contains(key));
		}
		return path;
	}
	
	// Replacements
	
	private readonly List<string> ReplacerKeys = [ "%Date%", "%Year%", "%Month%", "%Day%", "%Time%", "%PlayerName%", "%CurrentWorld%", "%HomeWorld%", "%Zone%" ];

	public Dictionary<string, string> GetReplacements() => this.ReplacerKeys.ToDictionary(key => key, this.GetKeyReplacement);
	
	private string GetKeyReplacement(string key) => key switch {
		"%Date%" => DateTime.Now.ToString("yyyy-MM-dd"),
		"%Year%" => DateTime.Now.ToString("yyyy"),
		"%Month%" => DateTime.Now.ToString("MM"),
		"%Day%" => DateTime.Now.ToString("dd"),
		"%Time%" => DateTime.Now.ToString("hh-mm-ss"),
		"%PlayerName%" => this.GetPlayerName(),
		"%CurrentWorld%" => this.GetCurrentWorld(),
		"%HomeWorld%" => this.GetHomeWorld(),
		"%Zone%" => this.GetZone(),
		_ => string.Empty
	};

	private string GetPlayerName() {
		return this._client.LocalPlayer?.Name.ToString() ?? "Unknown";
	}

	private string GetCurrentWorld() {
		return this._client.LocalPlayer?.CurrentWorld.Value.Name.ToString() ?? "Unknown";
	}
	
	private string GetHomeWorld() {
		return this._client.LocalPlayer?.HomeWorld.Value.Name.ToString() ?? "Unknown";
	}

	private string GetZone() {
		var rowId = this._client.TerritoryType;
		var sheet = this._data.GetExcelSheet<TerritoryType>();
		if (sheet.HasRow(rowId) && sheet.GetRow(rowId).PlaceName is { IsValid: true } placeName)
			return placeName.Value.Name.ExtractText();
		return "Unknown";
	}
}
