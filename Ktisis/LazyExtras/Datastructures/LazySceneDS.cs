using Ktisis.Data.Files;
using Ktisis.LazyExtras.Components;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Datastructures;

[Serializable]
public class LazyScene {
	public List<LazyIpcFavorite> Actors { get; set; } = [];
	public List<LazyKtisisLight> Lights { get; set; } = [];
	public LazyActorOffsetFile ActorOffsetFile { get; set; }
	public List<ActorPoseFileLink> ActorPoseFileLinks { get; set;} = [];
}
public class ActorPoseFileLink {
	public string Name { get; set; } = "";
	public PoseFile Pose { get; set; } = new PoseFile();
}
