
using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
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

	public LazyActorOffsetFile aof;
	public LazyActorOffsetsComponents(IEditorContext _ctx) {
		this.ctx = _ctx;
		this.aof = new();
	}	
	public void UpdateAOF(){
		aof.Actors.Clear();
		GetSceneActors();
	}

	// Data collection

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
	private void GetSceneCameras() {}
	private void GetSceneLights() {}
	private void GetActorPenumbraCollection() {}
	private void GetActorGlamourerDesign() {}
	
	// Input verification 

	private void CheckImport(string name) {
		/* check:
		 * - actor count
		 * - attempt to map actors to file entries
		 * return errors for ux
		 * */
	}

	// Reference frame/local mode

	private void CreateLocal() {}
	private void ToLocal() {}

	// Scene entity interaction

	public void ApplyOffset() {
		var actors = ctx.Scene.Recurse().OfType<ActorEntity>();
		if(!actors.Any()) return;

		// TODO yuck
		foreach(ActorEntity actor in actors) {
			foreach(LazyActorOffsetElement el in aof.Actors) {
				if(el.Name == actor.Name) {
					Transform? t = actor.GetTransform();
					if(t != null) {
						Transform tx = t;
						tx.Position = el.Position;
						tx.Rotation = el.Rotation;
						actor.SetTransform(tx);
					}
				}
			}
		}
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
