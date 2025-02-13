using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;
using System.Runtime.CompilerServices;
using System.Linq;
using Ktisis.LazyExtras.Components;

namespace Ktisis.Interface.Windows;

public class LazyCamera :KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly LazyCameraComponents _lazyCameraComponents;
	public LazyCamera(
		IEditorContext ctx,
		GuiManager gui
	) : base("Lazy camera") {
		this._ctx = ctx;
		this._gui = gui;
		this._lazyCameraComponents = new LazyCameraComponents(ctx, gui);
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
		this.CameraList();
	}

	private void CameraList() {
		ImGui.Text("Cameras:");
		ImGui.Separator();
		var l = this._ctx.Cameras.GetCameras().ToList();
		int i = 0;
		foreach ( var cam in l ) {
			ImGui.Text(cam.Name);
			ImGui.SameLine();
			var xoffset = ImGui.GetWindowSize().X - 100.0f;
			ImGui.SetCursorPosX(xoffset);
			if(ImGui.Button(
				$"Delete##lazyCamDelBtn{i}", 
				new Vector2 {
					X=80.0f, 
					Y=0
				})
			) {
				Ktisis.Log.Debug("LazyCamera: Removing camera: " + cam.Name);
				this._ctx.Cameras.RemoveCamera(cam);
			}
			i++;
		}
	}

	
}
