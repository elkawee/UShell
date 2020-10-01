

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NotImplementedException = System.NotImplementedException;
using Type = System.Type;


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

    #endregion 


    public class Resources { 
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
        //public BoneWeight[] boneWeights { get; set; }
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