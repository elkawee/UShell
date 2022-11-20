

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using NotImplementedException = System.NotImplementedException;
using Type = System.Type;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct MathfInternal
{
	public static volatile float FloatMinNormal = 1.17549435E-38f;

	public static volatile float FloatMinDenormal = float.Epsilon;

	public static bool IsFlushToZeroEnabled = FloatMinDenormal == 0f;
}



public struct Mathf
{
	public const float PI = (float)Math.PI;

	public const float Infinity = float.PositiveInfinity;

	public const float NegativeInfinity = float.NegativeInfinity;

	public const float Deg2Rad = (float)Math.PI / 180f;

	public const float Rad2Deg = 57.29578f;

	public static readonly float Epsilon = (!MathfInternal.IsFlushToZeroEnabled) ? MathfInternal.FloatMinDenormal : MathfInternal.FloatMinNormal;

    public static float Min(float a, float b)
	{
		return (!(a < b)) ? b : a;
	}

    public static float Max(float a, float b)
	{
		return (!(a > b)) ? b : a;
	}

    public static float Sqrt(float f)
	{
		return (float)Math.Sqrt(f);
	}

}



namespace UnityEngine.Rendering {
    public struct SphericalHarmonicsL2
    {
        private float shr0;

	    private float shr1;

	    private float shr2;

	    private float shr3;

	    private float shr4;

	    private float shr5;

	    private float shr6;

	    private float shr7;

	    private float shr8;

	    private float shg0;

	    private float shg1;

	    private float shg2;

	    private float shg3;

	    private float shg4;

	    private float shg5;

	    private float shg6;

	    private float shg7;

	    private float shg8;

	    private float shb0;

	    private float shb1;

	    private float shb2;

	    private float shb3;

	    private float shb4;

	    private float shb5;

	    private float shb6;

	    private float shb7;

	    private float shb8;

        public float this[int rgb, int coefficient]
	{
		get
		{
			switch (rgb * 9 + coefficient)
			{
			case 0:
				return shr0;
			case 1:
				return shr1;
			case 2:
				return shr2;
			case 3:
				return shr3;
			case 4:
				return shr4;
			case 5:
				return shr5;
			case 6:
				return shr6;
			case 7:
				return shr7;
			case 8:
				return shr8;
			case 9:
				return shg0;
			case 10:
				return shg1;
			case 11:
				return shg2;
			case 12:
				return shg3;
			case 13:
				return shg4;
			case 14:
				return shg5;
			case 15:
				return shg6;
			case 16:
				return shg7;
			case 17:
				return shg8;
			case 18:
				return shb0;
			case 19:
				return shb1;
			case 20:
				return shb2;
			case 21:
				return shb3;
			case 22:
				return shb4;
			case 23:
				return shb5;
			case 24:
				return shb6;
			case 25:
				return shb7;
			case 26:
				return shb8;
			default:
				throw new IndexOutOfRangeException("Invalid index!");
			}
		}
		set
		{
			switch (rgb * 9 + coefficient)
			{
			case 0:
				shr0 = value;
				break;
			case 1:
				shr1 = value;
				break;
			case 2:
				shr2 = value;
				break;
			case 3:
				shr3 = value;
				break;
			case 4:
				shr4 = value;
				break;
			case 5:
				shr5 = value;
				break;
			case 6:
				shr6 = value;
				break;
			case 7:
				shr7 = value;
				break;
			case 8:
				shr8 = value;
				break;
			case 9:
				shg0 = value;
				break;
			case 10:
				shg1 = value;
				break;
			case 11:
				shg2 = value;
				break;
			case 12:
				shg3 = value;
				break;
			case 13:
				shg4 = value;
				break;
			case 14:
				shg5 = value;
				break;
			case 15:
				shg6 = value;
				break;
			case 16:
				shg7 = value;
				break;
			case 17:
				shg8 = value;
				break;
			case 18:
				shb0 = value;
				break;
			case 19:
				shb1 = value;
				break;
			case 20:
				shb2 = value;
				break;
			case 21:
				shb3 = value;
				break;
			case 22:
				shb4 = value;
				break;
			case 23:
				shb5 = value;
				break;
			case 24:
				shb6 = value;
				break;
			case 25:
				shb7 = value;
				break;
			case 26:
				shb8 = value;
				break;
			default:
				throw new IndexOutOfRangeException("Invalid index!");
			}
		}
	}



        
    }
}

