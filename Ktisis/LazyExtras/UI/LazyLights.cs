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

namespace Ktisis.Interface.Windows;

public class LazyLight :KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly LazyLightsComponents _lazyLightsComponents;
	public LazyLight(
		IEditorContext ctx
	) : base("Lazy lights") {
		this._ctx = ctx;
		this._lazyLightsComponents = new LazyLightsComponents(ctx);
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(280,300),
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
		if(ImGui.Button("Export"))
			this._lazyLightsComponents.LightsSave();
		ImGui.SameLine();
		if(ImGui.Button("Import"))
			this._lazyLightsComponents.ImportLightJson(this._lazyLightsComponents._json);
		// TODO this should be replaced with a proper file-based solution.
		ImGui.InputTextMultiline("", ref this._lazyLightsComponents._json, uint.MaxValue, new Vector2(280, 120));
	}
}
