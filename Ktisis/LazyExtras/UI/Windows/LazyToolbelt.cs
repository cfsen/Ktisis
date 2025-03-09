using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;

namespace Ktisis.Interface.Windows;

public class LazyToolbelt : KtisisWindow {
	private readonly IEditorContext _ctx;
	public LazyToolbelt(
		IEditorContext ctx
	) : base("Lazy toolbelt", ImGuiWindowFlags.NoTitleBar) {
		this._ctx = ctx;
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(280,300),
			MaximumSize = new(560,600)
		};
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context stale, closing...");
		this.Close();
	}

	public override void PreDraw() {

	}

	public override void Draw() {
		ImGui.Text("Hello.");
	}
}