namespace UnityEngine { 

    /* 
    problem mangelnde multi inheritance 

    original : 
        UEObject > Component > Transform 
        UEObject > GameObject 

    ich haette gerne 
        
        UEObject1 > Component1 > Transform1

        wenn jetze  
        class Transform1 : Transform {} 

        erbt sie dann von Component aus Pasta_BaseTypes und kann nicht mehr von 
        Component1 erben 

        interfaces could probably do the stiching pattern 

        I1 > I2 > I3
          \    \    \ 
          C1 >  C2 > C3  

        but i'm not going to invest the time right now 
    */


    
    #region value types 

    public enum PrimitiveType
    {
        Sphere = 0,
        Capsule = 1,
        Cylinder = 2,
        Cube = 3,
        Plane = 4,
        Quad = 5
    }

    public enum SendMessageOptions
    {
        RequireReceiver = 0,
        DontRequireReceiver = 1
    }

    public struct Vector2
    {
        public const float kEpsilon = 1E-05F;
        public float x;
        public float y;

        // pasta from ILSpy
        public float this[int index]
	    {
		    get
		    {
			    switch (index)
			    {
			    case 0:
				    return x;
			    case 1:
				    return y;
			    default:
				    throw new IndexOutOfRangeException("Invalid Vector2 index!");
			    }
		    }
		    set
		    {
			    switch (index)
			    {
			    case 0:
				    x = value;
				    break;
			    case 1:
				    y = value;
				    break;
			    default:
				    throw new IndexOutOfRangeException("Invalid Vector2 index!");
			    }
		    }
	    }
    }

    public struct Vector3
    {
        public const float kEpsilon = 1E-05F;
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
	    {
		    this.x = x;
		    this.y = y;
		    this.z = z;
	    }

	    public Vector3(float x, float y)
	    {
		    this.x = x;
		    this.y = y;
		    z = 0f;
	    }


        public float this[int index]
	    {
		    get
		    {
			    switch (index)
			    {
			    case 0:
				    return x;
			    case 1:
				    return y;
			    case 2:
				    return z;
			    default:
				    throw new IndexOutOfRangeException("Invalid Vector3 index!");
			    }
		    }
		    set
		    {
			    switch (index)
			    {
			    case 0:
				    x = value;
				    break;
			    case 1:
				    y = value;
				    break;
			    case 2:
				    z = value;
				    break;
			    default:
				    throw new IndexOutOfRangeException("Invalid Vector3 index!");
			    }
		    }
	    }

        private static readonly Vector3 zeroVector = new Vector3(0f, 0f, 0f);

	    private static readonly Vector3 oneVector = new Vector3(1f, 1f, 1f);

	    private static readonly Vector3 upVector = new Vector3(0f, 1f, 0f);

	    private static readonly Vector3 downVector = new Vector3(0f, -1f, 0f);

	    private static readonly Vector3 leftVector = new Vector3(-1f, 0f, 0f);

	    private static readonly Vector3 rightVector = new Vector3(1f, 0f, 0f);

	    private static readonly Vector3 forwardVector = new Vector3(0f, 0f, 1f);

	    private static readonly Vector3 backVector = new Vector3(0f, 0f, -1f);

	    private static readonly Vector3 positiveInfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

	    private static readonly Vector3 negativeInfinityVector = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);

	    public float sqrMagnitude => x * x + y * y + z * z;

	    public static Vector3 zero => zeroVector;

	    public static Vector3 one => oneVector;

	    public static Vector3 forward => forwardVector;

	    public static Vector3 back => backVector;

	    public static Vector3 up => upVector;

	    public static Vector3 down => downVector;

	    public static Vector3 left => leftVector;

	    public static Vector3 right => rightVector;

	    public static Vector3 positiveInfinity => positiveInfinityVector;

	    public static Vector3 negativeInfinity => negativeInfinityVector;




        public static Vector3 operator *(Vector3 a, float d)
	    {
		    return new Vector3(a.x * d, a.y * d, a.z * d);
	    }
        public static Vector3 operator -(Vector3 a, Vector3 b)
	    {
		    return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
	    }
        public static Vector3 operator -(Vector3 a)
	    {
		    return new Vector3(0f - a.x, 0f - a.y, 0f - a.z);
	    }
        public static Vector3 operator +(Vector3 a, Vector3 b)
	    {
		    return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
	    }

