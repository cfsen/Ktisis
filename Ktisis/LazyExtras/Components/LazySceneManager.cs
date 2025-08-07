using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.LazyExtras.Components;
using Ktisis.LazyExtras.Datastructures;
using Ktisis.LazyExtras.UI.Widgets;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazySceneManager {
	private IEditorContext ctx;

	public LazySceneManager(IEditorContext ctx) { 
		this.ctx = ctx;
	}

	public async void Import(string importData) {
		dbg("Import()");
		var settings = new JsonSerializerSettings {
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore
		};
		LazyScene? ls = new();
		try {
			ls = JsonConvert.DeserializeObject<LazyScene>(importData, settings);
		}
		catch (Exception ex) {
			dbg("Import failed:");
			dbg(ex.Message);
			return;
		}
		if(ls == null) {
			dbg("Scene file is null!");
			return;
		}

		if(!ctx.Plugin.Ipc.IsPenumbraActive || !ctx.Plugin.Ipc.IsGlamourerActive) {
			dbg("Penumbra or Glamourer not active.");
			return;
		}

		
		dbg("Setting LazyBase.SelectedActor to null.");
		// Ensure that widgets won't try to access actors while the list is being manipulated
		ctx.LazyExtras.SelectedActor = null;

		dbg("Deserialization complete. Checking actors.");

		var actors = ctx.Scene.Recurse().OfType<ActorEntity>().ToList();
		if(!actors.Any()) {
			dbg("No actors found.");
			return;
		}

		var _gpose = ctx.Scene.GetModule<GroupPoseModule>();
		Dictionary<string, ActorEntity> actorPrespawnedMap = [];
		ActorEntity? primary = null;
		foreach(var actor in actors) {
			// separate primary, for future "clear current scene and load" feature
			if(_gpose.IsPrimaryActor(actor)) {
				dbg($"Primary actor is: {actor.Name}");
				primary = actor;
				continue;
			}
			if(actor is IDeletable del) {
				/*
				TODO 7.2 temporary fix
				Purging the pre-spawned secondary actors seems to improve stability issues
				Not ideal, since this doesn't allow building a scene from other scenes
				this was not neccessary before 7.2, 
				should be disabled/made an option once the actual cause of accessviolations is found
				 */
				dbg($"Purging pre-existing actor: {actor.Name}");
				del.Delete();
				continue;
			}
			// check if actor is in scene data, if so append to actorPrespawnedMap
			if(ls.ActorOffsetFile.Actors.Any(x => x.Name == actor.Name)) {
				dbg($"Added prespawned actor: {actor.Name}");
				actorPrespawnedMap.Add(actor.Name, actor);
			}
		}

		if(primary == null) {
			dbg("Failed to find primary actor.");
			return;
		}

		// Spawn additional actors
		List<string> spawnedNames = [];
		Dictionary<string, ActorEntity> spawned = [];
		foreach(var unspawned in ls.Actors) {
			if(!actorPrespawnedMap.ContainsKey(unspawned.Name) && unspawned.Name != primary.Name) {
				// Designs are applied by LIPC
				var ae = await ctx.LazyExtras.ipc.SpawnImportFavorite(unspawned, true);
				Task.Delay(500).Wait(); // TODO debugging
				dbg($"Spawning: {unspawned.Name}"); 
				spawnedNames.Add(unspawned.Name);
				spawned.Add(unspawned.Name, ae);
			}
		}

		dbg($"Spawned {spawnedNames.Count} actors, linked {spawned.Count} actors.");

		// move primary into dict
		actorPrespawnedMap[primary.Name] = primary;

		// Glamour all prespawned actors
		foreach(var pspawn in actorPrespawnedMap) {
			dbg($"Setting customization for {pspawn.Key}");
			var design = ls.Actors.First(x => x.Name == pspawn.Key);
			ctx.LazyExtras.ipc.ApplyAssociatedDesign(design, true);
			Task.Delay(500).Wait(); // TODO debugging
		}

		// Pre-emptive waiting for skeletons to be ready
		Task.Delay(750).Wait();
		
		// merge dicts
		foreach(var n in spawned)
			actorPrespawnedMap[n.Key] = n.Value;

		// load poses
		foreach(var pspawn in actorPrespawnedMap) {
			if(pspawn.Value.Pose == null) {
				dbg("Actor.Pose is null!");
				continue;
			}
			var pfl = ls.ActorPoseFileLinks.First(x => x.Name == pspawn.Key);
			if(pfl.Pose.Bones == null) {
				dbg("Bones is null!");
				continue;
			}

			// cursed af: wait at most 3s per skeleton, polling at 100ms interval
			dbg("Waiting for skelly");
			var wait = await WaitForSkelly(pspawn.Value.Pose);

			if(wait) {
				dbg($"Setting pose for {pspawn.Key}");
				await ctx.Posing.ApplyPoseFile(pspawn.Value.Pose, pfl.Pose, PoseMode.All, PoseTransforms.Rotation | PoseTransforms.Scale | PoseTransforms.Position | PoseTransforms.PositionRoot);
			}
			else {
				dbg($"Skeleton timeout: Failed to set pose for actor {pspawn.Key}");
			}
			
		}

		// load offsets
		dbg("Moving actors to offsets");
		ctx.LazyExtras.actors.aof = ls.ActorOffsetFile;
		ctx.LazyExtras.actors.LoadScene(true);
	}
	private async Task<bool> WaitForSkelly(EntityPose pose) {
		/*
		This cursed abomination is a workaround to avoid redrawing actors,
		as this would move their root node back to spawn origin.

		I have yet to see it need to wait more than 500ms.
		*/
		var startAt = DateTime.Now;
		while(GetSkelly(pose)) {
			if ((DateTime.Now - startAt).TotalSeconds > 3) {
				dbg("GetSkelly timeout: Skeleton not ready.");
				return false;
			}
			await Task.Delay(100);
		}
		var elapsed = DateTime.Now - startAt;
		dbg($"Waited {elapsed.TotalMilliseconds:F2} ms for skeleton.");
		return true;
	}
	private unsafe bool GetSkelly(EntityPose pose) {
		return pose.GetSkeleton() == null;
	}


	public string ExportSceneFile() {
		var ls = Export();
		return JsonConvert.SerializeObject(ls, Formatting.Indented);
	}
	public LazyScene Export() {
		dbg("Export()");
		var actorsState = ctx.LazyExtras.ipc.Export();
		//var lights = ctx.LazyExtras.lights.Export(); // TODO
		var offsets = ctx.LazyExtras.actors.Export();
		var actors = ctx.Scene.Recurse().OfType<ActorEntity>().ToList();

		LazyScene ls = new LazyScene();

		foreach(var act in actors) {
			if(act.Pose == null) {
				dbg("Failed to export, act.Pose = null.");
				continue;
			}
			EntityPoseConverter epc = new(act.Pose);
			ls.ActorPoseFileLinks.Add(new(){Pose=epc.SaveFile(), Name=act.Name });
		}

		// pose checking

		ls.Actors = actorsState;
		ls.ActorOffsetFile = offsets;
		dbg("\tComplete.");
		return ls;
	}
	private void dbg(string s) => Ktisis.Log.Debug($"SceneManager: {s}");
}
