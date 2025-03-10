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
using Ktisis.Scene.Entities.World;
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
		// TODO testing and cleanup
		var selected = sender.GetSelected();

		if(!selected.Any()) return;
		var count = selected.Count();
		var first = selected.First();

		if(count == 1 && first is ActorEntity entity) {
			this.SelectedActor = entity;
			return;
		}
		else if(count == 1 && first is LightEntity) {
			// don't swap selected actor when selecting a light
			return;
		}
		else if(count == 1 && first is not ActorEntity){
			this.SelectedActor = ResolveActorEntity(first);
			return;
		}
		else if(count > 1 && selected.Where(x => x is ActorEntity).Count() == count) {
			this.SelectedActor = ResolveActorEntity(first);
			return;
		}
		else if(count > 1 && selected.Where(x => x is LightEntity).Count() == count) {
			// don't swap selected actor if new selection is only lights
			return;
		}

		// if there's an actorentity in here, we'll find it
		var needle = selected.Where(x => x is not LightEntity).FirstOrDefault();
		if(needle != null)
			this.SelectedActor = ResolveActorEntity(needle);
	}

	// Target resolving
		// TODO this is used in several subclasses, who whould all point here via ctx.

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
