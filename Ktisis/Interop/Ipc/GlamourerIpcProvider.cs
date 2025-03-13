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
	}

	// Designs
	 
	public Dictionary<Guid, string> GetDesignList() 
		=> this._getDesignList.Invoke();
	public GlamourerApiEc ApplyDesign(Guid designId, int objectIndex, uint key = 0, ApplyFlag flags = ApplyFlag.Once | ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._applyDesign.Invoke(designId, objectIndex, key, flags);
	public GlamourerApiEc ApplyDesignName(Guid designId, string playerName, uint key = 0, ApplyFlag flags = ApplyFlag.Once | ApplyFlag.Equipment | ApplyFlag.Customization)
		=> this._applyDesignName.Invoke(designId, playerName, key, flags);

	// Items

	public GlamourerApiEc SetItem(int objectIndex, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
		=> this._setItem.Invoke(objectIndex, slot, itemId, stains, key, flags);
	public GlamourerApiEc SetItemName(string playerName, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
		=> this._setItemName.Invoke(playerName, slot, itemId, stains, key, flags);
	public GlamourerApiEc SetBonusItem(int objectIndex, ApiBonusSlot slot, ulong bonusItemId, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
		=> this._setBonusItem.Invoke(objectIndex, slot, bonusItemId, key, flags);
	public GlamourerApiEc SetBonusItemName(string playerName, ApiBonusSlot slot, ulong bonusItemId, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
		=> this._setBonusItemName.Invoke(playerName, slot, bonusItemId, key, flags);
	public GlamourerApiEc SetMetaState(int objectIndex, MetaFlag types, bool newValue, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
		=> this._setMetaState.Invoke(objectIndex, types, newValue, key, flags);
	public GlamourerApiEc SetMetaStateName(string playerName, MetaFlag types, bool newValue, uint key = 0, ApplyFlag flags = ApplyFlag.Once)
		=> this._setMetaStateName.Invoke(playerName, types, newValue, key, flags);
}
