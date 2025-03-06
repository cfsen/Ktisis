using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Interfaces;
public enum LazyWidgetCat {
	Pose,
	Camera,
	Light,
	Animation,
	Selection,
	Misc
}
public interface ILazyWidget {
	int CustomOrder { get; set; }
	LazyWidgetCat Category { get; }
	bool InToolbelt { get; set; }
	bool SupportsToolbelt { get; }
	Vector2 SizeToolbelt { get; }
	void Draw();
	void UpdateScaling();
}
