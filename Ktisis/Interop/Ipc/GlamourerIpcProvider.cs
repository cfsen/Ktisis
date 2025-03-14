using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Plugin;

using Glamourer.Api;
using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;

using Newtonsoft.Json.Linq;

namespace Ktisis.Interop.Ipc;
public class GlamourerIpcProvider {
	private GetDesignList _getDesignList;
	private ApplyDesign _applyDesign;
	private ApplyDesignName _applyDesignName;

	private SetItem _setItem;
	private SetItemName _setItemName;
	private SetBonusItem _setBonusItem;
	private SetBonusItemName _setBonusItemName;
	private SetMetaState _setMetaState;
	private SetMetaStateName _setMetaStateName;
	private GetState _getState;
	private GetStateName _getStateName;
	private GetStateBase64 _getStateBase64;
	private GetStateBase64Name _getStateBase64Name;
	private ApplyState _applyState;
	private ApplyStateName _applyStateName;
	private RevertState _revertState;
	private RevertStateName _revertStateName;
	private UnlockState _unlockState;
	private UnlockStateName _unlockStateName;
	private UnlockAll _unlockAll;
	private RevertToAutomation _revertToAutomation;
	private RevertToAutomationName _revertToAutomationName;

	public GlamourerIpcProvider(IDalamudPluginInterface dpi) {
		this._getDesignList = new GetDesignList(dpi);	
		this._applyDesign = new ApplyDesign(dpi);
		this._applyDesignName = new ApplyDesignName(dpi);

		this._setItem = new SetItem(dpi);
		this._setItemName = new SetItemName(dpi);
		this._setBonusItem = new SetBonusItem(dpi);
		this._setBonusItemName = new SetBonusItemName(dpi);
		this._setMetaState = new SetMetaState(dpi);
		this._setMetaStateName = new SetMetaStateName(dpi);

		this._getState = new GetState(dpi);
		this._getStateName = new GetStateName(dpi);
		this._getStateBase64 = new GetStateBase64(dpi);
		this._getStateBase64Name = new GetStateBase64Name(dpi);
		this._applyState = new ApplyState(dpi);
		this._applyStateName = new ApplyStateName(dpi);
		this._revertState = new RevertState(dpi);
		this._revertStateName = new RevertStateName(dpi);
		this._unlockState = new UnlockState(dpi);
		this._unlockStateName = new UnlockStateName(dpi);
		this._unlockAll = new UnlockAll(dpi);
		this._revertToAutomation = new RevertToAutomation(dpi);
		this._revertToAutomationName = new RevertToAutomationName(dpi);
		
	}

	// Designs
	 
	public Dictionary<Guid, string> GetDesignList() 
		=> this._getDesignList.Invoke();
	public GlamourerApiEc ApplyDesign(Guid designId, int objectIndex, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once | ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._applyDesign.Invoke(designId, objectIndex, key, flags);
	public GlamourerApiEc ApplyDesignName(Guid designId, string playerName, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once | ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._applyDesignName.Invoke(designId, playerName, key, flags);

	// Items

	public GlamourerApiEc SetItem(int objectIndex, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once)
		=> this._setItem.Invoke(objectIndex, slot, itemId, stains, key, flags);
	public GlamourerApiEc SetItemName(string playerName, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key = 0,
		ApplyFlag flags = ApplyFlag.Once)
		=> this._setItemName.Invoke(playerName, slot, itemId, stains, key, flags);
	public GlamourerApiEc SetBonusItem(int objectIndex, ApiBonusSlot slot, ulong bonusItemId, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once)
		=> this._setBonusItem.Invoke(objectIndex, slot, bonusItemId, key, flags);
	public GlamourerApiEc SetBonusItemName(string playerName, ApiBonusSlot slot, ulong bonusItemId, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once)
		=> this._setBonusItemName.Invoke(playerName, slot, bonusItemId, key, flags);
	public GlamourerApiEc SetMetaState(int objectIndex, MetaFlag types, bool newValue, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once)
		=> this._setMetaState.Invoke(objectIndex, types, newValue, key, flags);
	public GlamourerApiEc SetMetaStateName(string playerName, MetaFlag types, bool newValue, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Once)
		=> this._setMetaStateName.Invoke(playerName, types, newValue, key, flags);

	// State

	public (GlamourerApiEc, JObject?) GetState(int objectIndex, uint key = 0)
		=> this._getState.Invoke(objectIndex, key);
	public (GlamourerApiEc, JObject?) GetStateName(string playerName, uint key = 0)
		=> this._getStateName.Invoke(playerName, key);
	public (GlamourerApiEc, string?) GetStateBase64(int objectIndex, uint key = 0)
		=> this._getStateBase64.Invoke(objectIndex, key);
	public (GlamourerApiEc, string?) GetStateBase64Name(string objectName, uint key = 0)
		=> this._getStateBase64Name.Invoke(objectName, key);
	public GlamourerApiEc ApplyState(JObject applyState, int objectIndex, uint key = 0,	
		ApplyFlag flags = ApplyFlag.Equipment | ApplyFlag.Customization | ApplyFlag.Lock)
		=> this._applyState.Invoke(applyState, objectIndex, key, flags);
	public GlamourerApiEc ApplyStateName(JObject applyState, string playerName,	uint key = 0, 
		ApplyFlag flags = ApplyFlag.Equipment | ApplyFlag.Customization | ApplyFlag.Lock)
		=> this._applyStateName.Invoke(applyState, playerName, key, flags);
	public GlamourerApiEc RevertState(int objectIndex, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._revertState.Invoke(objectIndex, key, flags);
	public GlamourerApiEc RevertStateName(string playerName, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._revertStateName.Invoke(playerName, key, flags);
	public GlamourerApiEc UnlockState(int objectIndex, uint key = 0)
		=> this._unlockState.Invoke(objectIndex, key);
	public GlamourerApiEc UnlockStateName(string playerName, uint key = 0)
		=> this._unlockStateName.Invoke(playerName, key);
	public int UnlockAll(uint key)
		=> this._unlockAll.Invoke(key);
	public GlamourerApiEc RevertToAutomation(int objectIndex, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._revertToAutomation.Invoke(objectIndex, key, flags);
	public GlamourerApiEc RevertToAutomationName(string playerName, uint key = 0, 
		ApplyFlag flags = ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._revertToAutomationName.Invoke(playerName, key, flags);
	//public event Action<nint> StateChanged;
	//public event Action<nint, StateChangeType> StateChangedWithType;
	//public event Action<nint, StateFinalizationType> StateFinalized;
	//public event Action<bool>? GPoseChanged;

}
