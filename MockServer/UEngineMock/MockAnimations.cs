using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using NotImplementedException = System.NotImplementedException;
using Type = System.Type;



using UnityEngine ;


namespace UnityEngine { 


    //[RequiredByNativeCode]
    public struct Keyframe
    {
	    private float m_Time;
	    private float m_Value;
	    private float m_InTangent;
	    private float m_OutTangent;
	    private int   m_TangentMode;

	    public float time
	    {
		    get
		    {
			    return m_Time;
		    }
		    set
		    {
			    m_Time = value;
		    }
	    }

	    public float value
	    {
		    get
		    {
			    return m_Value;
		    }
		    set
		    {
			    m_Value = value;
		    }
	    }

	    public float inTangent
	    {
		    get
		    {
			    return m_InTangent;
		    }
		    set
		    {
			    m_InTangent = value;
		    }
	    }

	    public float outTangent
	    {
		    get
		    {
			    return m_OutTangent;
		    }
		    set
		    {
			    m_OutTangent = value;
		    }
	    }

	    public int tangentMode
	    {
		    get
		    {
			    return m_TangentMode;
		    }
		    set
		    {
			    m_TangentMode = value;
		    }
	    }

	    public Keyframe(float time, float value)
	    {
		    m_Time = time;
		    m_Value = value;
		    m_InTangent = 0f;
		    m_OutTangent = 0f;
		    m_TangentMode = 0;
	    }

	    public Keyframe(float time, float value, float inTangent, float outTangent)
	    {
		    m_Time = time;
		    m_Value = value;
		    m_InTangent = inTangent;
		    m_OutTangent = outTangent;
		    m_TangentMode = 0;
	    }
    }


    // UnityEngine.AnimationCurve


    public enum WrapMode
    {
	    Once = 1,
	    Loop = 2,
	    PingPong = 4,
	    Default = 0,
	    ClampForever = 8,
	    Clamp = 1
    }

    public enum AnimationBlendMode
    {
	    Blend,
	    Additive
    }

    internal enum AnimationEventSource
    {
	    NoSource,
	    Legacy,
	    Animator
    }

    public struct AnimatorClipInfo
    {
	    private int m_ClipInstanceID;

	    private float m_Weight;

	    public AnimationClip clip => (m_ClipInstanceID == 0) ? null : ClipInstanceToScriptingObject(m_ClipInstanceID);

	    public float weight => m_Weight;

	    //[MethodImpl(MethodImplOptions.InternalCall)]
	    //[GeneratedByOldBindingsGenerator]
	    private static /* extern */ AnimationClip ClipInstanceToScriptingObject(int instanceID) { throw new NotImplementedException(); } 
    }



    public struct AnimatorStateInfo
    {
	    private int m_Name;

	    private int m_Path;

	    private int m_FullPath;

	    private float m_NormalizedTime;

	    private float m_Length;

	    private float m_Speed;

	    private float m_SpeedMultiplier;

	    private int m_Tag;

	    private int m_Loop;

	    public int fullPathHash => m_FullPath;

	    [Obsolete("Use AnimatorStateInfo.fullPathHash instead.")]
	    public int nameHash
	    {
		    get
		    {
			    return m_Path;
		    }
	    }

	    public int shortNameHash => m_Name;

	    public float normalizedTime => m_NormalizedTime;

	    public float length => m_Length;

	    public float speed => m_Speed;

	    public float speedMultiplier => m_SpeedMultiplier;

	    public int tagHash => m_Tag;

	    public bool loop => m_Loop != 0;

	    public bool IsName(string name)
	    {
		    int num = Animator.StringToHash(name);
		    return num == m_FullPath || num == m_Name || num == m_Path;
	    }

	    public bool IsTag(string tag)
	    {
		    return Animator.StringToHash(tag) == m_Tag;
	    }
    }

    public struct Bounds
    {
	    private Vector3 m_Center;

	    private Vector3 m_Extents;

	    public Vector3 center
	    {
		    get
		    {
			    return m_Center;
		    }
		    set
		    {
			    m_Center = value;
		    }
	    }

	    public Vector3 size
	    {
		    get
		    {
			    return m_Extents * 2f;
		    }
		    set
		    {
			    m_Extents = value * 0.5f;
		    }
	    }

