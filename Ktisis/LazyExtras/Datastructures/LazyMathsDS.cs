using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.LazyExtras.Datastructures;

public struct WorldTransformData {
	public Matrix4x4 LocalToWorld { get; init; }
	public Matrix4x4 LocalToWorld_Position { get; init; }
	public Matrix4x4 LocalToWorld_Rotation { get; init; }

	public Matrix4x4 WorldToLocal { get; init; }
	public Matrix4x4 WorldToLocal_Position { get; init; }
	public Matrix4x4 WorldToLocal_Rotation { get; init; }
}
