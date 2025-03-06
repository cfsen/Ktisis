using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.LazyExtras.Components;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.LazyExtras.UI.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras;
/*
 * This is a very lazy solution to avoid deep/proper integration 
 * into the existing hierarchy. It's initialized in ContextBuilder,
 * and can also be found in the global context object (context.LazyExtras).
 * 
 * Responsibilities:
 * - Initializing and providing access to various lazy components
 * - Initializing widgets for LazyImgui
 * 
 * If at any point in the future this were to be integrated properly,
 * it should be a reasonably straightforward task.
 * 
 * */
public class LazyBase {
	//public LazyUiSizes Sizes;
	public IFramework fw;
	public FileDialogManager fdm;

	public LazyPoseComponents pose;
	public LazyCameraComponents camera;
	public LazyLightsComponents lights;

	public LazyBase(IEditorContext ctx, ISelectManager sel, IFramework fw) {
		Ktisis.Log.Debug("LazyBase init");
		this.fw = fw;
		this.pose = new(ctx);
		this.camera = new(ctx);
		this.lights = new(ctx);
		this.fdm = new();
	}
}