	    public Vector3 extents
	    {
		    get
		    {
			    return m_Extents;
		    }
		    set
		    {
			    m_Extents = value;
		    }
	    }

	    public Vector3 min
	    {
		    get
		    {
			    return center - extents;
		    }
		    set
		    {
			    SetMinMax(value, max);
		    }
	    }

	    public Vector3 max
	    {
		    get
		    {
			    return center + extents;
		    }
		    set
		    {
			    SetMinMax(min, value);
		    }
	    }

	    public Bounds(Vector3 center, Vector3 size)
	    {
		    m_Center = center;
		    m_Extents = size * 0.5f;
	    }

	    //[ThreadAndSerializationSafe]
	    private static bool Internal_Contains(Bounds m, Vector3 point)
	    {
            throw new NotImplementedException();
		    //return INTERNAL_CALL_Internal_Contains(ref m, ref point);
	    }

        /*
	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private static extern bool INTERNAL_CALL_Internal_Contains(ref Bounds m, ref Vector3 point);
        */

	    public bool Contains(Vector3 point)
	    {
		    return Internal_Contains(this, point);
	    }

	    private static float Internal_SqrDistance(Bounds m, Vector3 point)
	    {
            throw new NotImplementedException();
		    //return INTERNAL_CALL_Internal_SqrDistance(ref m, ref point);
	    }

        /*
	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private static extern float INTERNAL_CALL_Internal_SqrDistance(ref Bounds m, ref Vector3 point);
        */

	    public float SqrDistance(Vector3 point)
	    {
            throw new NotImplementedException();
		    //return Internal_SqrDistance(this, point);
	    }

        /*
	    private static bool Internal_IntersectRay(ref Ray ray, ref Bounds bounds, out float distance)
	    {
		    return INTERNAL_CALL_Internal_IntersectRay(ref ray, ref bounds, out distance);
	    }

	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private static extern bool INTERNAL_CALL_Internal_IntersectRay(ref Ray ray, ref Bounds bounds, out float distance);
        */
	    public bool IntersectRay(Ray ray)
	    {
            throw new NotImplementedException();
            /*
		    float distance;
		    return Internal_IntersectRay(ref ray, ref this, out distance);
            */
	    }

	    public bool IntersectRay(Ray ray, out float distance)
	    {
            throw new NotImplementedException();
            
		    //return Internal_IntersectRay(ref ray, ref this, out distance);
	    }

	    private static Vector3 Internal_GetClosestPoint(ref Bounds bounds, ref Vector3 point)
	    {
            throw new NotImplementedException();
            /*
		    INTERNAL_CALL_Internal_GetClosestPoint(ref bounds, ref point, out Vector3 value);
		    return value;
            */
	    }

        /*
	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private static extern void INTERNAL_CALL_Internal_GetClosestPoint(ref Bounds bounds, ref Vector3 point, out Vector3 value);
        */

	    public Vector3 ClosestPoint(Vector3 point)
	    {
		    return Internal_GetClosestPoint(ref this, ref point);
	    }

	    public override int GetHashCode()
	    {
		    return center.GetHashCode() ^ (extents.GetHashCode() << 2);
	    }

	    public override bool Equals(object other)
	    {
		    if (!(other is Bounds))
		    {
			    return false;
		    }
		    Bounds bounds = (Bounds)other;
		    return center.Equals(bounds.center) && extents.Equals(bounds.extents);
	    }

	    public static bool operator ==(Bounds lhs, Bounds rhs)
	    {
		    return lhs.center == rhs.center && lhs.extents == rhs.extents;
	    }

	    public static bool operator !=(Bounds lhs, Bounds rhs)
	    {
		    return !(lhs == rhs);
	    }

	    public void SetMinMax(Vector3 min, Vector3 max)
	    {
		    extents = (max - min) * 0.5f;
		    center = min + extents;
	    }

	    public void Encapsulate(Vector3 point)
	    {
		    SetMinMax(Vector3.Min(min, point), Vector3.Max(max, point));
	    }

	    public void Encapsulate(Bounds bounds)
	    {
		    Encapsulate(bounds.center - bounds.extents);
		    Encapsulate(bounds.center + bounds.extents);
	    }

	    public void Expand(float amount)
	    {
		    amount *= 0.5f;
		    extents += new Vector3(amount, amount, amount);
	    }