        public static Vector3 operator /(Vector3 a, float d)
	    {
		    return new Vector3(a.x / d, a.y / d, a.z / d);
	    }
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
	    {
		    return SqrMagnitude(lhs - rhs) < 9.99999944E-11f;
	    }
        public static bool operator !=(Vector3 lhs, Vector3 rhs)
	    {
		    return !(lhs == rhs);
	    }

        public static Vector3 Min(Vector3 lhs, Vector3 rhs)
	    {
		    return new Vector3(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));
	    }

	    public static Vector3 Max(Vector3 lhs, Vector3 rhs)
	    {
		    return new Vector3(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));
	    }



        public Vector3 normalized => Normalize(this);

        public static float Magnitude(Vector3 vector)
	    {
		    return Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
	    }
        public static float SqrMagnitude(Vector3 vector)
	    {
		    return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
	    }


        public static Vector3 Normalize(Vector3 value)
	    {
		    float num = Magnitude(value);
		    if (num > 1E-05f)
		    {
			    return value / num;
		    }
		    return zero;
	    }

	    public void Normalize()
	    {
		    float num = Magnitude(this);
		    if (num > 1E-05f)
		    {
			    this /= num;
		    }
		    else
		    {
			    this = zero;
		    }
	    }

        /*
            todo one zillion members 
        */
    }

    public struct Vector4
    {
        public const float kEpsilon = 1E-05F;
        public float x;
        public float y;
        public float z;
        public float w;

        private static readonly Vector4 zeroVector = new Vector4(0f, 0f, 0f, 0f);

	    private static readonly Vector4 oneVector = new Vector4(1f, 1f, 1f, 1f);

	    private static readonly Vector4 positiveInfinityVector = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

	    private static readonly Vector4 negativeInfinityVector = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public Vector4(float x, float y, float z, float w)
	    {
		    this.x = x;
		    this.y = y;
		    this.z = z;
		    this.w = w;
	    }

	    public Vector4(float x, float y, float z)
	    {
		    this.x = x;
		    this.y = y;
		    this.z = z;
		    w = 0f;
	    }

	    public Vector4(float x, float y)
	    {
		    this.x = x;
		    this.y = y;
		    z = 0f;
		    w = 0f;
	    }


        public float this[int index]
	    {
		    get
		    {
			    switch (index)
			    {
			    case 0:
				    return x;
			    case 1:
				    return y;
			    case 2:
				    return z;
			    case 3:
				    return w;
			    default:
				    throw new IndexOutOfRangeException("Invalid Vector4 index!");
			    }
		    }
		    set
		    {
			    switch (index)
			    {
			    case 0:
				    x = value;
				    break;
			    case 1:
				    y = value;
				    break;
			    case 2:
				    z = value;
				    break;
			    case 3:
				    w = value;
				    break;
			    default:
				    throw new IndexOutOfRangeException("Invalid Vector4 index!");
			    }
		    }
	    }

        public static bool operator ==(Vector4 lhs, Vector4 rhs)
	    {
		    return SqrMagnitude(lhs - rhs) < 9.99999944E-11f;
	    }

	    public static bool operator !=(Vector4 lhs, Vector4 rhs)
	    {
		    return !(lhs == rhs);
	    }

        public static float Dot(Vector4 a, Vector4 b)
	    {
		    return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
	    }

        public static Vector4 Project(Vector4 a, Vector4 b)
	    {
		    return b * Dot(a, b) / Dot(b, b);
	    }

	    public static float Distance(Vector4 a, Vector4 b)
	    {
		    return Magnitude(a - b);
	    }

	    public static float Magnitude(Vector4 a)
	    {
		    return Mathf.Sqrt(Dot(a, a));
	    }

	    public static Vector4 Min(Vector4 lhs, Vector4 rhs)
	    {
		    return new Vector4(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z), Mathf.Min(lhs.w, rhs.w));
	    }

	    public static Vector4 Max(Vector4 lhs, Vector4 rhs)
	    {
		    return new Vector4(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z), Mathf.Max(lhs.w, rhs.w));
	    }

	    public static Vector4 operator +(Vector4 a, Vector4 b)
	    {
		    return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
	    }

	    public static Vector4 operator -(Vector4 a, Vector4 b)
	    {
		    return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
	    }

	    public static Vector4 operator -(Vector4 a)
	    {
		    return new Vector4(0f - a.x, 0f - a.y, 0f - a.z, 0f - a.w);
	    }

	    public static Vector4 operator *(Vector4 a, float d)
	    {
		    return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
	    }

	    public static Vector4 operator *(float d, Vector4 a)
	    {
		    return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
	    }

	    public static Vector4 operator /(Vector4 a, float d)
	    {
		    return new Vector4(a.x / d, a.y / d, a.z / d, a.w / d);
	    }



        public static float SqrMagnitude(Vector4 a)
	    {
		    return Dot(a, a);
	    }
        public float SqrMagnitude()
	    {
		    return Dot(this, this);
	    }

    }

    public struct Ray
    {
	    private Vector3 m_Origin;

	    private Vector3 m_Direction;

	    public Vector3 origin
	    {
		    get
		    {
			    return m_Origin;
		    }
		    set
		    {
			    m_Origin = value;
		    }
	    }

	    public Vector3 direction
	    {
		    get
		    {
			    return m_Direction;
		    }
		    set
		    {
			    m_Direction = value.normalized;
		    }
	    }

	    public Ray(Vector3 origin, Vector3 direction)
	    {
		    m_Origin = origin;
		    m_Direction = direction.normalized;
	    }

	    public Vector3 GetPoint(float distance)
	    {
		    return m_Origin + m_Direction * distance;
	    }

	    public override string ToString()
	    {
		    return "todo" ; // UnityString.Format("Origin: {0}, Dir: {1}", m_Origin, m_Direction);
	    }

	    public string ToString(string format)
	    {
		    return "todo" ; // UnityString.Format("Origin: {0}, Dir: {1}", m_Origin.ToString(format), m_Direction.ToString(format));
	    }
    }


    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;


        public float this[int index]
	{
		get
		{
			switch (index)
			{
			case 0:
				return r;
			case 1:
				return g;
			case 2:
				return b;
			case 3:
				return a;
			default:
				throw new IndexOutOfRangeException("Invalid Vector3 index!");  // <- yes that's the message, as seen in ILSpy
			}
		}
		set
		{
			switch (index)
			{
			case 0:
				r = value;
				break;
			case 1:
				g = value;
				break;
			case 2:
				b = value;
				break;
			case 3:
				a = value;
				break;
			default:
				throw new IndexOutOfRangeException("Invalid Vector3 index!");
			}
		}
	}
    }

    public struct Quaternion
    {
        public const float kEpsilon = 1E-06F;
        public float x;
        public float y;
        public float z;
        public float w;
        /*
            todo one zillion members 
        */

        public float this[int index]
	{
		get
		{
			switch (index)
			{
			case 0:
				return x;
			case 1:
				return y;
			case 2:
				return z;
			case 3:
				return w;
			default:
				throw new IndexOutOfRangeException("Invalid Quaternion index!");
			}
		}
		set
		{
			switch (index)
			{
			case 0:
				x = value;
				break;
			case 1:
				y = value;
				break;
			case 2:
				z = value;
				break;
			case 3:
				w = value;
				break;
			default:
				throw new IndexOutOfRangeException("Invalid Quaternion index!");
			}
		}
	}
    }

    public struct Matrix4x4
    {

        public static Matrix4x4 identity => identityMatrix;

        public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3)
	{
		m00 = column0.x;
		m01 = column1.x;
		m02 = column2.x;
		m03 = column3.x;
		m10 = column0.y;
		m11 = column1.y;
		m12 = column2.y;
		m13 = column3.y;
		m20 = column0.z;
		m21 = column1.z;
		m22 = column2.z;
		m23 = column3.z;
		m30 = column0.w;
		m31 = column1.w;
		m32 = column2.w;
		m33 = column3.w;
	}

        private static readonly Matrix4x4 identityMatrix = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f));

        public float m00;
        public float m33;
        public float m23;
        public float m13;
        public float m03;
        public float m32;
        public float m22;
        public float m02;
        public float m12;
        public float m21;
        public float m11;
        public float m01;
        public float m30;
        public float m20;
        public float m10;
        public float m31;


        public float this[int row, int column]
	    {
		    get
		    {
			    return this[row + column * 4];
		    }
		    set
		    {
			    this[row + column * 4] = value;
		    }
	    }

	    public float this[int index]
	    {
		    get
		    {
			    switch (index)
			    {
			    case 0:
				    return m00;
			    case 1:
				    return m10;
			    case 2:
				    return m20;
			    case 3:
				    return m30;
			    case 4:
				    return m01;
			    case 5:
				    return m11;
			    case 6:
				    return m21;
			    case 7:
				    return m31;
			    case 8:
				    return m02;
			    case 9:
				    return m12;
			    case 10:
				    return m22;
			    case 11:
				    return m32;
			    case 12:
				    return m03;
			    case 13:
				    return m13;
			    case 14:
				    return m23;
			    case 15:
				    return m33;
			    default:
				    throw new IndexOutOfRangeException("Invalid matrix index!");
			    }
		    }
		    set
		    {
			    switch (index)
			    {
			    case 0:
				    m00 = value;
				    break;
			    case 1:
				    m10 = value;
				    break;
			    case 2:
				    m20 = value;
				    break;
			    case 3:
				    m30 = value;
				    break;
			    case 4:
				    m01 = value;
				    break;
			    case 5:
				    m11 = value;
				    break;
			    case 6:
				    m21 = value;
				    break;
			    case 7:
				    m31 = value;
				    break;
			    case 8:
				    m02 = value;
				    break;
			    case 9:
				    m12 = value;
				    break;
			    case 10:
				    m22 = value;
				    break;
			    case 11:
				    m32 = value;
				    break;
			    case 12:
				    m03 = value;
				    break;
			    case 13:
				    m13 = value;
				    break;
			    case 14:
				    m23 = value;
				    break;
			    case 15:
				    m33 = value;
				    break;
			    default:
				    throw new IndexOutOfRangeException("Invalid matrix index!");
			    }
		    }
	    }

    }

    public enum Space
    {
        World = 0,
        Self = 1
    }

    [System.Flags]
    public enum HideFlags
    {
        None = 0,
        HideInHierarchy = 1,
        HideInInspector = 2,
        DontSaveInEditor = 4,
        NotEditable = 8,
        DontSaveInBuild = 16,
        DontUnloadUnusedAsset = 32,
        DontSave = 52,
        HideAndDontSave = 61
    }

    

    //[UsedByNativeCode]
    public struct BoneWeight
    {
	    private float m_Weight0;

	    private float m_Weight1;

	    private float m_Weight2;

	    private float m_Weight3;

	    private int m_BoneIndex0;

	    private int m_BoneIndex1;

	    private int m_BoneIndex2;

	    private int m_BoneIndex3;

	    public float weight0
	    {
		    get
		    {
			    return m_Weight0;
		    }
		    set
		    {
			    m_Weight0 = value;
		    }
	    }

	    public float weight1
	    {
		    get
		    {
			    return m_Weight1;
		    }
		    set
		    {
			    m_Weight1 = value;
		    }
	    }

	    public float weight2
	    {
		    get
		    {
			    return m_Weight2;
		    }
		    set
		    {
			    m_Weight2 = value;
		    }
	    }

	    public float weight3
	    {
		    get
		    {
			    return m_Weight3;
		    }
		    set
		    {
			    m_Weight3 = value;
		    }
	    }

	    public int boneIndex0
	    {
		    get
		    {
			    return m_BoneIndex0;
		    }
		    set
		    {
			    m_BoneIndex0 = value;
		    }
	    }

	    public int boneIndex1
	    {
		    get
		    {
			    return m_BoneIndex1;
		    }
		    set
		    {
			    m_BoneIndex1 = value;
		    }
	    }

	    public int boneIndex2
	    {
		    get
		    {
			    return m_BoneIndex2;
		    }
		    set
		    {
			    m_BoneIndex2 = value;
		    }
	    }

	    public int boneIndex3
	    {
		    get
		    {
			    return m_BoneIndex3;
		    }
		    set
		    {
			    m_BoneIndex3 = value;
		    }
	    }

	    public override int GetHashCode()
	    {
		    return boneIndex0.GetHashCode() ^ (boneIndex1.GetHashCode() << 2) ^ (boneIndex2.GetHashCode() >> 2) ^ (boneIndex3.GetHashCode() >> 1) ^ (weight0.GetHashCode() << 5) ^ (weight1.GetHashCode() << 4) ^ (weight2.GetHashCode() >> 4) ^ (weight3.GetHashCode() >> 3);
	    }

	    public override bool Equals(object other)
	    {
		    if (!(other is BoneWeight))
		    {
			    return false;
		    }
		    BoneWeight boneWeight = (BoneWeight)other;
		    return boneIndex0.Equals(boneWeight.boneIndex0) && boneIndex1.Equals(boneWeight.boneIndex1) && boneIndex2.Equals(boneWeight.boneIndex2) && boneIndex3.Equals(boneWeight.boneIndex3) && new Vector4(weight0, weight1, weight2, weight3).Equals(new Vector4(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3));
	    }

	    public static bool operator ==(BoneWeight lhs, BoneWeight rhs)
	    {
		    return lhs.boneIndex0 == rhs.boneIndex0 && lhs.boneIndex1 == rhs.boneIndex1 && lhs.boneIndex2 == rhs.boneIndex2 && lhs.boneIndex3 == rhs.boneIndex3 && new Vector4(lhs.weight0, lhs.weight1, lhs.weight2, lhs.weight3) == new Vector4(rhs.weight0, rhs.weight1, rhs.weight2, rhs.weight3);
	    }

	    public static bool operator !=(BoneWeight lhs, BoneWeight rhs)
	    {
		    return !(lhs == rhs);
	    }
    }


    #endregion 


    public class Resources { 
        /*
            this does not distinguish between in-scene objects and assets 
        */
        public static GameObject []  roots                     = new GameObject[0];
        public static T[]            FindObjectsOfTypeAll<T>() { 
            if ( typeof(T).IsSubclassOf(typeof ( Component ) ) ) { 
                 return roots
                        .SelectMany( root_obj => AUX.SelfAndAllDescendants( root_obj ))
                        .Select( obj => obj.GetComponent<T>()) 
                        .Where ( obj => obj != null )
                        .ToArray();
            } else if ( typeof(T) == typeof(GameObject) )  { 
                return roots
                        .SelectMany( root_obj => AUX.SelfAndAllDescendants( root_obj ))
                        .Select( obj => (T) (System.Object)obj )
                        .ToArray();
            } else {
                throw new NotImplementedException();
            }
                 
        } 
        public static /*extern*/ Object[] FindObjectsOfTypeAll(Type type){
            var MI = typeof(Resources).GetMethod("FindObjectsOfTypeAll", new Type[0] ).MakeGenericMethod( new [] { type } );
            return ((IEnumerable)MI.Invoke( null , new object[0] )).Cast<Object>().ToArray();
        }
    }


    public class Object {
        // from ILSpy 
        private static bool CompareBaseObjects(Object lhs, Object rhs)
        {
	        bool flag = (object)lhs == null;
	        bool flag2 = (object)rhs == null;
	        if (flag2 && flag)
	        {
		        return true;
	        }
            /*
	        if (flag2)
	        {
		        return !IsNativeObjectAlive(lhs);
	        }
	        if (flag)
	        {
		        return !IsNativeObjectAlive(rhs);
	        }
            */
	        return object.ReferenceEquals(lhs, rhs);
        }
        public override int GetHashCode()
        {
	        return base.GetHashCode();
        }


        public override bool Equals(object o)
        {
	        return CompareBaseObjects(this, o as Object);
        }
        public static bool operator ==(Object x, Object y)
        {
	        return CompareBaseObjects(x, y);
        }
        public static bool operator !=(Object x, Object y)
        {
	        return !CompareBaseObjects(x, y);
        }

        //  -------- 

        
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }

        //  =====

        public static T FindObjectOfType<T>() where T : Object
        {
	        return (T)FindObjectOfType(typeof(T));
        }

        public static Object FindObjectOfType(Type type)
        {
	        Object[] array = FindObjectsOfType(type);
	        if (array.Length > 0)
	        {
		        return array[0];
	        }
	        return null;
        }

        #region quickhack

        public static Object[] FindObjectsOfType(Type type){
            // the mock scenegraph currently makes no distinction between in-scene objects and resources 
            var MI = typeof(Resources).GetMethod("FindObjectsOfTypeAll").MakeGenericMethod(new []{ type } );
            
            IEnumerable ienum = (IEnumerable) MI.Invoke( null , new [] { type } );
            return ienum.Cast<Object>().ToArray();      // todo : i'm not too sure if the original can only return `UnityEngine.Object`s 
        }

        public static T[] FindObjectsOfType<T>() where T : Object
        // orig : 
        // {    return Resources.ConvertObjects<T>(FindObjectsOfType(typeof(T))); }
        {
            return Resources.FindObjectsOfTypeAll<T>();
        }

        [Obsolete("Please use Resources.FindObjectsOfTypeAll instead")]
        public static Object[] FindObjectsOfTypeAll(Type type)
        // orig : 
        // {	        return Resources.FindObjectsOfTypeAll(type);        }
        { 
            return FindObjectsOfType(type);
        }

        public static /*extern*/ Object[] FindSceneObjectsOfType(Type type) => FindObjectsOfType(type);

        #endregion 

    }

    public class Component : Object
    {
        public GameObject         __go ;
        public Component ( GameObject go ) { __go = go; }  // <- this is where signatures differ from the original UnityEngine, using this directly fucks up bidirectional links between GO<->Component, use with caution 

        public GameObject gameObject => __go ;
        public T GetComponent<T>()   => __go.GetComponent<T>();
        public Transform transform => __go.GetComponent<Transform>();

        public T[] GetComponentsInChildren<T>( bool include_active = false) => __go.GetComponentsInChildren<T>(include_active);

       

    }



    public class Transform : Component , IEnumerable { 
        
        public List<Transform>   __children = new List<Transform>();

        public Transform __parent = null ;
        public Transform parent {
        get => __parent;
            set { 
            if ( __parent != null ) { 
                __parent.__children.Remove( this );
            }
            __parent = value ;
            __parent.__children.Add( this ) ;
        }} 

        public Transform ( GameObject go ) :base(go) {}

        // original :  [WrapperlessIcall] public extern Transform GetChild(int index);
        // meaning  :  exceptions n stuff will not be the same 
        public Transform GetChild(int index) => __children.ElementAt<Transform>( index ); 

        public int childCount => __children.Count;

        public IEnumerator GetEnumerator()
        {
            return new Enumerator( this ) ;
        }

        public Vector3 position
	    {
		    get
		    {
			    INTERNAL_get_position(out Vector3 value);
			    return value;
		    }
		    set
		    {
			    INTERNAL_set_position(ref value);
		    }
	    }

        private /* extern */ void INTERNAL_get_position(out Vector3 value)
        {
            value = new Vector3();
            // guesswork 
            value.x = localToWorldMatrix.m03;
            value.y = localToWorldMatrix.m13;
            value.y = localToWorldMatrix.m23;
        }

	
	    private /*extern*/ void INTERNAL_set_position(ref Vector3 value)
        {
             // guesswork 
             var M = _localToWorldMatrix ;
            M.m03 = value.x ; 
            M.m13 = value.y ; 
            M.m23 = value.z ;
            _localToWorldMatrix = M; 

        }


        /*
            quickhack 
            implementing this properly almost certainly needs a different representation 
        */


        Matrix4x4 _localToWorldMatrix = Matrix4x4.identity;

        public Matrix4x4 localToWorldMatrix { get { return _localToWorldMatrix; } }
        public Matrix4x4 worldToLocalMatrix { get; }
        


        // copied from the original ( ILSpy ) 
        private sealed class Enumerator : IEnumerator
        {
	        private Transform outer;

	        private int currentIndex = -1;

	        public object Current => outer.GetChild(currentIndex);

	        internal Enumerator(Transform outer)
	        {
		        this.outer = outer;
	        }

	        public bool MoveNext()
	        {
		        int childCount = outer.childCount;
		        return ++currentIndex < childCount;
	        }

	        public void Reset()
	        {
		        currentIndex = -1;
	        }
        }

    }

    public class Material : Object
    {
    }

    public class Renderer : Component
    {
        public Renderer ( GameObject go ) : base ( go ) {}     

        public Material[] sharedMaterials { get; set; }
        public Material[] materials { get; set; }
        public Material sharedMaterial { get; set; }
        public Material material { get; set; }
        
    }

    public sealed class MeshRenderer : Renderer
    {
        public MeshRenderer( GameObject go ) : base(go) {}

        public Mesh additionalVertexStreams { get; set; }
    }


    public sealed class Mesh : Object
    {
        //public Mesh();

         public int blendShapeCount { get; }
        public int vertexBufferCount { get; }
        //public Bounds bounds { get; set; }
        public int vertexCount { get; }
        public int subMeshCount { get; set; }
        public BoneWeight[] boneWeights { get; set; }
        public Matrix4x4[] bindposes { get; set; }
        public bool isReadable { get; }
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //[Obsolete("Property Mesh.uv1 has been deprecated. Use Mesh.uv2 instead (UnityUpgradable) -> uv2", true)]
        public Vector2[] uv1 { get; set; }
        public Vector3[] normals { get; set; }
        public Vector4[] tangents { get; set; }
        public Vector2[] uv { get; set; }
        public Vector2[] uv2 { get; set; }
        public Vector2[] uv3 { get; set; }
        public Vector2[] uv4 { get; set; }
        public Color[] colors { get; set; }
        public Vector3[] vertices { get; set; }
        //public Color32[] colors32 { get; set; }
        public int[] triangles { get; set; }

    }


    //[RequireComponent(typeof(Transform))]
    public sealed class MeshFilter : Component
    {
        public MeshFilter ( GameObject go ) : base(go) {} 
	    public Mesh mesh
	    {
		    //[MethodImpl(MethodImplOptions.InternalCall)]
		    //[GeneratedByOldBindingsGenerator]
		    get;
		    //[MethodImpl(MethodImplOptions.InternalCall)]
		    //[GeneratedByOldBindingsGenerator]
		    set;
	    }

	    public Mesh sharedMesh
	    {
		    //[MethodImpl(MethodImplOptions.InternalCall)]
		    //[GeneratedByOldBindingsGenerator]
		    get;
		    //[MethodImpl(MethodImplOptions.InternalCall)]
		    //[GeneratedByOldBindingsGenerator]
		    set;
	    }
    }

    

    public class Behaviour : Component
    {
        public Behaviour(GameObject go ):base(go){}

        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get; }
    }




    public sealed class Camera : Behaviour
    {
        public Camera(GameObject go ) : base(go) {} 

        public Matrix4x4 nonJitteredProjectionMatrix { get; set; }
      
        public Matrix4x4 projectionMatrix { get; set; }
       
        public Matrix4x4 worldToCameraMatrix { get; set; }
       
        public Matrix4x4 cameraToWorldMatrix { get; }
    }



    public static partial class AUX { 
        public static IEnumerable<GameObject>  AllDescendants ( GameObject go ) {
            if ( go.transform.__children.Count() == 0 ) yield break ;
            foreach ( var ch_tr in go.transform.__children) yield return ch_tr.gameObject ;
            foreach ( var ch_tr in go.transform.__children) foreach ( var cc in AllDescendants( ch_tr.gameObject ) ) yield return cc ;
        }
        public static IEnumerable<GameObject> SelfAndAllDescendants ( GameObject go ) => ( new [] { go } ).Concat( AllDescendants(go));
        
    }
    
    public class GameObject : Object {

        // dict meshes nicely with unity's property of only one Component of each type 
        public Dictionary<System.Type, Component>  __components = new Dictionary<Type, Component>() ;

        public Component AddComponent(Type componentType)
        {
            if ( !  typeof ( Component ).IsAssignableFrom( componentType))    throw new Exception("ding 1 " );

            Component compInstance = (Component) Activator.CreateInstance( componentType , new System.Object [] { this } ) ;  // <- every Mock-Component needs to have a " Comp( GameObject go ) " constructor implemented 
            __components[componentType] = compInstance ; // <- todo lazyness , this is probably not how the real API behaves ( i guess it silently returns leaving the original comp instance untouched instead of overriding it ) 
            return compInstance;
        }

        public T AddComponent<T>() where T : Component
        {
	        return AddComponent(typeof(T)) as T;
        }

        public Component AddComponent(string className)
        {
	        throw new NotSupportedException("AddComponent(string) is deprecated");
        }


        public GameObject ( ) {
            __components[typeof(Transform)] = new Transform(this);
        }
    
        public GameObject gameObject => this;
        public Transform  transform  => GetComponent<Transform>();

        public T[] GetComponentsInChildren<T>( bool include_active ) {
            return AUX.SelfAndAllDescendants( this).Select ( go => go.GetComponent<T>() ). Where ( comp => comp != null ).ToArray();
        }

        public  Component GetComponent(Type type)
        {
            if ( __components.ContainsKey( type ) ) return __components[type];
            else return null ;
        }
        public  T GetComponent<T>  () 
        {
            return (T) (System.Object) GetComponent( typeof( T)  ) ;
        }
       

    }
    
}