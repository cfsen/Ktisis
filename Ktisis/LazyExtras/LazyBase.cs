using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.LazyExtras.Components;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;

using System;
using System.Diagnostics;
using System.Linq;


namespace Ktisis.LazyExtras;
[Singleton]
public class LazyBase :IDisposable {
	public IFramework fw;
	public FileDialogManager fdm;
	private ISelectManager sel;

	public LazyPoseComponents pose;
	public LazyCameraComponents camera;
	public LazyLightsComponents lights;
	public LazyOverlayComponents overlay;
	public LazyMaths math;
	public LazyIO io;
	public LazyActorOffsetsComponents actors;
	public LazyIpcIntegrator ipc;

	public ActorEntity? SelectedActor;

	public LazyBase(IEditorContext ctx, ISelectManager sel, IFramework fw, IDalamudPluginInterface dpi, LazyIO io, LazyIpcIntegrator ipc) {
		dbg("LazyBase init");
		this.fw = fw;
		this.fdm = new();
		this.sel = sel;
		this.sel.Changed += ActorSelectionChanged;

		this.math = new();
		this.io = io;
		this.camera = new(ctx);
		this.lights = new(ctx);
		this.overlay = new(ctx);
		this.pose = new(ctx, this.math);
		this.actors = new(ctx);
		this.ipc = ipc;

	}

	private void ActorSelectionChanged(ISelectManager sender) {
		// TODO last light tracking
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
		else if(count > 1 && selected.Count(x => x is ActorEntity) == count) {
			this.SelectedActor = ResolveActorEntity(first);
			return;
		}
		else if(count > 1 && selected.Count(x => x is LightEntity) == count) {
			// don't swap selected actor if new selection is only lights
			return;
		}

		// if there's an actorentity in here, we'll find it
		var needle = selected.FirstOrDefault(x => x is not LightEntity);
		if(needle != null)
			this.SelectedActor = ResolveActorEntity(needle);
	}

	// Target resolving

	/// <summary>
	/// Backtracks current selection in order to find the parent ActorEntity. Max depth 10. 
	/// </summary>
	/// <returns>Selected ActorEntity on success, null on failure.</returns>
	public ActorEntity? ResolveActorEntity() {
		// Resolves the parent actor entity of any bone. Recursion warning.
		var selected = this.sel.GetSelected().FirstOrDefault();
		if (selected == null)
			return null;

		ActorEntity? actor = this.Backtrack(selected, 0, 10);
		if (actor != null)
			return actor;
		return null;
	}

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
		if (parent != null) {
			var res = Backtrack(parent, depth+1, maxdepth);
			if (res != null)
				return res;
		}
		return null;
	}

	public void Dispose() {
		Stopwatch t = Stopwatch.StartNew();
		sel.Changed -= ActorSelectionChanged;
		this.ipc.Dispose();
		this.io.Dispose();
		t.Stop();
		dbg($"LazyBase dependencies disposed in {t.ElapsedMilliseconds}.");
	}

	private void dbg(string s) => Ktisis.Log.Debug($"LazyBase: {s}");
}