	    public void Expand(Vector3 amount)
	    {
		    extents += amount * 0.5f;
	    }

	    public bool Intersects(Bounds bounds)
	    {
		    Vector3 min = this.min;
		    float x = min.x;
		    Vector3 max = bounds.max;
		    int result;
		    if (x <= max.x)
		    {
			    Vector3 max2 = this.max;
			    float x2 = max2.x;
			    Vector3 min2 = bounds.min;
			    if (x2 >= min2.x)
			    {
				    Vector3 min3 = this.min;
				    float y = min3.y;
				    Vector3 max3 = bounds.max;
				    if (y <= max3.y)
				    {
					    Vector3 max4 = this.max;
					    float y2 = max4.y;
					    Vector3 min4 = bounds.min;
					    if (y2 >= min4.y)
					    {
						    Vector3 min5 = this.min;
						    float z = min5.z;
						    Vector3 max5 = bounds.max;
						    if (z <= max5.z)
						    {
							    Vector3 max6 = this.max;
							    float z2 = max6.z;
							    Vector3 min6 = bounds.min;
							    result = ((z2 >= min6.z) ? 1 : 0);
							    goto IL_00d8;
						    }
					    }
				    }
			    }
		    }
		    result = 0;
		    goto IL_00d8;
		    IL_00d8:
		    return (byte)result != 0;
	    }

	    public override string ToString()
	    {
		    return "todo" ; // UnityString.Format("Center: {0}, Extents: {1}", m_Center, m_Extents);
	    }

	    public string ToString(string format)
	    {
		    return "todo" ; // UnityString.Format("Center: {0}, Extents: {1}", m_Center.ToString(format), m_Extents.ToString(format));
	    }
    }



    [StructLayout(LayoutKind.Sequential)]
    public sealed class AnimationCurve
    {
	    internal IntPtr m_Ptr;   // artefact -- dunno what it does in the original 

        // hmm the original impl has the default constructor set this to null 
        // in order to get as close to the orig behaviour as possible, it seems sensible to mimic 
        // otoh, as per usual, everything important is hidden behind `external` functions 
        public Keyframe [] __keys  = new Keyframe[0];   

	    public Keyframe[] keys
	    {
		    get
		    {
			    return GetKeys();
		    }
		    set
		    {
			    SetKeys(value);
		    }
	    }

	    public Keyframe this[int index]
	    {
		    get
		    {
			    return GetKey_Internal(index);
		    }
	    }

        // https://docs.unity3d.com/ScriptReference/AnimationCurve-length.html
        // # of keyframes 
	    public int length => __keys.Length ; 
	    
	    public WrapMode preWrapMode
	    {
		    get;
		    set;
	    }

	    public WrapMode postWrapMode
	    {
		    get;
		    set;
	    }

	    public AnimationCurve(params Keyframe[] keys)
	    {
		    Init(keys);
	    }


	    public AnimationCurve()
	    {
		    // Init(null); //    <- original behaviour
            // currently just let it sit at zero elem inter
	    }


	    private /*extern*/ void Cleanup() {}

	    ~AnimationCurve()
	    {
		    Cleanup();
	    }


	    public /* extern */ float Evaluate(float time) { throw new NotImplementedException() ; }

	    
	    public /* extern */ int AddKey(float time, float value) { throw new NotImplementedException() ; }

	    public int AddKey(Keyframe key)
	    {
		    return AddKey_Internal(key);
	    }

	    private int AddKey_Internal(Keyframe key)
	    {
            throw new NotImplementedException() ; 
		    //return INTERNAL_CALL_AddKey_Internal(this, ref key);
	    }
        /* 
	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private static extern int INTERNAL_CALL_AddKey_Internal(AnimationCurve self, ref Keyframe key);
        */

	    public int MoveKey(int index, Keyframe key)
	    {
            throw new NotImplementedException() ;
		    //return INTERNAL_CALL_MoveKey(this, index, ref key);
	    }

        /*
	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private static extern int INTERNAL_CALL_MoveKey(AnimationCurve self, int index, ref Keyframe key);
        */

	    
	    public /* extern */ void RemoveKey(int index) { throw new NotImplementedException() ; } 

	    
	    private /* extern */ void SetKeys(Keyframe[] keys) { __keys = keys ; } 

