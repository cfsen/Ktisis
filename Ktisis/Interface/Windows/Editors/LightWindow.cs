using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Windows.Editors;

public class LightWindow : EntityEditWindow<LightEntity> {
	private readonly LocaleManager _locale;
	private Dictionary<string, Vector3> _lights = new Dictionary<string, Vector3>();

	public LightWindow(
		IEditorContext ctx,
		LocaleManager locale
	) : base("Light Editor", ctx) {
		this._locale = locale;
		_lights.Add("Off", this.RGBToVector(0, 0, 0));

		_lights.Add("Direct Sunlight", this.RGBToVector(255,255,255));
		_lights.Add("High Noon Sun", this.RGBToVector(255,255,251));
		_lights.Add("Overcast Sky", this.RGBToVector(201,226,255));
		_lights.Add("Clear Blue Sky", this.RGBToVector(64,156,255));

		_lights.Add("Fog day", this.RGBToVector(163, 204, 191));
		_lights.Add("Fog night", this.RGBToVector(52, 72, 102));
		_lights.Add("Rainy day", this.RGBToVector(178,206,357));
		_lights.Add("Rainy night", this.RGBToVector(76,116,153));


		_lights.Add("LB0", Vector3.Zero);
		
		_lights.Add("Warm Fluorescent", this.RGBToVector(255,244,229));
		_lights.Add("Full Spectrum Fluorescent", this.RGBToVector(255,244,242));
		_lights.Add("Grow Light Fluorescent", this.RGBToVector(255,239,247));
		_lights.Add("Standard Fluorescent", this.RGBToVector(244,255,250));
		_lights.Add("Cool White Fluorescent", this.RGBToVector(212,235,255));
		_lights.Add("Black Light Fluorescent", this.RGBToVector(167,0,255));

		_lights.Add("LB1", Vector3.Zero);

		_lights.Add("High Pressure Sodium", this.RGBToVector(255,183,76));
		_lights.Add("Sodium Vapor", this.RGBToVector(255,209,178));
		_lights.Add("Thavnarian Lamp", this.RGBToVector(255,247,176));
		_lights.Add("Metal Halide", this.RGBToVector(242,252,255));
		_lights.Add("Mercury Vapor", this.RGBToVector(216,247,255));

		_lights.Add("LB2", Vector3.Zero);

		_lights.Add("Candle", this.RGBToVector(255,147,41));
		_lights.Add("40W Tungsten", this.RGBToVector(255,197,143));
		_lights.Add("100W Tungsten", this.RGBToVector(255,214,170));
		_lights.Add("Halogen", this.RGBToVector(255,241,224));
		_lights.Add("Carbon Arc", this.RGBToVector(255,250,244));

		_lights.Add("LB3", Vector3.Zero);

		_lights.Add("5500K", this.RGBToVector(248,255,183));
		_lights.Add("5100K", this.RGBToVector(255,248,167));
		_lights.Add("4700K", this.RGBToVector(255,234,144));
		_lights.Add("4300K", this.RGBToVector(255,218,122));
		_lights.Add("3900K", this.RGBToVector(255,201,100));
		_lights.Add("3500K", this.RGBToVector(255,182,78));
		_lights.Add("3100K", this.RGBToVector(255,162,57));
		_lights.Add("2700K", this.RGBToVector(255,139,39));
		_lights.Add("2300K", this.RGBToVector(255,115,23));
		_lights.Add("1900K", this.RGBToVector(255,89,11));
		_lights.Add("1500K", this.RGBToVector(255, 61, 4));

		_lights.Add("LB4", Vector3.Zero);

		_lights.Add("Pastel Purple", this.RGBToVector(205, 180, 219));
		_lights.Add("Pastel Light Pink", this.RGBToVector(255, 200, 221));
		_lights.Add("Pastel Pink", this.RGBToVector(255, 175, 204));
		_lights.Add("Pastel Blue", this.RGBToVector(189, 224, 254));
		_lights.Add("Pastel Deep Blue", this.RGBToVector(162, 210, 255));

		_lights.Add("LB5", Vector3.Zero);

		_lights.Add("P1", this.RGBToVector(251, 248, 204));
		_lights.Add("P2", this.RGBToVector(253, 228, 207));
		_lights.Add("P3", this.RGBToVector(255, 207, 210));
		_lights.Add("P4", this.RGBToVector(241, 192, 232));
		_lights.Add("P5", this.RGBToVector(207, 186, 240));
		_lights.Add("P6", this.RGBToVector(163, 196, 243));
		_lights.Add("P7", this.RGBToVector(144, 219, 244));
		_lights.Add("P8", this.RGBToVector(142, 236, 245));
		_lights.Add("P9", this.RGBToVector(152, 245, 225));
		_lights.Add("P10", this.RGBToVector(185, 251, 192));

	}

