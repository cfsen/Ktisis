using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
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

	public LazyPoseComponents pose;

	public LazyBase(IEditorContext ctx, ISelectManager sel, IFramework fw) {
		Ktisis.Log.Debug("LazyBase init");
		//this.Sizes = new();
		this.fw = fw;
		this.pose = new(ctx);
	}

	//public bool BtnIcon(FontAwesomeIcon icon, string id, Vector2 size, string tooltip) {
	//	// TODO disabled due to some shenanigans with them ending up earlier than they should
	//	//if (ImGui.IsItemHovered())
	//		//using (var _tip = ImRaii.Tooltip()) {
	//		//	ImGui.Text(tooltip);
	//		//};
	//	using var _ = ImRaii.PushFont(UiBuilder.IconFont);
	//	return ImGui.Button($"{icon.ToIconString()}###{id}", new(size.X*Sizes.Scale, size.Y*Sizes.Scale));
	//}
}

// Used for scalable UI in LazyImgui and LazyWidgets
//public struct LazyUiSizes {
//	public Vector2 BtnSmall;
//	public Vector2 BtnBig;
//	public Vector2 Space;
//	public float Scale;

//	public LazyUiSizes() {
//		this.BtnSmall = new(37, 37);
//		this.BtnBig = new(79, 79);
//		this.Space = new(5, 5);
//		this.Scale = 1.0f;
//	}
//}