	    private Keyframe GetKey_Internal(int index) => __keys[index] ; 
	    

	    

	    
	    private /* extern */ Keyframe[] GetKeys() => __keys;

	    
	    public /* extern */ void SmoothTangents(int index, float weight) { throw new NotImplementedException() ; } 

	    public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
	    {
		    float num = (valueEnd - valueStart) / (timeEnd - timeStart);
		    Keyframe[] keys = new Keyframe[2]
		    {
			    new Keyframe(timeStart, valueStart, 0f, num),
			    new Keyframe(timeEnd, valueEnd, num, 0f)
		    };
		    return new AnimationCurve(keys);
	    }

	    public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
	    {
		    Keyframe[] keys = new Keyframe[2]
		    {
			    new Keyframe(timeStart, valueStart, 0f, 0f),
			    new Keyframe(timeEnd, valueEnd, 0f, 0f)
		    };
		    return new AnimationCurve(keys);
	    }

	    
	    private /* extern */ void Init(Keyframe[] keys) { 
            __keys = keys ;
        }
    }


    public class TrackedReference
    {
	    internal IntPtr m_Ptr;

	    protected TrackedReference()
	    {
	    }

	    public static bool operator ==(TrackedReference x, TrackedReference y)
	    {
		    if ((object)y == null && (object)x == null)
		    {
			    return true;
		    }
		    if ((object)y == null)
		    {
			    return x.m_Ptr == IntPtr.Zero;
		    }
		    if ((object)x == null)
		    {
			    return y.m_Ptr == IntPtr.Zero;
		    }
		    return x.m_Ptr == y.m_Ptr;
	    }

	    public static bool operator !=(TrackedReference x, TrackedReference y)
	    {
		    return !(x == y);
	    }

	    public override bool Equals(object o)
	    {
		    return o as TrackedReference == this;
	    }

	    public override int GetHashCode()
	    {
		    return (int)m_Ptr;
	    }

	    public static implicit operator bool(TrackedReference exists)
	    {
		    return exists != null;
	    }
    }



    public sealed class AnimationState : TrackedReference
    {
	    public bool enabled
	    {
		    get;
		    set;
	    }

	    public float weight
	    {
		    get;
		    set;
	    }

	    public WrapMode wrapMode
	    {
		    get;
		    set;
	    }

	    public float time
	    {
		    get;
		    set;
	    }

	    public float normalizedTime
	    {
		    get;
		    set;
	    }

	    public float speed
	    {
		    get;
		    set;
	    }

	    public float normalizedSpeed
	    {
		    get;
		    set;
	    }

	    public float length
	    {
		    get;
	    }

	    public int layer
	    {
		    get;
		    set;
	    }

	    public AnimationClip clip
	    {
		    get;
	    }

	    public string name
	    {
		    get;
		    set;
	    }

	    public AnimationBlendMode blendMode
	    {
		    get;
		    set;
	    }

	    //[MethodImpl(MethodImplOptions.InternalCall)]
	    //[GeneratedByOldBindingsGenerator]
	    public /*extern*/ void AddMixingTransform(Transform mix, /* [DefaultValue("true")]*/ bool recursive){ throw new NotImplementedException() ; }

	    //[ExcludeFromDocs]
	    public void AddMixingTransform(Transform mix)
	    {
		    bool recursive = true;
		    AddMixingTransform(mix, recursive);
	    }

	    
	    public /*extern*/ void RemoveMixingTransform(Transform mix){ throw new NotImplementedException() ; } 
    }



    public sealed class AnimationEvent
    {
	    internal float m_Time;

	    internal string m_FunctionName;

	    internal string m_StringParameter;

	    internal UnityEngine.Object m_ObjectReferenceParameter;

	    internal float m_FloatParameter;

	    internal int m_IntParameter;

	    internal int m_MessageOptions;

	    internal AnimationEventSource m_Source;

	    internal AnimationState m_StateSender;

	    internal AnimatorStateInfo m_AnimatorStateInfo;

	    internal AnimatorClipInfo m_AnimatorClipInfo;

	    [Obsolete("Use stringParameter instead")]
	    public string data
	    {
		    get
		    {
			    return m_StringParameter;
		    }
		    set
		    {
			    m_StringParameter = value;
		    }
	    }

