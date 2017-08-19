using System;
using UnityEngine;

// TODO: try to use BigInteger or BigRational once .Net 4.0 is available
public struct WorldPos : IEquatable<WorldPos>
{
	Vector3 insideSectorPosition;
	long sectorX, sectorY, sectorZ;

	const int sectorCubeSideLength = 100;
	//const double offset = 0.5;


	public static readonly WorldPos Zero = new WorldPos();


	public WorldPos normalized
	{
		get
		{
			var x = sectorX * sectorCubeSideLength + (double)insideSectorPosition.x;
			var y = sectorY * sectorCubeSideLength + (double)insideSectorPosition.y;
			var z = sectorZ * sectorCubeSideLength + (double)insideSectorPosition.z;

			var invLen = 1.0 / Math.Sqrt(x * x + y * y + z * z);

			return this.MultiplyBy(invLen);
		}
	}

	public WorldPos(float x, float y, float z)
	{
		insideSectorPosition = new Vector3(x, y, z);
		sectorX = 0;
		sectorY = 0;
		sectorZ = 0;
		MoveSectorIfNeeded();
	}

	public WorldPos(Vector3 pos)
	{
		insideSectorPosition = pos;
		sectorX = 0; sectorY = 0; sectorZ = 0;
		MoveSectorIfNeeded();
	}

	public Vector3 Remainder()
	{
		var x = Mathf.Floor(insideSectorPosition.x);
		var y = Mathf.Floor(insideSectorPosition.y);
		var z = Mathf.Floor(insideSectorPosition.z);

		return new Vector3(
			(float)(insideSectorPosition.x - x),
			(float)(insideSectorPosition.y - y),
			(float)(insideSectorPosition.z - z)
		);
	}

	void MoveSectorIfNeeded()
	{
		long sector_add;

		sector_add = (long)(insideSectorPosition.x / sectorCubeSideLength);
		insideSectorPosition.x -= sectorCubeSideLength * sector_add;
		sectorX += sector_add;

		sector_add = (long)(insideSectorPosition.y / sectorCubeSideLength);
		insideSectorPosition.y -= sectorCubeSideLength * sector_add;
		sectorY += sector_add;

		sector_add = (long)(insideSectorPosition.z / sectorCubeSideLength);
		insideSectorPosition.z -= sectorCubeSideLength * sector_add;
		sectorZ += sector_add;
	}
	public static double Distance(WorldPos a, WorldPos b)
	{
		return a.Distance(b);
	}
	public double Distance(WorldPos worldPos)
	{
		return this.Towards(worldPos).ToVector3().magnitude;
	}
	public double DistanceSqr(WorldPos worldPos)
	{
		return this.Towards(worldPos).ToVector3().sqrMagnitude;
	}

	public Vector3 ToVector3()
	{
		return new Vector3(
			(float)(insideSectorPosition.x + (sectorX * sectorCubeSideLength)),
			(float)(insideSectorPosition.y + (sectorY * sectorCubeSideLength)),
			(float)(insideSectorPosition.z + (sectorZ * sectorCubeSideLength))
		);
	}
	public Vector4 ToVector4()
	{
		return ToVector3();
	}

	public bool Equals(WorldPos other)
	{
		return
			other.insideSectorPosition == insideSectorPosition &&
			other.sectorX == sectorX &&
			other.sectorY == sectorY &&
			other.sectorZ == sectorZ;
	}

	public override bool Equals(object obj)
	{
		if ((obj is WorldPos) == false) return false;
		return this.Equals((WorldPos)obj);
	}

	public override int GetHashCode()
	{
		return insideSectorPosition.GetHashCode() ^ sectorX.GetHashCode() ^ sectorY.GetHashCode() ^ sectorZ.GetHashCode();
	}


	public WorldPos Towards(WorldPos other)
	{
		return this.Towards(ref other);
	}
	public WorldPos Towards(ref WorldPos other)
	{
		return other.Subtract(this);
	}

	public WorldPos Subtract(WorldPos other)
	{
		return Subtract(ref other);
	}

	public WorldPos Subtract(ref WorldPos other)
	{
		var ret = new WorldPos();
		ret.insideSectorPosition = this.insideSectorPosition - other.insideSectorPosition;
		ret.sectorX = this.sectorX - other.sectorX;
		ret.sectorY = this.sectorY - other.sectorY;
		ret.sectorZ = this.sectorZ - other.sectorZ;
		ret.MoveSectorIfNeeded();
		return ret;
	}

	public WorldPos Add(WorldPos other)
	{
		return Add(ref other);
	}
	public WorldPos Add(ref WorldPos other)
	{
		var ret = new WorldPos();
		ret.insideSectorPosition = this.insideSectorPosition + other.insideSectorPosition;
		ret.sectorX = this.sectorX + other.sectorX;
		ret.sectorY = this.sectorY + other.sectorY;
		ret.sectorZ = this.sectorZ + other.sectorZ;
		ret.MoveSectorIfNeeded();
		return ret;
	}

	public WorldPos MultiplyBy(double v)
	{
		var ret = new WorldPos();
		
		ret.insideSectorPosition = this.ToVector3() * (float)v;
		ret.MoveSectorIfNeeded();
		return ret;


		// (s + i) * v = s*v + i*v;


		ret.insideSectorPosition = this.insideSectorPosition * (float)v;
		ret.MoveSectorIfNeeded();

		double add;

		add = this.sectorX * v;
		ret.sectorX = (long)add;
		ret.insideSectorPosition.x += (float)((add - (long)add) * sectorCubeSideLength);

		add = this.sectorY * v;
		ret.sectorY = (long)add;
		ret.insideSectorPosition.y += (float)((add - (long)add) * sectorCubeSideLength);

		add = this.sectorZ * v;
		ret.sectorZ = (long)add;
		ret.insideSectorPosition.z += (float)((add - (long)add) * sectorCubeSideLength);

		ret.MoveSectorIfNeeded();

		return ret;
	}



	public static WorldPos operator *(WorldPos left, float right)
	{
		return left.MultiplyBy(right);
	}
	public static WorldPos operator /(WorldPos left, float right)
	{
		return left.MultiplyBy(1.0f / right);
	}


	public static WorldPos operator +(WorldPos left, WorldPos right)
	{
		return left.Add(ref right);
	}
	public static WorldPos operator +(WorldPos left, Vector3 right)
	{
		return left + new WorldPos(right);
	}
	public static WorldPos operator +(Vector3 left, WorldPos right)
	{
		return right + left;
	}



	public static WorldPos operator -(WorldPos left, WorldPos right)
	{
		return left.Subtract(ref right);
	}
	public static WorldPos operator -(WorldPos left, Vector3 right)
	{
		return left - new WorldPos(right);
	}


	public static bool operator ==(WorldPos left, WorldPos right)
	{
		return left.Equals(right);
	}
	public static bool operator !=(WorldPos left, WorldPos right)
	{
		return left.Equals(right) == false;
	}


	public static implicit operator Vector3(WorldPos self)
	{
		return self.ToVector3();
	}
	public static implicit operator Vector4(WorldPos self)
	{
		return self.ToVector4();
	}


	public override string ToString()
	{
		var f = "0.000";
		return string.Format("{0};{1};{2}", insideSectorPosition.x.ToString(f), insideSectorPosition.y.ToString(f), insideSectorPosition.z.ToString(f), sectorX, sectorY, sectorZ);
	}


	public static WorldPos Normalize(WorldPos self)
	{
		return self.normalized;
	}

}