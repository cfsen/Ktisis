using Ktisis.Data.Files;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;
public class LazyCameraSettings : JsonFile {
	public string Name { get; set; } = "";
	public Vector3 Position { get; set; } = Vector3.Zero;
	public Quaternion Rotation { get; set; } = Quaternion.Identity;
	public float fov { get; set; } = 0.0f;
	public float zoom { get; set; } = 1.0f;
}
public class LazyCameraComponents {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;

	public LazyCameraComponents(IEditorContext ctx, GuiManager gui) {
		this._ctx = ctx;
		this._gui = gui;
	}
}
