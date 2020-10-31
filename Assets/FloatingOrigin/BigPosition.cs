using System;
using UnityEngine;

// TODO: try to use BigInteger or BigRational once .Net 4.0 is available
[System.Serializable]
public struct BigPosition : IEquatable<BigPosition>
{
	[SerializeField]
	Vector3 insideSectorPosition;
	[SerializeField]
	long sectorX, sectorY, sectorZ;

	const int sectorCubeSideLength = 100;
	//const double offset = 0.5;


	public static readonly BigPosition Zero = new BigPosition();


	public BigPosition normalized
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

	public float magnitude
	{
		get
		{
			return ToVector3().magnitude;
		}
	}

	public float sqrMagnitude
	{
		get
		{
			return ToVector3().sqrMagnitude;
		}
	}

	public BigPosition(float x, float y, float z)
	{
		insideSectorPosition = new Vector3(x, y, z);
		sectorX = 0;
		sectorY = 0;
		sectorZ = 0;
		MoveSectorIfNeeded();
	}

	public BigPosition(Vector3 pos)
	{
		insideSectorPosition = pos;
		sectorX = 0; sectorY = 0; sectorZ = 0;
		MoveSectorIfNeeded();
	}

	public BigPosition KeepOnlySectorPos()
	{
		return new BigPosition()
		{
			sectorX = this.sectorX,
			sectorY = this.sectorY,
			sectorZ = this.sectorZ,
		};
	}


	private Vector3 Remainder()
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

	public void MoveSectorIfNeeded()
	{
		long sectorAdd;

		sectorAdd = (long)(insideSectorPosition.x / sectorCubeSideLength);
		insideSectorPosition.x -= sectorCubeSideLength * sectorAdd;
		sectorX += sectorAdd;

		sectorAdd = (long)(insideSectorPosition.y / sectorCubeSideLength);
		insideSectorPosition.y -= sectorCubeSideLength * sectorAdd;
		sectorY += sectorAdd;

		sectorAdd = (long)(insideSectorPosition.z / sectorCubeSideLength);
		insideSectorPosition.z -= sectorCubeSideLength * sectorAdd;
		sectorZ += sectorAdd;
	}

	public static double Distance(BigPosition a, BigPosition b)
	{
		return a.Distance(b);
	}
	public double Distance(BigPosition worldPos)
	{
		return this.Towards(ref worldPos).magnitude;
	}
	public double DistanceSqr(BigPosition worldPos)
	{
		return this.Towards(ref worldPos).sqrMagnitude;
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

	public bool Equals(BigPosition other)
	{
		return
			other.insideSectorPosition == insideSectorPosition &&
			other.sectorX == sectorX &&
			other.sectorY == sectorY &&
			other.sectorZ == sectorZ;
	}

	public override bool Equals(object obj)
	{
		if ((obj is BigPosition) == false) return false;
		return this.Equals((BigPosition)obj);
	}

	public override int GetHashCode()
	{
		return insideSectorPosition.GetHashCode() ^ sectorX.GetHashCode() ^ sectorY.GetHashCode() ^ sectorZ.GetHashCode();
	}


	public BigPosition Towards(BigPosition other)
	{
		return this.Towards(ref other);
	}
	public BigPosition Towards(ref BigPosition other)
	{
		return other.Subtract(this);
	}

	public BigPosition Subtract(BigPosition other)
	{
		return Subtract(ref other);
	}

	public BigPosition Subtract(ref BigPosition other)
	{
		var ret = new BigPosition();
		ret.insideSectorPosition = this.insideSectorPosition - other.insideSectorPosition;
		ret.sectorX = this.sectorX - other.sectorX;
		ret.sectorY = this.sectorY - other.sectorY;
		ret.sectorZ = this.sectorZ - other.sectorZ;
		ret.MoveSectorIfNeeded();
		return ret;
	}

	public BigPosition Add(BigPosition other)
	{
		return Add(ref other);
	}
	public BigPosition Add(ref BigPosition other)
	{
		var ret = new BigPosition();
		ret.insideSectorPosition = this.insideSectorPosition + other.insideSectorPosition;
		ret.sectorX = this.sectorX + other.sectorX;
		ret.sectorY = this.sectorY + other.sectorY;
		ret.sectorZ = this.sectorZ + other.sectorZ;
		ret.MoveSectorIfNeeded();
		return ret;
	}

	public BigPosition MultiplyBy(double v)
	{
		var ret = new BigPosition();

		ret.insideSectorPosition = this.ToVector3() * (float)v;
		ret.MoveSectorIfNeeded();
		return ret;


		// (s + i) * v = s*v + i*v;

		/*
		// maybe better way, but seemed to not work

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
		*/
	}



	public static BigPosition operator *(BigPosition left, float right)
	{
		return left.MultiplyBy(right);
	}
	public static BigPosition operator /(BigPosition left, float right)
	{
		return left.MultiplyBy(1.0f / right);
	}


	public static BigPosition operator +(BigPosition left, BigPosition right)
	{
		return left.Add(ref right);
	}
	public static BigPosition operator +(BigPosition left, Vector3 right)
	{
		return left + new BigPosition(right);
	}
	public static BigPosition operator +(Vector3 left, BigPosition right)
	{
		return right + left;
	}



	public static BigPosition operator -(BigPosition left, BigPosition right)
	{
		return left.Subtract(ref right);
	}
	public static BigPosition operator -(BigPosition left, Vector3 right)
	{
		return left - new BigPosition(right);
	}


	public static bool operator ==(BigPosition left, BigPosition right)
	{
		return left.Equals(right);
	}
	public static bool operator !=(BigPosition left, BigPosition right)
	{
		return left.Equals(right) == false;
	}


	public static implicit operator Vector3(BigPosition self)
	{
		return self.ToVector3();
	}
	public static implicit operator Vector4(BigPosition self)
	{
		return self.ToVector4();
	}


	public override string ToString()
	{
		var f = "0.000";
		return string.Format("{0};{1};{2}", insideSectorPosition.x.ToString(f), insideSectorPosition.y.ToString(f), insideSectorPosition.z.ToString(f), sectorX, sectorY, sectorZ);
	}


	public static BigPosition Normalize(BigPosition self)
	{
		return self.normalized;
	}

}