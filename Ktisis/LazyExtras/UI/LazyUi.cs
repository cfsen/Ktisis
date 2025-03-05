using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
		uis.Scale = newScaling;
	}

	// Slider inputs

	public bool SliderTableRow(string s, ref Vector3 val, SliderFormatFlag type, ref bool stateChanged) {
		bool res = false;
		float speed = (type == SliderFormatFlag.Rotation)? 0.3f : 0.0015f; 
		ImGui.BeginTable($"###SliderTable{s}", 3, ImGuiTableFlags.NoBordersInBody, new(410,20));
		ImGui.TableNextColumn();
		using(ImRaii.PushColor(ImGuiCol.FrameBg, 0xFF000044)) { 
			ImRaii.ItemWidth(120);
			res |= ImGui.DragFloat($"###X{s}{type}", ref val.X, speed, 0, 0, SliderFormat(type), ImGuiSliderFlags.NoRoundToFormat);
			stateChanged |= ImGui.IsItemDeactivatedAfterEdit();
		}
		ImGui.TableNextColumn();
		using(ImRaii.PushColor(ImGuiCol.FrameBg, 0xFF004400)) { 
			ImRaii.ItemWidth(120);
			res |= ImGui.DragFloat($"###Y{s}{type}", ref val.Y, speed, 0, 0, SliderFormat(type), ImGuiSliderFlags.NoRoundToFormat);
			stateChanged |= ImGui.IsItemDeactivatedAfterEdit();
		}
		ImGui.TableNextColumn();
		using(ImRaii.PushColor(ImGuiCol.FrameBg, 0xFF440000)) { 
			ImRaii.ItemWidth(120);
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
		using (ImRaii.PushFont(UiBuilder.IconFont)){ 
			res = ImGui.Button($"{s}###{id}", new(size.X*uis.Scale, size.Y*uis.Scale));
			};
		if (ImGui.IsItemHovered())
			using (ImRaii.Tooltip()) {
				ImGui.Text(tooltip);
			};
		return res;
	}
	public bool BtnIcon(FontAwesomeIcon icon, string id, Vector2 size, string tooltip) {
		bool res;
		using (ImRaii.PushFont(UiBuilder.IconFont)){ 
			res = ImGui.Button($"{icon.ToIconString()}###{id}", new(size.X*uis.Scale, size.Y*uis.Scale));
			};
		if (ImGui.IsItemHovered())
			using (ImRaii.Tooltip()) {
				ImGui.Text(tooltip);
			};
		return res;
	}
}

// Used for scalable UI in LazyImgui and LazyWidgets
public struct LazyUiSizes {
	public Vector2 BtnSmall;
	public Vector2 BtnBig;
	public Vector2 Space;
	public float Scale;

	public LazyUiSizes() {
		this.BtnSmall = new(37, 37);
		this.BtnBig = new(79, 79);
		this.Space = new(5, 5);
		this.Scale = 1.0f;
	}
}
public enum SliderFormatFlag {
	Rotation,
	Position,
	Scale
}