	    public string stringParameter
	    {
		    get
		    {
			    return m_StringParameter;
		    }
		    set
		    {
			    m_StringParameter = value;
		    }
	    }

	    public float floatParameter
	    {
		    get
		    {
			    return m_FloatParameter;
		    }
		    set
		    {
			    m_FloatParameter = value;
		    }
	    }

	    public int intParameter
	    {
		    get
		    {
			    return m_IntParameter;
		    }
		    set
		    {
			    m_IntParameter = value;
		    }
	    }

	    public UnityEngine.Object objectReferenceParameter
	    {
		    get
		    {
			    return m_ObjectReferenceParameter;
		    }
		    set
		    {
			    m_ObjectReferenceParameter = value;
		    }
	    }

	    public string functionName
	    {
		    get
		    {
			    return m_FunctionName;
		    }
		    set
		    {
			    m_FunctionName = value;
		    }
	    }

	    public float time
	    {
		    get
		    {
			    return m_Time;
		    }
		    set
		    {
			    m_Time = value;
		    }
	    }

	    public SendMessageOptions messageOptions
	    {
		    get
		    {
			    return (SendMessageOptions)m_MessageOptions;
		    }
		    set
		    {
			    m_MessageOptions = (int)value;
		    }
	    }

	    public bool isFiredByLegacy => m_Source == AnimationEventSource.Legacy;

	    public bool isFiredByAnimator => m_Source == AnimationEventSource.Animator;

	    public AnimationState animationState
	    {
		    get
		    {
			    if (!isFiredByLegacy)
			    {
				    //Debug.LogError("AnimationEvent was not fired by Animation component, you shouldn't use AnimationEvent.animationState");
			    }
			    return m_StateSender;
		    }
	    }

	    public AnimatorStateInfo animatorStateInfo
	    {
		    get
		    {
			    if (!isFiredByAnimator)
			    {
				    //Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorStateInfo");
			    }
			    return m_AnimatorStateInfo;
		    }
	    }

	    public AnimatorClipInfo animatorClipInfo
	    {
		    get
		    {
			    if (!isFiredByAnimator)
			    {
				    //Debug.LogError("AnimationEvent was not fired by Animator component, you shouldn't use AnimationEvent.animatorClipInfo");
			    }
			    return m_AnimatorClipInfo;
		    }
	    }

	    public AnimationEvent()
	    {
		    m_Time = 0f;
		    m_FunctionName = "";
		    m_StringParameter = "";
		    m_ObjectReferenceParameter = null;
		    m_FloatParameter = 0f;
		    m_IntParameter = 0;
		    m_MessageOptions = 0;
		    m_Source = AnimationEventSource.NoSource;
		    m_StateSender = null;
	    }

	    internal int GetHash()
	    {
		    int num = 0;
		    num = functionName.GetHashCode();
		    return 33 * num + time.GetHashCode();
	    }
    }



    public class Motion : UnityEngine.Object
    {
	    public float averageDuration
	    {
		    
		    get;
	    }

	    public float averageAngularSpeed
	    {
		    
		    get;
	    }

	    public Vector3 averageSpeed
	    {
		    get;
		    /*{
			    INTERNAL_get_averageSpeed(out Vector3 value);
			    return value;
		    }*/
	    }

	    public float apparentSpeed
	    {
		    
		    get;
	    }

	    public bool isLooping
	    {
		    
		    get;
	    }

	    public bool legacy
	    {
		    
		    get;
	    }

	    public bool isHumanMotion
	    {
		    
		    get;
	    }

	    [Obsolete("isAnimatorMotion is not supported anymore. Use !legacy instead.", true)]
	    public bool isAnimatorMotion
	    {
		    
		    get;
	    }

        /*
	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [GeneratedByOldBindingsGenerator]
	    private extern void INTERNAL_get_averageSpeed(out Vector3 value);

	    [MethodImpl(MethodImplOptions.InternalCall)]
	    [Obsolete("ValidateIfRetargetable is not supported anymore. Use isHumanMotion instead.", true)]
	    [GeneratedByOldBindingsGenerator]
	    public extern bool ValidateIfRetargetable(bool val);
        */
    }




public sealed class AnimationClip : Motion
{
	public float length
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
	}

