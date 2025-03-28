﻿using System;
using System.Collections.Generic;
using System.Linq;

using GLib.Lists;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interop.Ipc;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Popup;

public class ActorCollectionPopup : KtisisPopup {
	private readonly IEditorContext _ctx;
	private readonly ActorEntity _entity;
	private readonly PenumbraIpcProvider _ipc;
	private readonly ListBox<KeyValuePair<Guid, string>> _list;
	
	public ActorCollectionPopup(
		IEditorContext ctx,
		ActorEntity entity
	) : base("##ActorCollectionPopup") {
		this._ctx = ctx;
		this._entity = entity;
		this._ipc = ctx.Plugin.Ipc.GetPenumbraIpc();
		this._list = new ListBox<KeyValuePair<Guid, string>>("##CollectionList", this.DrawItem);
	}

	private (Guid Id, string Name) _current = (Guid.Empty, string.Empty);

	protected override void OnDraw() {
		if (!this._entity.IsValid || !this._ctx.Plugin.Ipc.IsPenumbraActive) {
			this.Close();
			Ktisis.Log.Info("Stale, closing.");
			return;
		}

		var penumbraGetCollection = this._ipc.GetCollectionForObject(this._entity.Actor);
		this._current = penumbraGetCollection.effectiveCollection;
		ImGui.Text($"Assigning collection for {this._entity.Name}");
		ImGui.TextDisabled($"Currently set to: {this._current}");

		var list = this._ipc.GetCollections().ToList();
		if (this._list.Draw(list, out var selected)) {
			Ktisis.Log.Debug(this._entity.Actor.ObjectIndex.ToString());
			var penumbraSetCollection = this._ipc.SetCollectionForObject(this._entity.Actor.ObjectIndex, selected.Key);
			if (penumbraGetCollection.objectValid)
				this._entity.Redraw();
		}
	}

	private bool DrawItem(KeyValuePair<Guid, string> item, bool _) => ImGui.Selectable(item.Value, item.Key == this._current.Id);
}
