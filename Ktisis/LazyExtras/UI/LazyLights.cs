using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;
using Ktisis.LazyExtras.Components;
using System.IO;

namespace Ktisis.Interface.Windows;

public class LazyLight :KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly LazyLightsComponents _lazyLightsComponents;
	public LazyLight(
		IEditorContext ctx,
		GuiManager gui
	) : base("Lazy lights") {
		this._ctx = ctx;
		this._gui = gui;
		this._lazyLightsComponents = new LazyLightsComponents(ctx);
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(240,130),
			MaximumSize = new(560,600)
		};
	}

	public override void Draw() {
		ImGui.Text("Spawn preset");
		if(ImGui.Button("3 point"))
			this._lazyLightsComponents.SpawnStudioLights();
		ImGui.SameLine();
		if(ImGui.Button("Apartment exterior"))
			this._lazyLightsComponents.SpawnStudioApartmentAmbientLights();
		ImGui.Separator();
		ImGui.Text("Import/Export");
		if(ImGui.Button("Export")) {
			this._lazyLightsComponents.LightsSave();
			this._gui.FileDialogs.SaveFile(
				"Save lights",
				this._lazyLightsComponents._json,
				new GLib.Popups.ImFileDialog.FileDialogOptions{ 
					Filters="Ktisis lights{.klights}",
					Extension = ".klights" 
					}
				);
		}
		ImGui.SameLine();
		if(ImGui.Button("Import")) {
			this._gui.FileDialogs.OpenFile(
				"Ktisis lights", 
				(path) => { 
					try	{
						var _ = File.ReadAllText(path);
						this._lazyLightsComponents._json = _;
						this._lazyLightsComponents.ImportLightJson(_);
					}
					catch { 
						Ktisis.Log.Debug("LazyLights: Couldn't read the file. Cry about it.");	
					}
				}, 
				new GLib.Popups.ImFileDialog.FileDialogOptions {
					Filters="Ktisis lights{.klights}",
					Extension=".klights"
			});
		}
		ImGui.SameLine();
		if(ImGui.Button("Remove all lights"))
			this._lazyLightsComponents.LightsDeleteAll();

	}
}
