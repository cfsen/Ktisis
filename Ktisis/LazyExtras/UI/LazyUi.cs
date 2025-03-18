﻿using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using ImGuiNET;

using Ktisis.LazyExtras.UI.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.UI;
class LazyUi {
	public LazyUiSizes uis;
	public LazyUi(){ 
		this.uis = new();
	}

	// Logic

	public void UpdateScaling(float newScaling){ 

	}

	// Widget utility

	public void DrawHeader(FontAwesomeIcon icon, string header) {
		ImGui.Dummy(new(0, uis.BtnSmaller.Y/5));
		ImGui.Spacing();
		using(ImRaii.PushFont(UiBuilder.IconFont)) {
			ImGui.Text(icon.ToIconString());
		}
		ImGui.SameLine();
		ImGui.Text(header);
		ImGui.Spacing();
	}
	public void DrawFooter() {
		ImGui.Dummy(new(0, uis.BtnSmaller.Y/2));
	}

	// Data presentation

	public void DrawPseudoTable(string[] content) {
		if(!(content.Length > 0)) return;
		for(int i = 0; i < content.Length; i++) {
			ImGui.Text(content[i]);
			ImGui.SameLine();
			ImGui.SetCursorPosX((i+1)*uis.SidebarW/content.Length);
		}
	}

	// Slider inputs

	public bool SliderTableRow(string s, ref Vector3 val, SliderFormatFlag type, ref bool stateChanged) {
		// TODO eh. might want to move responsibility for scaling to callers
		this.uis.RefreshScale();
		bool res = false;
		float speed = (type == SliderFormatFlag.Rotation)? 0.3f : 0.0015f; 
		ImGui.BeginTable($"###SliderTable{s}", 3, ImGuiTableFlags.NoBordersInBody, new(uis.SidebarW-3*uis.Space,20));
		ImGui.TableNextColumn();
		using(ImRaii.PushColor(ImGuiCol.FrameBg, 0xFF000044)) { 
			ImRaii.ItemWidth(uis.SidebarW/3-2*uis.Space);
			res |= ImGui.DragFloat($"###X{s}{type}", ref val.X, speed, 0, 0, SliderFormat(type), ImGuiSliderFlags.NoRoundToFormat);
			stateChanged |= ImGui.IsItemDeactivatedAfterEdit();
		}
		ImGui.TableNextColumn();
		using(ImRaii.PushColor(ImGuiCol.FrameBg, 0xFF004400)) { 
			ImRaii.ItemWidth(uis.SidebarW/3-2*uis.Space);
			res |= ImGui.DragFloat($"###Y{s}{type}", ref val.Y, speed, 0, 0, SliderFormat(type), ImGuiSliderFlags.NoRoundToFormat);
			stateChanged |= ImGui.IsItemDeactivatedAfterEdit();
		}
		ImGui.TableNextColumn();
		using(ImRaii.PushColor(ImGuiCol.FrameBg, 0xFF440000)) { 
			ImRaii.ItemWidth(uis.SidebarW/3-2*uis.Space);
			res |= ImGui.DragFloat($"###Z{s}{type}", ref val.Z, speed, 0, 0, SliderFormat(type), ImGuiSliderFlags.NoRoundToFormat);
			stateChanged |= ImGui.IsItemDeactivatedAfterEdit();
		}
		ImGui.EndTable();
		return res;
	}
	private string SliderFormat(SliderFormatFlag type) {
		if(type == SliderFormatFlag.Rotation)
			return "\x20\x20%.3f°";
		else
			return "%.3f";
	}

	// Buttons

	public bool Btn(string s, string id, Vector2 size, string tooltip) {
		bool res;
		//using (ImRaii.PushFont(UiBuilder.IconFont)){ 
			res = ImGui.Button($"{s}###{id}", size);
			//};
		if (ImGui.IsItemHovered())
			using (ImRaii.Tooltip()) {
				ImGui.Text(tooltip);
			};
		return res;
	}
	public bool BtnIcon(FontAwesomeIcon icon, string id, Vector2 size, string tooltip) {
		bool res;
		using (ImRaii.PushFont(UiBuilder.IconFont)){ 
			res = ImGui.Button($"{icon.ToIconString()}###{id}", size);
			};
		if (ImGui.IsItemHovered())
			using (ImRaii.Tooltip()) {
				ImGui.Text(tooltip);
			};
		return res;
	}
	public bool BtnIconState(FontAwesomeIcon icon, string id, Vector2 size, string tooltip, bool btnState, uint activeCol, uint passiveCol) {
		using(ImRaii.PushColor(ImGuiCol.Button, btnState ? activeCol : passiveCol)) {
			return BtnIcon(icon, id, size, tooltip);
		}
	}

	// TODO generic call handling
	public void BtnSave(Func<(LazyIOFlag flag, string data)> dataDispatcher, string id, string tooltip, LazyIO io) {
		if(BtnIcon(FontAwesomeIcon.Save, id, uis.BtnSmall, tooltip) && !io.dialogOpen) {
			io.dialogOpen = true;
			var (flag, data) = dataDispatcher();
			io.SetSaveBuffer(data);
			io.OpenDialogSave(flag, (success, result) => {
				if (success) 
					io.SetSaveBuffer("");
				io.dialogOpen = false;
			});
		}
	}
	public void BtnLoad(LazyIOFlag flag, Action<bool, List<string>?> dataReceiver, string id, string tooltip, LazyIO io) {
		if(BtnIcon(FontAwesomeIcon.FolderOpen, id, uis.BtnSmall, tooltip) && !io.dialogOpen) {
			io.dialogOpen = true;
			io.OpenDialogLoad(flag, (success, result) => {
				if (success) {
					List<string>? fileContents = io.ReadFile2(result[0]);
					if(fileContents != null) {
						dataReceiver(success, fileContents);
					}
					else
						dataReceiver(false, null);
				} 
				io.dialogOpen = false;
			});
		}
	}
}

// Used for scalable UI in LazyImgui and LazyWidgets
// TODO only use one instance of this ffs
public struct LazyUiSizes {
	private Vector2 bBtnSmaller;
	private Vector2 bBtnSmall;
	private Vector2 bBtnBig;

	private float bSpace;

	public readonly Vector2 BtnSmall => bBtnSmall * Scale;
	public readonly Vector2 BtnSmaller => bBtnSmaller * Scale;
	public readonly Vector2 BtnBig => bBtnBig * Scale;
	public readonly float Space => bSpace * Scale;

	public float Scale;

	public Vector2 ScreenDimensions;
	public float SidebarFactor;
	public float SidebarW => ScreenDimensions.X * SidebarFactor * Scale;


	public LazyUiSizes() {
		bBtnSmall = new(37, 37);
		bBtnSmaller = new(22, 22);
		bBtnBig = new(77, 77);
		bSpace = 5.0f;
		Scale = ImGui.GetIO().FontGlobalScale;
		SidebarFactor = 1/7.0f;

		SetScreenDimensionLimits();
	}
	public bool RefreshScale() {
		if(Scale != ImGui.GetIO().FontGlobalScale) {
			Scale = ImGui.GetIO().FontGlobalScale;
			return true;
		}
		return false;
	}
	// TODO update LazyImgui to use instead
	private unsafe void SetScreenDimensionLimits() {
		Device* dev = Device.Instance();
		ScreenDimensions.X = dev->Width;
		ScreenDimensions.Y = dev->Height;
	}
}
public enum SliderFormatFlag {
	Rotation,
	Position,
	Scale
}
