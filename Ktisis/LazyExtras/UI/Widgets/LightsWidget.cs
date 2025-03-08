using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Data.Common;
using System.IO;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

namespace Ktisis.LazyExtras.UI.Widgets;
class LightsWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;
	
	bool dialogOpen = false;

	public LightsWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Light;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		if(dialogOpen)
			ctx.LazyExtras.io.DrawDialog();

		ImGui.BeginGroup();

		lui.DrawHeader(FontAwesomeIcon.Lightbulb, "Lights");
		DrawPresetSpawnControls();
		DrawImportExportControls();
		lui.DrawFooter();

		ImGui.EndGroup();
	}

	private void DrawPresetSpawnControls() {
		ImGui.Text("Spawn preset");
		if(ImGui.Button("3 point"))
			ctx.LazyExtras.lights.SpawnStudioLights();
		ImGui.SameLine();
		if(ImGui.Button("Apartment exterior"))
			ctx.LazyExtras.lights.SpawnStudioApartmentAmbientLights();
	}
	private void DrawImportExportControls() {
		ImGui.Text("Import/Export");
		if(ImGui.Button("Export")) {
			dialogOpen = true;
			ctx.LazyExtras.lights.LightsSave();
			ctx.LazyExtras.io.SetSaveBuffer(ctx.LazyExtras.lights._json);
			ctx.LazyExtras.io.OpenLightSaveDialog((valid, res) => {
			switch (valid) {
				case true:
						dbg("Saving lights.");
					break;
				default:
						dbg("Failed to save lights.");
					break;
			}
			dialogOpen = false;
			});
		}
		ImGui.SameLine();

		if(ImGui.Button("Import")) {
			dialogOpen = true;
			ctx.LazyExtras.io.OpenLightDialog((valid, res) => {
			switch (valid) {
				case true:
					dbg("Lights loaded.");
					ctx.LazyExtras.lights._json = ctx.LazyExtras.io.LoadFileData();
					ctx.LazyExtras.lights.ImportLightJson(ctx.LazyExtras.io.LoadFileData());
					break;
				default:
					dbg("Failed to load lights.");
					break;
			}
			dialogOpen = false;
			});
		}
		ImGui.SameLine();

		if(ImGui.Button("Remove all lights"))
			ctx.LazyExtras.lights.LightsDeleteAll();
	}
	private void dbg(string s) => Ktisis.Log.Debug($"LightsWidget: {s}");
}
