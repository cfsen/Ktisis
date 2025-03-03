using Dalamud.Plugin.Services;

using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.LazyExtras.Components;

namespace Ktisis.LazyExtras
{
    public class LazyBase {
		public LazyPoseComponents pose;
		public IFramework fw;
		public LazyBase(IEditorContext ctx, ISelectManager sel, IFramework fw) {
			this.pose = new(ctx);
			this.fw = fw;
		}
    }
}
