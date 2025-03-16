using Ktisis.Common.Utility;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Components;

public class LazyKtisisLight : JsonFile {
	public string Name { get; set; } = "";
	public uint Type { get; set; } = 0;
	public float AreaAngleX { get; set; } = 0.0f;
	public float AreaAngleY { get; set; } = 0.0f;
	public float Angle { get; set; } = 0.0f;
	public float FalloffAngle { get; set; } = 0.0f;
	public uint FalloffType { get; set; } = 0;
	public float Falloff { get; set; } = 0.0f;
	public float Intensity { get; set; } = 0.0f;
	public float Range { get; set; } = 0.0f;
	public float ShadowRange { get; set; } = 0.0f;
	public float ShadowNear { get; set; } = 0.0f;
	public float ShadowFar { get; set; } = 0.0f;
	public uint Flags { get; set; } = 0;
	public Vector3 Position { get; set; } = Vector3.Zero;
	public Quaternion Rotation { get; set; } = Quaternion.Identity;
	public Vector3 Scale { get; set; } = Vector3.Zero;
	public Vector3 Color { get; set; } = Vector3.Zero;
}

public class LazyLightsComponents {
	private readonly IEditorContext _ctx;
	public string _json;
	public LazyLightsComponents(IEditorContext ctx) { 
		this._ctx = ctx;
		this._json = "";
	}
	#region Situational spawners
	public async void SpawnStudioLights() {
		//var selected = this._ctx.Selection.GetSelected().OfType<ActorEntity>().FirstOrDefault();
		var selected = this.ResolveActorEntity();
		if (selected == null)
			return;

		Vector3? actorPos = selected.GetTransform()?.Position ?? Vector3.Zero;
		Vector3? cameraPos = this._ctx.Cameras?.Current?.GetPosition() ?? Vector3.Zero;

		if (actorPos == null || cameraPos == null)
			return;

		// Determine placement of cameras
		Vector3 v = cameraPos.Value - actorPos.Value;

		// lights
		string[] lnames = { "Key", "Fill", "Rim" };
		var light = this._ctx.Scene.Factory.CreateLight(Structs.Lights.LightType.AreaLight);
		foreach (var lname in lnames)
		{
			light.SetName(lname);
			await light.Spawn();
		}

		// adjust the lights
		var keylight = this._ctx.Scene.Children.OfType<LightEntity>();
		foreach (LightEntity l in keylight)
		{
			Transform t = l?.GetTransform() ?? new Transform();
			switch (l?.Name)
			{
				case "Key":
					float angleKey = (float)(11*Math.PI/6);
					t.Position.X = actorPos.Value.X + 3.5f*(float)Math.Sin(angleKey);
					t.Position.Z = actorPos.Value.Z + 3.5f*(float)Math.Cos(angleKey);
					t.Position.Y = actorPos.Value.Y + 1.0f;
					t.Rotation = Quaternion.CreateFromYawPitchRoll(this.DegToRad(150), 0f, 0f);
					break;
				case "Rim":
					t.Position = actorPos.Value;
					t.Position.Y += 7.0f;
					t.Rotation = Quaternion.CreateFromYawPitchRoll(0.0f, (float)Math.PI/2, 0.0f);
					break;
				case "Fill":
					float angleFill = (float)(Math.PI);
					t.Position.X = actorPos.Value.X + 3.5f*(float)Math.Sin(angleFill);
					t.Position.Z = actorPos.Value.Z + 3.5f*(float)Math.Cos(angleFill);
					t.Position.Y = actorPos.Value.Y + 1.0f;
					t.Rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f, 0f);
					break;
			}
			l?.SetTransform(t);
			l?.Update();
			this.SetLight(l, l.Name);
		}
	}

	public async void SpawnStudioApartmentAmbientLights() {
		Vector3 ambientLightPos = new Vector3(0.0f, 7.250f, -18.820f);
		var lspawn = this._ctx.Scene.Factory.CreateLight(Structs.Lights.LightType.AreaLight);
		lspawn.SetName("Ambient Light");
		await lspawn.Spawn();
		var l = this._ctx.Scene.Children.OfType<LightEntity>().Where(s => s.Name == "Ambient Light").First();
		if (l != null)
		{
			this.SetExteriorLight(l);
		}
	}

	private unsafe void SetExteriorLight(LightEntity l) {
		if (l != null)
		{
			var lobj = l.GetObject();
			var light = lobj != null ? lobj->RenderLight : null;
			if (light == null) return;
			light->FalloffAngle = 180.0f;
			float intensity = 2.730f;
			light->Color.Intensity = intensity;
			light->AreaAngle = new Vector2(this.DegToRad(16.0f), 0.0f);
			light->Color.RGB = new Vector3(64/255.0f, 156/255.0f, 255/255.0f);
			light->ShadowFar = 23.450f;
			light->ShadowNear = 14.870f;
			light->Flags ^= Structs.Lights.LightFlags.ObjectShadow;
			light->Flags ^= Structs.Lights.LightFlags.CharaShadow;


			Transform t = new Transform();
			t.Position = new Vector3(0.0f, 7.250f, -18.820f);
			t.Rotation = Quaternion.Zero;
			t.Scale = new Vector3(13.151f, 13.151f, 13.151f);
			l.SetTransform(t);
			l.Update();
		}
	}

	private unsafe void SetLight(LightEntity lightEnt, string key) {
		var sceneLight = lightEnt.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		light->FalloffAngle = 90.0f;
		float intensity = 0f;
		switch (key)
		{
			case "Key":
				intensity = 0.5f;
				light->AreaAngle = new Vector2(this.DegToRad(-14.0f), 0.0f);
				light->Range = 10.0f;
				break;
			case "Rim":
				intensity = 0.15f;
				light->Range = 7.0f;
				break;
			case "Fill":
				intensity = 0.2f;
				light->AreaAngle = new Vector2(this.DegToRad(-14.0f), 0.0f);
				light->Range = 10.0f;
				break;
		}
		light->Color.Intensity = intensity;
		light->Color.RGB = new Vector3(255/255.0f, 244/255.0f, 229/255.0f);
	}

	#endregion

	#region Lights preset load/save

	public void LightsSave() {
		var l = this._ctx.Scene.Children.OfType<LightEntity>();
		if (l != null)
		{
			List<LazyKtisisLight> buffer = new List<LazyKtisisLight>();
			foreach (var entity in l)
			{
				var r = this.ReadLight(entity);
				if (r != null)
				{
					r.Name = entity.Name;
					var _ = entity.GetTransform();
					r.Position = _.Position;
					r.Rotation = _.Rotation;
					r.Scale = _.Scale;
					buffer.Add(r);
				}
			}
			var json = new JsonFileSerializer().Serialize(buffer);
			this._json = json;
		}
	}

	private unsafe LazyKtisisLight? ReadLight(LightEntity lightEnt) {
		var sceneLight = lightEnt.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return null;
		LazyKtisisLight r = new LazyKtisisLight();
		r.Type = (uint)light->LightType;
		r.AreaAngleX = light->AreaAngle.X;
		r.AreaAngleY = light->AreaAngle.Y;
		r.Angle = light->LightAngle;
		r.FalloffAngle = light->FalloffAngle;
		r.FalloffType = (uint)light->FalloffType;
		r.Falloff = light->Falloff;
		r.ShadowFar = light->ShadowFar;
		r.ShadowNear = light->ShadowNear;
		r.ShadowRange = light->CharaShadowRange;
		r.Flags = (uint)light->Flags;
		r.Color = light->Color.RGB;
		r.Intensity = light->Color.Intensity;
		r.Range = light->Range;
		return r;
	}

	private unsafe void SetupImportedLight(LightEntity le, LazyKtisisLight kl) {
		var sl = le.GetObject();
		var l = sl != null ? sl->RenderLight : null;
		if (l == null) return;
		l->AreaAngle = new Vector2(kl.AreaAngleX, kl.AreaAngleY);
		l->CharaShadowRange = kl.ShadowRange;
		l->ShadowFar = kl.ShadowFar;
		l->ShadowNear = kl.ShadowNear;
		l->Color.RGB = kl.Color;
		l->Color.Intensity = kl.Intensity;
		l->Falloff = kl.Falloff;
		l->FalloffAngle = kl.FalloffAngle;
		l->FalloffType = (FalloffType)kl.FalloffType;
		l->Flags = (LightFlags)kl.Flags;
		l->LightAngle = kl.Angle;
		l->LightType = (LightType)kl.Type;
		l->Range = kl.Range;

		Transform t = new Transform();
		t.Position = kl.Position;
		t.Rotation = kl.Rotation;
		t.Scale = kl.Scale;
		le.SetTransform(t);
		le.Update();
	}

	public async void ImportLightJson(string s) {
		List<LazyKtisisLight> decode = [];
		try	{
			decode = new JsonFileSerializer().Deserialize<List<LazyKtisisLight>>(s);
		} 
		catch (Exception e)	{
			Ktisis.Log.Debug("Light import: Failed to import." + e.ToString());
			return;
		}

		if (decode == null || decode.Count == 0) return;

		foreach (LazyKtisisLight l in decode) {
			Ktisis.Log.Debug("Light imported:" + l.Name);
			await this.LoadKtisisLights(l);
		}
	}

	private async Task LoadKtisisLights(LazyKtisisLight kl) {
		var lspawn = this._ctx.Scene.Factory.CreateLight((LightType)kl.Type);
		lspawn.SetName("_tmp");
		await lspawn.Spawn();
		var l = this._ctx.Scene.Children.OfType<LightEntity>().FirstOrDefault(s => s.Name == "_tmp");
		if (l != null) {
			l.Name = kl.Name;
			this.SetupImportedLight(l, kl);
		}
	}
	#endregion

	#region Lights utilities
	public void LightsDeleteAll() {
		var l = this._ctx.Scene.Children.OfType<LightEntity>().ToList();
		if(l == null) return;

		foreach(LightEntity le in l) {
			le.Delete();
		}
	}
	#endregion

	#region Ktisis/math/etc utilities
	private ActorEntity? ResolveActorEntity() {
		// Resolves the parent actor entity of any bone. Recursion warning.
		var selected = this._ctx.Selection.GetSelected().FirstOrDefault();
		if (selected == null)
			return null;

		ActorEntity? actor = this.Backtrack(selected, 0, 10);
		if (actor != null)
			return actor;
		return null;
	}

	private ActorEntity? Backtrack(object node, int depth = 0, int maxdepth = 0) {
		// Recursion used in ResolveActorEntity.
		if (node is ActorEntity ae)
			return ae;
		if (depth >= maxdepth)
			return null;

		var parentProperty = node.GetType().GetProperty("Parent");
		if (parentProperty == null)
			return null;

		var parent = parentProperty.GetValue(node);
		if (parent != null)
		{
			var res = Backtrack(parent, depth+1, maxdepth);
			if (res != null)
				return res;
		}
		return null;
	}

	private double RadToDeg(float rad) {
		return rad * 180 / Math.PI;
	}
	private float DegToRad(float deg) {
		return (float)(deg * Math.PI / 180);
	}

	public Vector3 RGBToVector(int r, int g, int b) {
		return new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
	}

	#endregion

	// SceneManager 

	// TODO
	//public List<LazyKtisisLight> Export() {
	//	return 
	//}
}