	internal float startTime
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
	}

	internal float stopTime
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
	}

	public float frameRate
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		set;
	}

	public WrapMode wrapMode
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		set;
	}

	public Bounds localBounds
	{
		get
		{
            throw new NotImplementedException();
			//INTERNAL_get_localBounds(out Bounds value);
			//return value;
		}
		set
		{
            throw new NotImplementedException();
			//INTERNAL_set_localBounds(ref value);
		}
	}

	public new bool legacy
	{
	
	
		get;
	
		set;
	}

	public bool humanMotion
	{

		get;
	}

	public bool empty
	{

		get;
	}

	public AnimationEvent[] events
	{
		get
		{
			return (AnimationEvent[])GetEventsInternal();
		}
		set
		{
			SetEventsInternal(value);
		}
	}

	internal bool hasRootMotion
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
	}

	public AnimationClip()
	{
		Internal_CreateAnimationClip(this);
	}

	
	public /*extern */ void SampleAnimation(GameObject go, float time) => throw new NotImplementedException(); 

	
	private static /* extern*/ void Internal_CreateAnimationClip(/*[Writable]*/ AnimationClip self) => throw new NotImplementedException(); 

	
	public /* extern */ void SetCurve(string relativePath, Type type, string propertyName, AnimationCurve curve) => throw new NotImplementedException(); 

	public void EnsureQuaternionContinuity()
	{
        throw new NotImplementedException(); 
		// INTERNAL_CALL_EnsureQuaternionContinuity(this);
	}
    /*
	[MethodImpl(MethodImplOptions.InternalCall)]
	[GeneratedByOldBindingsGenerator]
	private static extern void INTERNAL_CALL_EnsureQuaternionContinuity(AnimationClip self);
    */
	public void ClearCurves()
	{
        throw new NotImplementedException(); 
		// INTERNAL_CALL_ClearCurves(this);
	}
    /*
	[MethodImpl(MethodImplOptions.InternalCall)]
	[GeneratedByOldBindingsGenerator]
	private static extern void INTERNAL_CALL_ClearCurves(AnimationClip self);
    */

    /*
	[MethodImpl(MethodImplOptions.InternalCall)]
	[GeneratedByOldBindingsGenerator]
	private extern void INTERNAL_get_localBounds(out Bounds value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[GeneratedByOldBindingsGenerator]
	private extern void INTERNAL_set_localBounds(ref Bounds value);
    */

	public void AddEvent(AnimationEvent evt)
	{
		if (evt == null)
		{
			throw new ArgumentNullException("evt");
		}
		AddEventInternal(evt);
	}

	
	internal /*extern*/ void AddEventInternal(object evt) => throw new NotImplementedException();

	
	internal /*extern*/ void SetEventsInternal(Array value) => throw new NotImplementedException();

	
	internal /*extern*/ Array GetEventsInternal() => throw new NotImplementedException();
}



public sealed class Animator : Behaviour   // giganto class -- incomplete 
{
    public Animator( GameObject go ) : base ( go ) { }
    public static /*extern*/ int StringToHash(string name) { throw new NotImplementedException() ; } 

    public RuntimeAnimatorController runtimeAnimatorController
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		set;
	}

}


public class RuntimeAnimatorController : Object
{
	public AnimationClip[] animationClips
	{
		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[GeneratedByOldBindingsGenerator]
		get;
	}
}

} 


/// AnimationUtility kram 

namespace UnityEditor {
    
    
    // also giganto class, and incomplete 
    public class AnimationUtility
    {
        public static  EditorCurveBinding[] GetCurveBindings( AnimationClip clip){ // TODO 
            return new EditorCurveBinding[0];    
        }
    }


    /// <summary>
    ///   <para>Defines how a curve is attached to an object that it controls.</para>
    /// </summary>
    public struct EditorCurveBinding : IEquatable<EditorCurveBinding>
    {
	    /// <summary>
	    ///   <para>The transform path of the object that is animated.</para>
	    /// </summary>
	    public string path;

	    private Type m_type;

	    /// <summary>
	    ///   <para>The name of the property to be animated.</para>
	    /// </summary>
	    public string propertyName;

	    private int m_isPPtrCurve;

	    private int m_isDiscreteCurve;

	    private int m_isPhantom;

