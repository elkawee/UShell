

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NotImplementedException = System.NotImplementedException;
using Type = System.Type;


namespace UnityEngineMock1 { 

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

    public struct Vector3
    {
        public const float kEpsilon = 1E-05F;
        public float x;
        public float y;
        public float z;

        /*
            todo one zillion members 
        */
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
    }

    public enum Space
    {
        World = 0,
        Self = 1
    }

    #endregion 


    public class Resources { 
        public GameObject [] roots = new GameObject[0];
        public static T[] FindObjectsOfTypeAll<T>() { return new T[0]; } 
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


    }

    public class Component : Object
    {
        public GameObject         __go ;
        public Component ( GameObject go ) { __go = go; }

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
                __parent = value ;
                __parent.__children.Add( this ) ;
            }
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

    public static partial class AUX { 
        public static IEnumerable<GameObject>  AllChildren ( GameObject go ) {
            if ( go.transform.__children.Count() == 0 ) yield break ;
            foreach ( var ch_tr in go.transform.__children) yield return ch_tr.gameObject ;
            foreach ( var ch_tr in go.transform.__children) foreach ( var cc in AllChildren( ch_tr.gameObject ) ) yield return cc ;
        }
        public static IEnumerable<GameObject> SelfAndAllChildren ( GameObject go ) => ( new [] { go } ).Concat( AllChildren(go));
        
    }
    
    public class GameObject : Object {

        // dict meshes nicely with unity's property of only one Component of each type 
        public Dictionary<System.Type, Component>  __components = new Dictionary<Type, Component>() ;

        public GameObject ( ) {
            __components[typeof(Transform)] = new Transform(this);
        }
    
        public GameObject gameObject => this;
        public Transform  transform  => GetComponent<Transform>();

        public T[] GetComponentsInChildren<T>( bool include_active ) {
            return AUX.SelfAndAllChildren( this).Select ( go => go.GetComponent<T>() ). Where ( comp => comp != null ).ToArray();
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