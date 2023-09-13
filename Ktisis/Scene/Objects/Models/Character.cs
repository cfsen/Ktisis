using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scene.Objects.Skeleton;
using Ktisis.Scene.Objects.World;

namespace Ktisis.Scene.Objects.Models; 

public class Character : WorldObject {
	// CharacterBase

	private unsafe CharacterBase* CharaBase => (CharacterBase*)this.Address;

	// Armature & Models

	private readonly Armature Armature;
	private readonly CharaModels Models;
	
	// Constructor

	public Character(nint address) : base(address) {
		this.Armature = new Armature();
		this.AddChild(this.Armature);
		this.Models = new CharaModels();
		this.AddChild(this.Models);
	}

	// Update handler

	public unsafe bool IsRendering() {
		var ptr = this.CharaBase;
		if (ptr == null) return false;
		return (ptr->UnkFlags_01 & 2) != 0 && ptr->UnkFlags_02 != 0;
	}

	public override void Update(SceneGraph scene, SceneContext _ctx) {
		this.UpdateAddress();

		// Don't do anything until fully loaded.
		if (!this.IsRendering()) return;

		base.Update(scene, _ctx);
	}

	// Retrieve address from parent object

	protected virtual void UpdateAddress() {}
}