	    internal int m_ClassID;

	    internal int m_ScriptInstanceID;

	    public bool isPPtrCurve => m_isPPtrCurve != 0;

	    public bool isDiscreteCurve => m_isDiscreteCurve != 0;

	    internal bool isPhantom
	    {
		    get
		    {
			    return m_isPhantom != 0;
		    }
		    set
		    {
			    m_isPhantom = (value ? 1 : 0);
		    }
	    }

	    /// <summary>
	    ///   <para>The type of the property to be animated.</para>
	    /// </summary>
	    public Type type
	    {
		    get
		    {
			    return m_type;
		    }
		    set
		    {
			    m_type = value;
			    m_ClassID = 0;
			    m_ScriptInstanceID = 0;
		    }
	    }

	    public static bool operator ==(EditorCurveBinding lhs, EditorCurveBinding rhs)
	    {
		    if (lhs.m_ClassID != 0 && rhs.m_ClassID != 0 && (lhs.m_ClassID != rhs.m_ClassID || lhs.m_ScriptInstanceID != rhs.m_ScriptInstanceID))
		    {
			    return false;
		    }
		    return lhs.m_isPPtrCurve == rhs.m_isPPtrCurve && lhs.m_isDiscreteCurve == rhs.m_isDiscreteCurve && lhs.path == rhs.path && lhs.type == rhs.type && lhs.propertyName == rhs.propertyName;
	    }

	    public static bool operator !=(EditorCurveBinding lhs, EditorCurveBinding rhs)
	    {
		    return !(lhs == rhs);
	    }

	    public override int GetHashCode()
	    {
		    return $"{path}:{type.Name}:{propertyName}".GetHashCode();
	    }

	    public override bool Equals(object other)
	    {
		    return other is EditorCurveBinding && Equals((EditorCurveBinding)other);
	    }

	    public bool Equals(EditorCurveBinding other)
	    {
		    return this == other;
	    }

	    private static void BaseCurve(string inPath, Type inType, string inPropertyName, out EditorCurveBinding binding)
	    {
		    binding = default(EditorCurveBinding);
		    binding.path = inPath;
		    binding.type = inType;
		    binding.propertyName = inPropertyName;
		    binding.m_isPhantom = 0;
	    }

	    /// <summary>
	    ///   <para>Creates a preconfigured binding for a float curve.</para>
	    /// </summary>
	    /// <param name="inPath">The transform path to the object to animate.</param>
	    /// <param name="inType">The type of the object to animate.</param>
	    /// <param name="inPropertyName">The name of the property to animate on the object.</param>
	    public static EditorCurveBinding FloatCurve(string inPath, Type inType, string inPropertyName)
	    {
		    BaseCurve(inPath, inType, inPropertyName, out EditorCurveBinding binding);
		    binding.m_isPPtrCurve = 0;
		    binding.m_isDiscreteCurve = 0;
		    return binding;
	    }

	    /// <summary>
	    ///   <para>Creates a preconfigured binding for a curve that points to an Object.</para>
	    /// </summary>
	    /// <param name="inPath">The transform path to the object to animate.</param>
	    /// <param name="inType">The type of the object to animate.</param>
	    /// <param name="inPropertyName">The name of the property to animate on the object.</param>
	    public static EditorCurveBinding PPtrCurve(string inPath, Type inType, string inPropertyName)
	    {
		    BaseCurve(inPath, inType, inPropertyName, out EditorCurveBinding binding);
		    binding.m_isPPtrCurve = 1;
		    binding.m_isDiscreteCurve = 1;
		    return binding;
	    }

	    /// <summary>
	    ///   <para>Creates a preconfigured binding for a curve where values should not be interpolated.</para>
	    /// </summary>
	    /// <param name="inPath">The transform path to the object to animate.</param>
	    /// <param name="inType">The type of the object to animate.</param>
	    /// <param name="inPropertyName">The name of the property to animate on the object.</param>
	    public static EditorCurveBinding DiscreteCurve(string inPath, Type inType, string inPropertyName)
	    {
		    BaseCurve(inPath, inType, inPropertyName, out EditorCurveBinding binding);
		    binding.m_isPPtrCurve = 0;
		    binding.m_isDiscreteCurve = 1;
		    return binding;
	    }
    }

}

