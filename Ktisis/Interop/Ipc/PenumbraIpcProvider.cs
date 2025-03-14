using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class PenumbraIpcProvider {
	private readonly GetCollections _getCollections;
	private readonly GetCollectionForObject _getCollectionForObject;
	private readonly SetCollectionForObject _setCollectionForObject;
	private readonly GetCutsceneParentIndex _getCutsceneParentIndex;
	private readonly SetCutsceneParentIndex _setCutsceneParentIndex;
    
	public PenumbraIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._getCollections = new GetCollections(dpi);
		this._getCollectionForObject = new GetCollectionForObject(dpi);
		this._setCollectionForObject = new SetCollectionForObject(dpi);
		this._getCutsceneParentIndex = new GetCutsceneParentIndex(dpi);
		this._setCutsceneParentIndex = new SetCutsceneParentIndex(dpi);
	}

	public Dictionary<Guid, string> GetCollections() => this._getCollections.Invoke();
	public (bool objectValid, bool individualSet, (Guid Id, string Name) effectiveCollection) GetCollectionForObject(IGameObject gameObject) 
		=> this._getCollectionForObject.Invoke(gameObject.ObjectIndex);
	public (PenumbraApiEc, (Guid Id, string Name)? OldCollection) SetCollectionForObject(int gameObjectIdx, Guid? collectionId, 
		bool allowCreateNew = true, bool allowDelete = true)
		=> this._setCollectionForObject.Invoke(gameObjectIdx, collectionId, allowCreateNew, allowDelete);
	public int GetAssignedParentIndex(IGameObject gameObject) {
		return this._getCutsceneParentIndex.Invoke(gameObject.ObjectIndex);
	}

	public bool SetAssignedParentIndex(IGameObject gameObject, int index) {
		Ktisis.Log.Verbose($"Setting assigned parent for '{gameObject.Name}' ({gameObject.ObjectIndex}) to {index}");
		
		var result = this._setCutsceneParentIndex.Invoke(gameObject.ObjectIndex, index);

		var success = result == PenumbraApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Penumbra parent set failed with return code: {result}");
		return success;
	}
}