	// Draw handlers

	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(400, 300),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	public override void Draw() {
		var s = this.Context.Selection;
		if (s.Count == 1) {
			var p = s.GetSelected().First();
			if (p is LightEntity l)
				this.SetTarget(l);
		}

		var entity = this.Target;
		
		ImGui.Text($"{entity.Name}:");
		ImGui.Spacing();

		using var _ = ImRaii.TabBar("##LightEditTabs");
		this.DrawTab("Light", this.DrawLightTab, entity);
		this.DrawTab("Shadows", this.DrawShadowsTab, entity);
	}
	
	// Tabs

	private void DrawTab(string label, Action<LightEntity> draw, LightEntity entity) {
		using var _tab = ImRaii.TabItem(label);
		if (_tab.Success) draw.Invoke(entity);
	}
	
	// Light Tab

	private unsafe void DrawLightTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		ImGui.Spacing();
		this.DrawLightFlag("Enable reflections", light, LightFlags.Reflection);
		ImGui.Spacing();
		
		// Light type
		
		var lightTypePreview = this._locale.Translate($"lightType.{light->LightType}");
		if (ImGui.BeginCombo("Light Type", lightTypePreview)) {
			foreach (var value in Enum.GetValues<LightType>()) {
				var valueLabel = this._locale.Translate($"lightType.{value}");
				if (ImGui.Selectable(valueLabel, light->LightType == value))
					light->LightType = value;
			}
			ImGui.EndCombo();
		}
		
		switch (light->LightType) {
			case LightType.SpotLight:
				ImGui.SliderFloat("Cone Angle##LightAngle", ref light->LightAngle, 0.0f, 180.0f, "%0.0f deg");
				ImGui.SliderFloat("Falloff Angle##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f deg");
				break;
			case LightType.AreaLight:
				var angleSpace = ImGui.GetStyle().ItemInnerSpacing.X;
				var angleWidth = ImGui.CalcItemWidth() / 2 - angleSpace;
				ImGui.PushItemWidth(angleWidth);
				ImGui.SliderAngle("##AngleX", ref light->AreaAngle.X, -90, 90);
				ImGui.SameLine(0, angleSpace);
				ImGui.SliderAngle("Light Angle##AngleY", ref light->AreaAngle.Y, -90, 90);
				ImGui.PopItemWidth();
				ImGui.SliderFloat("Falloff Angle##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f deg");
				break;
		}
		
		ImGui.Spacing();
		
		// Falloff
		
		var falloffPreview = this._locale.Translate($"lightFalloff.{light->FalloffType}");
		if (ImGui.BeginCombo("Falloff Type", falloffPreview)) {
			foreach (var value in Enum.GetValues<FalloffType>()) {
				var valueLabel = this._locale.Translate($"lightFalloff.{value}");
				if (ImGui.Selectable(valueLabel, light->FalloffType == value))
					light->FalloffType = value;
			}
			ImGui.EndCombo();
		}

		ImGui.DragFloat("Falloff Power##FalloffPower", ref light->Falloff, 0.01f, 0.0f, 1000.0f);
		
		// Base light settings
		
		ImGui.Spacing();
		
		var color = light->Color.RGB;
		if (ImGui.ColorEdit3("Color", ref color, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Uint8))
			light->Color.RGB = color;
		ImGui.DragFloat("Intensity", ref light->Color.Intensity, 0.01f, 0.0f, 100.0f);
		if (ImGui.DragFloat("Range##LightRange", ref light->Range, 0.1f, 0, 999))
			entity.Flags |= LightEntityFlags.Update;


