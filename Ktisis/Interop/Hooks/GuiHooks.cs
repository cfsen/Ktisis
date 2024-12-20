using System;

using Dalamud.Hooking;

using Ktisis.Structs.Actor;

namespace Ktisis.Interop.Hooks {
	internal class GuiHooks {
		// Target name in the GPose window

		internal delegate void TarNameDelegate(IntPtr a1);
		internal static Hook<TarNameDelegate> TarNameHook = null!;

		internal static string ReplaceTarName = "(Hidden by Ktisis)";
		internal unsafe static void UpdateTarName(IntPtr a1) {
			try {
				string nameToDisplay = ReplaceTarName;

				if (Ktisis.Configuration.DisplayCharName) {
					var target = Ktisis.GPoseTarget;
					if (target != null) {
						var actor = (Actor*)Ktisis.GPoseTarget!.Address;
						if (actor != null) {
							var name = actor->GetName();
							if (name != null) nameToDisplay = name;
						}
					}
				}

				for (var i = 0; i < nameToDisplay.Length; i++)
					*(char*)(a1 + 488 + i) = nameToDisplay[i];
			} catch (Exception e) {
				Logger.Error(e, "Error in UpdateTarName. Disabling hook.");
				TarNameHook.Disable();
			}

			TarNameHook.Original(a1);
		}

		// Init & dispose

		internal static void Init() {
			var tarName = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 49 8D 8F ?? ?? ?? ?? 48 83 C4 20");
			TarNameHook = Services.Hooking.HookFromAddress<TarNameDelegate>(tarName, UpdateTarName);
            TarNameHook.Enable();
		}

		internal static void Dispose() {
			TarNameHook.Disable();
			TarNameHook.Dispose();
		}
	}
}
