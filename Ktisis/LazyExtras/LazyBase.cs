using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.LazyExtras.Components;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.LazyExtras.UI.Widgets;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Services.Plugin;
using Ktisis.Structs.Camera;

using Microsoft.Extensions.DependencyInjection;

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
public class LazyBase :IDisposable {
	//public LazyUiSizes Sizes;
	public IFramework fw;
	public FileDialogManager fdm;
	private ISelectManager sel;

	public LazyPoseComponents pose;
	public LazyCameraComponents camera;
	public LazyLightsComponents lights;
	public ActorEntity? SelectedActor;

	public LazyIO io;


	public LazyBase(IEditorContext ctx, ISelectManager sel, IFramework fw, IDalamudPluginInterface dpi) {
		Ktisis.Log.Debug("LazyBase init");
		this.fw = fw;
		this.pose = new(ctx);
		this.camera = new(ctx);
		this.lights = new(ctx);
		this.fdm = new();
		this.io = new(dpi);
		this.sel = sel;
		this.sel.Changed += ActorSelectionChanged;
	}

	private void ActorSelectionChanged(ISelectManager sender) {
		dbg("Selection event");
		dbg($"Sent: {sender.GetSelected().FirstOrDefault()}");

		var selected = sender.GetSelected();

		// TODO rushed implementation to get something that works, do something more elegant
		if(!selected.Any()) {
			dbg("0 selected");
			return;
		}
		else if(selected.Count() == 1) {
			dbg("1 selected");
			if(selected is ActorEntity ae){
				dbg("1 AE");
				this.SelectedActor = ae;
			}
			else {
				dbg("1 SE recurse");
				var res = ResolveActorEntity(selected.First());
				if(res is ActorEntity rec)
					this.SelectedActor = rec;
			}
		}
		else {
			dbg("n>1 selected");
			var first = selected.First();
			if(first is ActorEntity ae)
				this.SelectedActor = ae;
		}
	}

	// Target resolving

	/// <summary>
	/// Backtracks current selection in order to find the parent ActorEntity. Max depth 10. 
	/// </summary>
	/// <returns>Selected ActorEntity on success, null on failure.</returns>
	public ActorEntity? ResolveActorEntity(SceneEntity selected) {
		// Resolves the parent actor entity of any bone. Recursion warning.
		if (selected == null) return null;

		ActorEntity? actor = this.Backtrack(selected, 0, 10);
		if (actor != null)
			return actor;
		return null;
	}

	/// <summary>
	/// Recursive function for ResolveActorEntity()
	/// </summary>
	/// <param name="node">Current node</param>
	/// <param name="depth">Current depth</param>
	/// <param name="maxdepth">Max depth</param>
	/// <returns>ActorEntity on success, null on failure.</returns>
	private ActorEntity? Backtrack(object node, int depth = 0, int maxdepth = 0) {
		// Recursion used in ResolveActorEntity.
		if (node is ActorEntity ae)
			return ae;
		if (depth >= maxdepth)
			return null;

		var parentProperty = node.GetType().GetProperty("Parent");
		if (parentProperty == null)
			return null;

		var parent = parentProperty.GetValue(node);
		if (parent != null)
		{
			var res = Backtrack(parent, depth+1, maxdepth);
			if (res != null)
				return res;
		}
		return null;
	}

	public void Dispose() {
		Ktisis.Log.Debug("LazyBase dispose.");
		sel.Changed -= ActorSelectionChanged;
		this.io.Dispose();
	}

	private void dbg(string s) => Ktisis.Log.Debug($"LazyBase: {s}");
}
