using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Ipc;
using Ktisis.LazyExtras.Datastructures;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Modules.Actors;

using Lumina.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;

[Singleton]
public class LazyIpcIntegrator : IDisposable {
	private IEditorContext ctx;
	private LazyIO io;
	private IpcManager ipc;

	public bool AutomationEnabled = true;

	public LazyIpcFavorites favorites; 
	public List<LazyIpcFavorite> bufferFavs;
	public LazyIpcIntegrator (IEditorContext _ctx, LazyIO _io) {
		ctx = _ctx;
		io = _io;
		ipc = ctx.Plugin.Ipc;
		bufferFavs = [];
		FavoritesImport();
	}

	// Manipulators

	public async Task SpawnFavorite(LazyIpcFavorite lif) {
		var entity = await ctx.Scene.GetModule<ActorModule>().Spawn();
		entity.Name = lif.Name;
		ApplyAssociatedDesign(lif);
	}

	public void ApplyAssociatedDesign(LazyIpcFavorite lif) {
		if(ctx.Scene.Recurse().Where(x => x.Name == lif.Name).FirstOrDefault() is not ActorEntity ae) return;
		var fav = favorites.Actors.First(x => x.Name == lif.Name);

		dbg("ApplyAssociatedDesign(), found lif:");
		Dumplif(fav);

		if(fav.PenumbraCollection != null) {
			var resetCol = ipc.GetPenumbraIpc().SetCollectionForObject(ae.Actor.ObjectIndex, null);
			if(resetCol.Item1 == Penumbra.Api.Enums.PenumbraApiEc.Success)
				dbg("Reset collection.");
			var pRes = ipc.GetPenumbraIpc().SetCollectionForObject(ae.Actor.ObjectIndex, fav.PenumbraCollection);
			if(pRes.Item1 == Penumbra.Api.Enums.PenumbraApiEc.Success)
				dbg("Assigned new colelction");
			//dbg(pRes.ToString());
		}
		if(fav.GlamourerState != null) {
			var gRes = ipc.GetGlamourerIpc().ApplyState(fav.GlamourerState, ae.Actor.ObjectIndex);
		}

		ae.Redraw();
	}

	// Buffer management

	public void ScanSceneActors() {
		var actors = ctx.Scene.Recurse().OfType<ActorEntity>().ToList();
		if(actors == null) return;
		if(!ipc.IsGlamourerActive || !ipc.IsPenumbraActive) return;

		bufferFavs.Clear();
		foreach(var actor in actors) {
			AddToBuffer(actor);
		}
	}

	private unsafe void AddToBuffer(ActorEntity act) {
		LazyIpcFavorite lif = new(){ Name=act.Name };
		lif.Persistent = favorites.Actors.Any(x => x.Name == act.Name);

		if(lif.Persistent) {
			dbg("Loading persistent design");
			LazyIpcFavorite fav = favorites.Actors.Where(x => x.Name == act.Name).First();
			lif = fav;
			fav.Present = true;
		}
		else {
			lif.Name = act.Name;

			var glamourerState = ipc.GetGlamourerIpc().GetState(act.Actor.ObjectIndex);
			if(glamourerState.Item1 == Glamourer.Api.Enums.GlamourerApiEc.Success)
				lif.GlamourerState = glamourerState.Item2;

			lif.GlamourerUseState = true;
			lif.GlamourerDesignNameDefault = null;

			var (objectValid, individualSet, effectiveCollection) = ipc.GetPenumbraIpc().GetCollectionForObject(act.Actor);
			if(objectValid) {
				lif.PenumbraCollection = effectiveCollection.Id;
				lif.PenumbraName = effectiveCollection.Name;
			}
			lif.Automatic = false;

			lif.Present = true;
		}

		bufferFavs.Add(lif);
	}

	// Favorites management

	public List<LazyIpcFavorite> InactiveFavs() {
		return favorites.Actors.Where(x => !x.Present).ToList();
	}
	public void AddToFavorites(LazyIpcFavorite lif) {
		if(favorites.Actors.Any(x => x.Name == lif.Name)) return;
		lif.Persistent = true;
		favorites.Actors.Add(lif);
	}
	public bool ActorRemove(LazyIpcFavorite lif) {
		bool res = favorites.Actors.Remove(lif);
		dbg(res.ToString());
		return res;
	}
	private bool ActorRemove(string name) {
		var target = favorites.Actors.Where(x => x.Name == name).FirstOrDefault();
		return favorites.Actors.Remove(target);
	}
	public void DeduplicateFav() {
		Dictionary<string, int> entries = new Dictionary<string, int>();
		foreach(var act in favorites.Actors) {
			if(entries.ContainsKey(act.Name))
				entries[act.Name]++;
			else
				entries.Add(act.Name, 1);
		}

		foreach(var key in entries.Keys) {
			if(entries[key] > 1)
				_deleteEntries(key, entries[key]);
		}
		void _deleteEntries(string name, int amount) {
			dbg($"Purging {amount} entries for {name}");
			for(int i = 0; i < amount-1; i++) {
				ActorRemove(name);
			}
		}
	}

	// Favorites IO

	private void FavoritesImport() {
		var _ = io.ReadFile(io.GetConfigPath("LazyFavorites.json"));
		if(_ == null) {
		// Regenerate
			dbg("no ipc favorites found");
			favorites = new LazyIpcFavorites();
		}
		else {
			dbg("loading ipc favorites");
			favorites = FavoritesGenerateObj(_.Value.data);
		}
		// Reset presence state
		for(int i = 0; i < favorites.Actors.Count; i++) {
			favorites.Actors[i].Present = false;
		}
	}
	private LazyIpcFavorites FavoritesGenerateObj(string json) {
		var settings = new JsonSerializerSettings {
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore
		};
		return JsonConvert.DeserializeObject<LazyIpcFavorites>(json, settings);
	}
	private string FavoritesExport() {
		return JsonConvert.SerializeObject(favorites, Formatting.Indented);
	}
	private void FavoritesSave() {
		io.SaveConfig("LazyFavorites.json", FavoritesExport());
	}
	public void Dispose() {
		FavoritesSave();
		dbg("LazyIpcIntegrator disposing.");
	}

	// Debug

	private void dbg(string s) => Ktisis.Log.Debug($"IpcIntegrator: {s}");
	public void DumpDbg() {
		DumpBuffer();
		DumpFavs();
	}
	private void DumpBuffer() {
		dbg("Dumping buffer state");
		if(bufferFavs.Count > 0) {
			foreach(var b in bufferFavs) {
				Dumplif(b);
			}
		}
		dbg("Dump complete.");
	}
	private void DumpFavs() {
		dbg("Dumping favorites state");
		if(favorites.Actors.Count > 0) {
			foreach(var b in favorites.Actors) {
				Dumplif(b);
			}
		}
		dbg("Dump complete.");
	}
	private void Dumplif(LazyIpcFavorite lif) {
		dbg("Dumping lif:\r" +
			$"\tName: {lif.Name}" +
			$"\tCollection: {lif.PenumbraName}" +
			$"\tPersistent: {lif.Persistent}" +
			$"\tPresent: {lif.Present}");
	}
}
