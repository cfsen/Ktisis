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

public class LazyExtras :KtisisWindow {
	private readonly IEditorContext _ctx;
	public LazyExtras(
		IEditorContext ctx
	) : base("Lazy extras") {
		this._ctx = ctx;
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
		ImGui.Text("Hello.");
	}
}
