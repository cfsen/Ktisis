using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Ktisis.LazyExtras.Datastructures;

namespace Ktisis.LazyExtras.Components;
public class LazyMaths {
	// Matrices

	/// <summary>
	/// Generates world-local and inverse transformation matrices and populates `result`.
	/// </summary>
	/// <param name="q">Orientation of local space</param>
	/// <param name="pos">Position of local space in world space</param>
	/// <param name="result">Struct housing matrices</param>
	/// <returns>true on success, false if matrices cannot be inverted.</returns>
	public bool CalcWorldMatrices(Quaternion q, Vector3 pos, out WorldTransformData result) {
		Matrix4x4 m = Matrix4x4.CreateFromQuaternion(q);
		Matrix4x4 mp = Matrix4x4.CreateTranslation(pos);
		if(!Matrix4x4.Invert(m, out Matrix4x4 m_inv)) {
			dbg("Could not compute m_inv!");
			result = default;
			return false;
		}
		if(!Matrix4x4.Invert(mp, out Matrix4x4 mp_inv)) {
			dbg("Could not compute mp_inv!");
			result = default;
			return false;
		}

		result = new WorldTransformData {
			LocalToWorld = mp * m,
			LocalToWorld_Position = mp,
			LocalToWorld_Rotation = m,

			//WorldToLocal = m_inv * mp_inv,
			WorldToLocal = mp_inv * m_inv,
			WorldToLocal_Position = mp_inv,
			WorldToLocal_Rotation = m_inv
		};

		return true;
	}

	// Angles

	/// <summary>
	/// Determines angles of a vector in 3 planes.
	/// </summary>
	/// <param name="u">Vector to deconstruct</param>
	/// <param name="degrees">Return result in degrees</param>
	/// <returns>Vector3 containing angles.</returns>
	public Vector3 VectorAngles(Vector3 u, bool degrees = false) {
		Vector3 len = new() {
			X = MathF.Max(MathF.Sqrt(u.X * u.X + u.Z * u.Z), float.Epsilon),
			Y = MathF.Max(MathF.Sqrt(u.Y * u.Y + u.Z * u.Z), float.Epsilon),
			Z = MathF.Max(MathF.Sqrt(u.X * u.X + u.Y * u.Y), float.Epsilon)
		};

		Vector3 s = new() {X=float.Sign(u.X), Y=float.Sign(u.Y), Z=float.Sign(u.Z)};

		//dbg("u=" + u.ToString());
		//dbg("s= " + s.ToString());

		//// Determine which quadrant is being targeted. Left for debug purposes.
		//int[] quad = [0,0,0,0]; // Quad 1,2,3,4

		//if(s.Y >= 0 && s.Z >= 0)		quad[0] ^= 1;	// Quad 1
		//else if(s.Y >= 0 && s.Z < 0)	quad[1] ^= 1;	// Quad 2
		//else if(s.Y < 0 && s.Z < 0)	quad[2] ^= 1;	// Quad 3
		//else if(s.Y < 0 && s.Z >= 0)	quad[3] ^= 1;	// Quad 4

		//// All X<0 are invalid, TODO this should be handled better
		//if(s.X < 0) quad = [0,0,0,0];

		//dbg($"quad: [{string.Join(", ", quad)}]");

		// Initial angle calc
		Vector3 o = new() {
			X = MathF.Acos(u.X / len.X),	// XZ-plane maps to rotation around Y axis, yaw = cos(x) || sin(z)
			Y = MathF.Acos(u.Z / len.Y),	// YZ-plane maps to rotation around X axis, pitch = cos(z) || sin(y)
			Z = MathF.Acos(u.X / len.Z)		// XY-plane mapts to rotation around Z axis, roll = cos(x) || sin(y)
		};

		// Adjust for quad
		if((s.Y >= 0 && s.Z >= 0) || (s.Y < 0 && s.Z >= 0))
			o.X *= -1.0f;
		if((s.Y < 0 && s.Z >= 0) || (s.Y < 0 && s.Z < 0))
			o.Z *= -1.0f;
		if(s.X < 0) {
			o.X += MathF.PI;
			o.X *= -1.0f;
		}

		//dbg($"Y: {o.X*180/MathF.PI} P: {o.Y*180/MathF.PI} R: {o.Z*180/MathF.PI}");

		// Conversion
		if (degrees) {
			o.X *= 180 / MathF.PI;
			o.Y *= 180 / MathF.PI;
			o.Z *= 180 / MathF.PI;
		}

		return o;
	}

	// Radian conversion

	public float DegToRad(float deg) => deg/180.0f*MathF.PI;
	public float RadToDeg(float rad) => rad*180.0f/MathF.PI; 
	public double DegToRad(double deg) => deg/180.0f*MathF.PI;
	public double RadToDeg(double rad) => rad*180.0f/MathF.PI; 

	// Debug

	private void dbg(string s) => Ktisis.Log.Debug($"LazyMathsComponents: {s}");
}
