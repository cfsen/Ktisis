using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Datastructures;

[Serializable]
public struct LazyActorOffsetFile {
	public List<LazyActorOffsetElement> Actors { get; set; }
	public List<LazyActorOffsetElement> Lights { get; set; }
	public List<LazyActorOffsetElement> Cameras { get; set; }
	public LazyActorOffsetFile() {
		this.Actors = [];
		this.Lights = [];
		this.Cameras = [];
	}
}

public struct LazyActorOffsetElement {
	public string Name { get; set; }
	public string Penumbra { get; set; }
	public string Glamourer { get; set; }
	public Vector3 Position { get; set; }
	public Quaternion Rotation { get; set; }
}
