﻿using System.IO;

using Dalamud.Utility;

using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Builders;

public interface IRefImageBuilder : IEntityBuilder<ReferenceImage, IRefImageBuilder> {
	public IRefImageBuilder SetPath(string path);
}

public sealed class RefImageBuilder : EntityBuilder<ReferenceImage, IRefImageBuilder>, IRefImageBuilder {
	private string FilePath = string.Empty;
	private bool Visible = true;
	
	public RefImageBuilder(
		ISceneManager scene
	) : base(scene) { }

	protected override IRefImageBuilder Builder => this;

	public IRefImageBuilder SetPath(string path) {
		this.FilePath = path;
		return this;
	}

	public IRefImageBuilder SetVisible(bool visible) {
		this.Visible = visible;
		return this;
	}

	protected override ReferenceImage Build() {
		if (this.Name.IsNullOrEmpty())
			this.Name = Path.GetFileName(this.FilePath);
		
		return new ReferenceImage(this.Scene) {
			Name = this.Name,
			FilePath = this.FilePath,
			Visible = this.Visible
		};
	}
}
