﻿using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.Camera;

[Action("Camera_Work_Toggle")]
public class FreecamToggleAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.NO_KEY)
		}
	};

	public override bool CanInvoke() => this.Context.Editor != null;
	
	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.Context.Editor!.Cameras.ToggleWorkCameraMode();
		return true;
	}
}