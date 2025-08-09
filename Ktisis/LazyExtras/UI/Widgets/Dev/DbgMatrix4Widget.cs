using Dalamud.Interface;
using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.LazyExtras.Interfaces;

using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class DbgMatrix4Widget :ILazyWidget {
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	private IEditorContext ctx;
	private LazyUi lui;
	private LazyUiSizes uis;

	public DbgMatrix4Widget(IEditorContext ctx) {
		this.Category = LazyWidgetCat.Misc;
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
		lui.DrawHeader(FontAwesomeIcon.Ad, "Demo");
		
		ImGui.Text("Demo widget");

		lui.DrawFooter();
		ImGui.EndGroup();
	}
	//public void dbgCsM4() {
	//	this.DrawM4Table(this._dbgM4, "FIRST");
	//	//ImGui.Text(this.dbgV3(this._dbgM4.Translation));
	//	this.DrawM4Table(this._dbgM4_2, "SECOND");
	//	//ImGui.Text(this.dbgV3(this._dbgM4_2.Translation));
	//	this.DrawM4Table(this._dbgM4_3, "THIRD");
	//	//ImGui.Text(this.dbgV3(this._dbgM4_3.Translation));
	//	this.DrawM4Table(this._dbgM4_2, "FOURTH");
	//}

	//// Saved for future debug
	//public void dbgMatrixInspector(string bone) {
	//	if(this.ResolveActorEntity() is not ActorEntity ae) return;
	//	if(ae.Recurse().Where(x => x.Name == bone && x is BoneNode).FirstOrDefault() is not BoneNode bn) return;
	//	var m = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(bn.GetTransform()?.Rotation ?? Quaternion.Identity));
	//	//dbg(m[0,0].ToString());
	//	//dbg(m.ToString());
	//	//dbg("Okay");
	//	this.DrawM4Table(m, bn.Name);
	//	Matrix4x4 mi = new Matrix4x4();
	//	if(Matrix4x4.Invert(m, out mi)) {
	//		this.DrawM4Table(mi, bn.Name + "(inv)");
	//		this.DrawM4Table(Matrix4x4.Multiply(m, mi), "A*A^-1");
	//	}

	//}

	//// Saved for future debug
	//private void DrawM4Table(Matrix4x4 m, string lbl) {
	//	ImGui.Text(lbl);
	//	if(ImGui.BeginTable($"{lbl}###{lbl}", 4)) {
	//		int i = 0, r = 0, c = 0;
	//		for(i = 0; i < 16; i++) {
	//			ImGui.TableNextColumn();
	//			ImGui.Text(m[r,c].ToString());
	//			if(c == 3) { r++; c=0; }
	//			else c++;
	//		}
	//		ImGui.EndTable();
	//	}
	//}
}
