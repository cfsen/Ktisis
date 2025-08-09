using Dalamud.Interface;
using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Entities.Game;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class DbgWidget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;

	public DbgWidget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.None;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
		this.InToolbelt = false;

		this.ctx = ctx;
		this.lui = new();
		this.uis = lui.uis;
	}
	public void UpdateScaling() {
		this.uis.RefreshScale();
	}
	public void Draw() {
		ImGui.BeginGroup();
		lui.DrawHeader(FontAwesomeIcon.Wrench, "Debugger");
		
		if(ImGui.Button("Run"))
		{
			DbgFunc();
		}
		DrawImportExportControls();

		lui.DrawFooter();
		ImGui.EndGroup();
	}

	private (LazyIOFlag, string) IODataDispatcher() {
		return (LazyIOFlag.Save | LazyIOFlag.Pose, "pose data");
	}
	private void IODataReceiver(bool success, List<string>? data) {
		if(success) {
			foreach(var d in data!)
				dbg(d);
		}
	}
	private void DrawImportExportControls() {
		lui.BtnSave(IODataDispatcher, "WDEMO_Dispathcer", "Save", ctx.LazyExtras.io);
		lui.BtnLoad(LazyIOFlag.Load | LazyIOFlag.Pose, IODataReceiver, "WDEMO_Receiver", "Load", ctx.LazyExtras.io);
	}

	private void DbgFunc() {
		dbg("Running.");
		if(ctx.Selection.GetSelected().FirstOrDefault() is not ActorEntity ae)
		{
			dbg(ctx.Selection.GetSelected().GetType().ToString());
			return;
		}

		Quaternion? q = ae.GetTransform()?.Rotation;
		if(q == null)
		{
			dbg("invalid q");
			return;
		}
		Quaternion qv = q.Value;

		//Vector3 v = ctx.LazyExtras.math.QuaternionToEulerAngles(qv);
		//dbg(v.ToString());
		//dbg($"{ctx.LazyExtras.math.RadToDeg(v[0]).ToString()}, {ctx.LazyExtras.math.RadToDeg(v[1]).ToString()}, {ctx.LazyExtras.math.RadToDeg(v[2]).ToString()}");

	}

	private void dbg(string s) => Ktisis.Log.Debug($"DbgWidget: {s}");
}
