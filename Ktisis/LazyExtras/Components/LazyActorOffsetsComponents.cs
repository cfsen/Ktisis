
using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.LazyExtras.Datastructures;
using Ktisis.LazyExtras.UI.Widgets;
using Ktisis.Scene.Entities.Game;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazyActorOffsetsComponents {
	private IEditorContext ctx;

	private LazyMaths mathx;
	private WorldTransformData wtdFile;
	private WorldTransformData wtdScene;

	private List<ActorMap> am;
	private Dictionary<string, ActorEntity> sceneActors;

	public LazyActorOffsetFile aof;
	public LazyActorOffsetsComponents(IEditorContext _ctx) {
		this.ctx = _ctx;
		this.mathx = new LazyMaths();

		this.aof = new();
		this.am = [];
		this.sceneActors = new Dictionary<string, ActorEntity>();
	}	
	
	// Data containers (sigh)

	private class ActorMap(LazyActorOffsetElement _laof, ActorEntity _ae) {
		public LazyActorOffsetElement laof {get;set;} = _laof;
		public ActorEntity ae {get;set;}= _ae;
	}

	// Data collection

	public void UpdateAOF(){
		aof.Actors.Clear();
		GetSceneActors();
	}
	private void GetSceneActors() {
		var actors = ctx.Scene.Recurse().OfType<ActorEntity>();
		if(!actors.Any()) return;
		
		foreach(ActorEntity actor in actors) {
			LazyActorOffsetElement el = new() {
				Name = actor.Name,
				Position = actor.GetTransform()?.Position ?? Vector3.Zero,
				Rotation = actor.GetTransform()?.Rotation ?? Quaternion.Zero
			};

			this.aof.Actors.Add(el);
		}
	}
	// TODO should prob go in the final class that joins all of these together
	//private void GetSceneCameras() {}
	//private void GetSceneLights() {}
	//private void GetIpcStates() {}	// TODO just use stuff from LazyIpc

	// Validation

	private bool ValidateScene() {
		dbg("ValidateScene()");

		am.Clear();
		sceneActors.Clear();

		if(!aof.Actors.Any()) {
			dbg("No actors in file.");
			return false;
		}

		var actors = ctx.Scene.Recurse().OfType<ActorEntity>();
		if(!actors.Any()) return false;
		sceneActors = actors.ToDictionary(x => x.Name);

		if(!MapActors(actors)) return false;
		dbg("Scene validated");
		return true;
	}

	// Mapping

	public bool MapActors(IEnumerable<ActorEntity> actors) {
		foreach(LazyActorOffsetElement laof in aof.Actors)
			if (sceneActors.TryGetValue(laof.Name, out ActorEntity? value))
				am.Add(new ActorMap(laof, value));

		if(am.Count < sceneActors.Count) {
			dbg("Not enough scene actors");
			return false;
		}

		dbg("Mapping successful.");
		return true;
	}

	// Reference frame/local mode

	private void ComputeMatricies(Vector3 filePosition, Quaternion fileOrientation, Vector3 worldPosition, Quaternion worldOrientation) {
		mathx.CalcWorldMatrices(worldOrientation, worldPosition, out wtdScene);
		mathx.CalcWorldMatrices(fileOrientation, filePosition, out wtdFile);
	}
	private void ToLocal() {}

	// Scene entity interaction

	public void LoadScene(bool localMode) {
		if(localMode)
			ApplyOffsetLocal();
		else
			ApplyOffsetWorld();
	}
	private void ApplyOffsetWorld() {
		if(!ValidateScene()) return;
		dbg("Loading world offsets.");

		foreach(var act in am) {
			// capture state
			var epc = new EntityPoseConverter(act.ae.Pose!);
			var initial = epc.Save();

			if(act.ae.GetTransform() is Transform actXfm) {
				dbg($"Applying offset to: {act.ae.Name}");

				Transform? _ = new();
				_.Position = act.laof.Position;
				_.Rotation = act.laof.Rotation;

				ctx.LazyExtras.fw.RunOnTick(() => {
					act.ae.SetTransform(_);
					});
			}
			else
				dbg($"Failed to get transform for: {act.ae.Name}");

			// push history
			var final = epc.Save();
			HistoryAdd(MementoCreate(epc, initial, final, PoseTransforms.Rotation | PoseTransforms.PositionRoot | PoseTransforms.Position | PoseTransforms.Scale));
		}
	}
	public void ApplyOffsetLocal() {
		if(!ValidateScene()) return;

		ActorMap anchor = am.First();
		dbg($"Anchor to: {anchor.ae.Name}");
		
		if(anchor.ae.GetTransform() is not Transform anchorXfm) return;
		dbg("Valid anchor world state.");

		ComputeMatricies(anchor.laof.Position, anchor.laof.Rotation, anchorXfm.Position, anchorXfm.Rotation);
		dbg("Matrices computed");


		foreach(var act in am) {
			// capture state
			var epc = new EntityPoseConverter(act.ae.Pose!);
			var initial = epc.Save();

			if(act == anchor) {
				// TODO
					// alternative to this: you could use current world orientation of anchor
					// in which case you leave the anchor untouched, and see the next TODO
				// uses orientation from file
				anchorXfm.Rotation = act.laof.Rotation;
				ctx.LazyExtras.fw.RunOnTick(() => {
					act.ae.SetTransform(anchorXfm);
				});
				continue;
			}

			if(act.ae.GetTransform() is Transform actXfm) {
				dbg($"Applying offset to: {act.ae.Name}");
				//dbg($"\tWorld pos: {actXfm.Position}");
				//dbg($"\tFile pos: {act.laof.Position}");

				// xfm position to anchor local, then to current world
				Vector3 fileToLocal = Vector3.Transform(act.laof.Position, wtdFile.WorldToLocal_Position);
				Vector3 localToWorld = Vector3.Transform(fileToLocal, wtdScene.LocalToWorld_Position);

				// TODO transform orientation, optional

				Transform? _ = new();
				_.Position = localToWorld;
				_.Rotation = act.laof.Rotation;

				ctx.LazyExtras.fw.RunOnTick(() => {
					act.ae.SetTransform(_);
					});

				//dbg($"\tf2l pos: {fileToLocal}");
				//dbg($"\tl2w pos: {localToWorld}");
			}
			else
				dbg($"Failed to get transform for: {act.ae.Name}");

			// push history
			var final = epc.Save();
			HistoryAdd(MementoCreate(epc, initial, final, PoseTransforms.Rotation | PoseTransforms.PositionRoot | PoseTransforms.Position | PoseTransforms.Scale));
		}
		
	}

	// Memento helpers
		// TODO REFAC
	private void HistoryAdd(PoseMemento pm) {
		this.ctx.Actions.History.Add(pm);
	}
	private PoseMemento MementoCreate(EntityPoseConverter epc, PoseContainer initial, PoseContainer final, PoseTransforms flags) {
		return new PoseMemento(epc) {
			Modes = PoseMode.All,
			Transforms = flags,
			Bones = null,
			Initial = initial,
			Final = final
		};
	}

	// JSON import/export

	public void ImportOffset(string json) {
		aof = GenerateOffsetObject(json);
	}
	public LazyActorOffsetFile GenerateOffsetObject(string json) {
		var settings = new JsonSerializerSettings {
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore
		};
		return JsonConvert.DeserializeObject<LazyActorOffsetFile>(json, settings);
	}
	public string ExportOffsets() {
		return JsonConvert.SerializeObject(aof, Formatting.Indented);
	}
	private void dbg(string s) => Ktisis.Log.Debug($"LazyActorOffsets: {s}");
}
