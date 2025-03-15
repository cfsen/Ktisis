using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Datastructures;

[Serializable]
public struct LazyIpcFavorites {
	public List<LazyIpcFavorite> Actors { get; set; }
	public LazyIpcFavorites() {
		this.Actors = [];
	}
}

public class LazyIpcFavorite {
	public required string Name { get; set; }
	public string? PenumbraName { get; set; }
	public Guid? PenumbraCollection { get; set; }
	public string? GlamourerDesignNameDefault { get; set; }
	public bool GlamourerUseState { get; set; }
	public JObject? GlamourerState { get; set; }
	public bool Automatic { get; set; }
	public bool Persistent { get; set; }
	public bool Present { get; set; }
}
