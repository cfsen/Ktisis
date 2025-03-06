using Dalamud.Interface.ImGuiFileDialog;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.IO;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

namespace Ktisis.LazyExtras.UI.Widgets
{
	class LightsWidget :ILazyWidget {
		public LazyWidgetCat Category { get; }
		public int CustomOrder { get; set; }
		public bool InToolbelt { get; set; }
		public bool SupportsToolbelt { get; }
		public Vector2 SizeToolbelt { get; }

		private IEditorContext ctx;
		private LazyUi lui;
		private LazyUiSizes uis;
		private float gs;
		
		bool dopen = false;

		public LightsWidget(IEditorContext ctx) {
			this.Category = LazyWidgetCat.Misc;
			this.SupportsToolbelt = false;
			this.SizeToolbelt = Vector2.Zero;
			this.InToolbelt = false;

			this.ctx = ctx;
			this.lui = new();
			this.uis = lui.uis;
			this.gs = lui.uis.Scale;
		}
		public void UpdateScaling() {
			this.uis.RefreshScale();
		}
		public void Draw() {
			
			ImGui.Text("Spawn preset");
			if(ImGui.Button("3 point"))
				this.ctx.LazyExtras.lights.SpawnStudioLights();
			ImGui.SameLine();
			if(ImGui.Button("Apartment exterior"))
				this.ctx.LazyExtras.lights.SpawnStudioApartmentAmbientLights();
			ImGui.Separator();
			ImGui.Text("Import/Export");
			if(ImGui.Button("Export")) {
				this.ctx.LazyExtras.lights.LightsSave();
				// TODO migrate off glib
				//this._gui.FileDialogs.SaveFile(
				//	"Save lights",
				//	this.ctx.LazyExtras.lights._json,
				//	new GLib.Popups.ImFileDialog.FileDialogOptions{ 
				//		Filters="Ktisis lights{.klights}",
				//		Extension = ".klights" 
				//		}
				//	);
			}
			ImGui.SameLine();
			if(ImGui.Button("Import")) {
				this.dopen = true;
				this.ctx.LazyExtras.fdm.OpenFileDialog("Import", ".klights", (success, res) => { 
					if(success){ 
						Ktisis.Log.Debug("nope");
						try
						{
							var _ = File.ReadAllText(res[0]);
							this.ctx.LazyExtras.lights._json = _;
							this.ctx.LazyExtras.lights.ImportLightJson(_);
						} catch
						{
							Ktisis.Log.Debug("LazyLights: Couldn't read the file. Cry about it.");
						}

					} else {
						Ktisis.Log.Debug("Yup");
					}
					this.dopen = false;
					}
				, 1, @"C:\", false);
				// TODO migrate off glib
				//this._gui.FileDialogs.OpenFile(
				//	"Ktisis lights", 
				//	(path) => { 
				//		try	{
				//			var _ = File.ReadAllText(path);
				//			this.ctx.LazyExtras.lights._json = _;
				//			this.ctx.LazyExtras.lights.ImportLightJson(_);
				//		}
				//		catch { 
				//			Ktisis.Log.Debug("LazyLights: Couldn't read the file. Cry about it.");	
				//		}
				//	}, 
				//	new GLib.Popups.ImFileDialog.FileDialogOptions {
				//		Filters="Ktisis lights{.klights}",
				//		Extension=".klights"
				//});
			}
			ImGui.SameLine();
			if(ImGui.Button("Remove all lights"))
				this.ctx.LazyExtras.lights.LightsDeleteAll();
			if(this.dopen)
				this.ctx.LazyExtras.fdm.Draw();

		}
    }
}