		// cfsen: Presets for light color

		ImGui.Spacing();

		foreach (var template in this._lights)
		{
			if (template.Key.StartsWith("LB"))
			{
				ImGui.Spacing();
				continue;
			}
			if (ImGui.ColorButton(template.Key, new Vector4(template.Value.X, template.Value.Y, template.Value.Z, 16.0f)))
			{
				light->Color.RGB = template.Value;
			}
			ImGui.SameLine();
		}
	}
	
	// Shadows tab

	private unsafe void DrawShadowsTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		ImGui.Spacing();
		this.DrawLightFlag("Dynamic shadows", light, LightFlags.Dynamic);
		ImGui.Spacing();
		
		this.DrawLightFlag("Cast character shadows", light, LightFlags.CharaShadow);
		this.DrawLightFlag("Cast object shadows", light, LightFlags.ObjectShadow);

		ImGui.Spacing();
		ImGui.DragFloat("Shadow Range", ref light->CharaShadowRange, 0.1f, 0.0f, 1000.0f);
		ImGui.Spacing();
		ImGui.DragFloat("Shadow Near", ref light->ShadowNear, 0.01f, 0.0f, 1000.0f);
		ImGui.DragFloat("Shadow Far", ref light->ShadowFar, 0.01f, 0.0f, 1000.0f);
		//ImGui.DragFloat("_unk0##TESTING_LIGHT_unk0", ref light->_unk0, 0.01f, -10000.0f, 10000.0f);
		//ImGui.DragFloat("_unkVec0.X##TESTING_LIGHT_unkvec0x", ref light->_unkVec0.X, 0.01f, -10000.0f, 10000.0f);
		//ImGui.DragFloat("_unkVec0.Y##TESTING_LIGHT_unkvec0y", ref light->_unkVec0.Y, 0.01f, -10000.0f, 10000.0f);

		// These seem to control light culling
		// Cutoff range negative X axis
		ImGui.DragFloat("Culling -X##TESTING_LIGHT_unk0vec0z", ref light->_unkVec0.Z, 0.01f, -10000.0f, 10000.0f);
		// Cutoff range negative Y axis
		ImGui.DragFloat("Culling -Y##TESTING_LIGHT_unkvec1x", ref light->_unkVec1.X, 0.01f, -10000.0f, 10000.0f);
		// Cutoff range negative Z axis
		ImGui.DragFloat("Culling -Z##TESTING_LIGHT_unkvec1y", ref light->_unkVec1.Y, 0.01f, -10000.0f, 10000.0f);

		// still unknown
		//ImGui.DragFloat("_unkVec1.Z##TESTING_LIGHT_unk0vec1z", ref light->_unkVec1.Z, 0.01f, -10000.0f, 10000.0f);

		// Cutoff range positive X axis
		ImGui.DragFloat("Culling +X##TESTING_LIGHT_unkvec2x", ref light->_unkVec2.X, 0.01f, -10000.0f, 10000.0f);
		// Cutoff range positive Y axis
		ImGui.DragFloat("Culling +Y##TESTING_LIGHT_unkvec2y", ref light->_unkVec2.Y, 0.01f, -10000.0f, 10000.0f);
		// Cutoff range positive Z axis
		ImGui.DragFloat("Culling +Z##TESTING_LIGHT_unk0vec2z", ref light->_unkVec2.Z, 0.01f, -10000.0f, 10000.0f);

		//ImGui.DragFloat("_unkVec2.W##TESTING_LIGHT_unk0vec2w", ref light->_unkVec2.W, 0.01f, -10000.0f, 10000.0f);
	}
	
	// Utility
	
	private unsafe void DrawLightFlag(string label, RenderLight* light, LightFlags flag) {
		var active = light->Flags.HasFlag(flag);
		if (ImGui.Checkbox(label, ref active))
			light->Flags ^= flag;
	}

	public Vector3 RGBToVector(int r, int g, int b) {
		return new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
	}
}
