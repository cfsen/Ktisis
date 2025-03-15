using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Interfaces;
[Flags]
public enum LazyWidgetCat {
	None = 0,
	Pose = 1,
	Gesture = 2,
	Camera = 4,
	Light = 8,
	Selection = 16,
	Misc = 32,
	Transformers = 64,
	Scene = 128,
	Smart = 256
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
