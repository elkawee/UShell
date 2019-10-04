


using PLJGlue   = LightJson.Glue;
using JsonValue = LightJson.JsonValue;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

using SObject = System.Object;
using D = System.Diagnostics.Debug;


namespace TypeMapping {

    public class Mapping {
        public class ConsumerBase {
            public Type LHS_Type;
            public Type expected_RHS_Type;
            public object F;  // needs to be castable into Func<expected_RHS_Type,LHS_Type> but 3.5 doesn't have variance, so there is no common base type to any closed generic type
        }

        #region old list-based consumers 
        public class ConsumerArrayLHS_LIST<LHS_ElemT>:ConsumerBase {
            public ConsumerArrayLHS_LIST() {
                ConsumerBase elemConsumer = GetConsumer(typeof(LHS_ElemT));
                LHS_Type = typeof(LHS_ElemT[]);
                expected_RHS_Type = typeof(List<>).MakeGenericType(new[] { elemConsumer.expected_RHS_Type });
                var closed_mkFunc = (this).GetType()/*class type should be closed*/.GetMethod("MkFunc").MakeGenericMethod(new[] { elemConsumer.expected_RHS_Type });
                F = closed_mkFunc.Invoke(null,new[] { elemConsumer.F }); // if the runtime type of elemConsumer.F doesnt match , it throws here 
            }
            /*
                the sole reason for this indirection is that c# can't translate member calls on open generic types 
                e.g. i need T in List<T>.Count() bound here
                (introduce RHS_ElemT as generic variable, only to specialize it the constructor ) 
                ... there probably is a less convoluted way of doing this  
            */
            public static Func<List<RHS_ElemT>,LHS_ElemT[]> MkFunc<RHS_ElemT>(Func<RHS_ElemT,LHS_ElemT> elemFunc) {
                Func<List<RHS_ElemT>,LHS_ElemT[]> F = (L_in) =>
                {
                    int len = L_in.Count();
                    var R = new LHS_ElemT[len];
                    var rator = L_in.GetEnumerator();
                    for(int i = 0;i < len;i++) {
                        rator.MoveNext();
                        R[i] = elemFunc(rator.Current);
                    }
                    return R;
                };
                return F;
            }
        }
        #endregion 

        public class ConsumerArrayLHS<LHS_ElemT>:ConsumerBase {
            public ConsumerArrayLHS() {
                ConsumerBase elemConsumer = GetConsumer(typeof(LHS_ElemT));
                LHS_Type = typeof(LHS_ElemT[]);
                expected_RHS_Type = typeof(IEnumerable<>).MakeGenericType(new[] { elemConsumer.expected_RHS_Type });
                var closed_mkFunc = (this).GetType()/*class type should be closed*/.GetMethod("MkFunc").MakeGenericMethod(new[] { elemConsumer.expected_RHS_Type });
                F = closed_mkFunc.Invoke(null,new[] { elemConsumer.F }); // if the runtime type of elemConsumer.F doesnt match , it throws here 
            }
            /*
                the sole reason for this indirection is that c# can't translate member calls on open generic types 
                e.g. i need T in: List<T>.Count() bound here
                (introduce RHS_ElemT as generic variable, only to specialize it the constructor ) 
                ... there probably is a less convoluted way of doing this  
            */
            public static Func<IEnumerable<RHS_ElemT>,LHS_ElemT[]> MkFunc<RHS_ElemT>(Func<RHS_ElemT,LHS_ElemT> elemFunc) {
                Func<IEnumerable<RHS_ElemT>,LHS_ElemT[]> F = (L_in) =>
                {
                    int len = L_in.Count();
                    var R = new LHS_ElemT[len];
                    var rator = L_in.GetEnumerator();
                    for(int i = 0;i < len;i++) {
                        rator.MoveNext();
                        R[i] = elemFunc(rator.Current);
                    }
                    return R;
                };
                return F;
            }
        }
        #region indexer_consumers 

        public class RHS_Shape_Exception : Exception { } 
        /*
            for indexer types with c#-compile-time-static lengths
            length of the RHS-side ( json) elements are enforced from LHS ( the type the consumer is an adapter for ) 
            - superfluous RHS elems are ignored (for now)  
            - too few RHS elems throw

            also , all of these generate new instances of the type they shadow (MotherT )
            for in-place modification more work is needed - particularly value types with this[,] operators run into the usual implicit boxing problem 
        */

        public class ConsumerIndexer_New_LHS<MotherT, ElemT>:ConsumerBase where MotherT : new() {
            public ConsumerIndexer_New_LHS(int len) {       // for now :  only indexers that have constant a [0..len) domain for the indexer argument, "len" can in general not be discovered from type or instance 
                LHS_Type = typeof(MotherT);
                expected_RHS_Type = typeof(IEnumerable<ElemT>);
                var elemConsumer = GetConsumer(typeof(ElemT));

                var closed_mkFun = GetType().GetMethod("MkFun").MakeGenericMethod(new[] { elemConsumer.expected_RHS_Type });
                F = closed_mkFun.Invoke(null,new[] { elemConsumer.F,len });
            }
            public static Func<IEnumerable<RHS_ElemT>,MotherT> MkFun<RHS_ElemT>(Func<RHS_ElemT,ElemT> elemFunc,int len) {
                // can't really determine wheter indexer property is implemented from the type system 
                MethodInfo setterMI = typeof(MotherT).GetProperty("Item").GetSetMethod();
                Func<IEnumerable<RHS_ElemT>,MotherT> F = (L_in) =>
                {
                    var R = new MotherT();
                    object obR = RuntimeHelpers.GetObjectValue(R);   // todo only for value types - how is this different from casting to (object) ?? 
                    var rator = L_in.GetEnumerator();
                    for(int i = 0;i < len;i++) {
                        if ( ! rator.MoveNext() ) throw new RHS_Shape_Exception();
                        setterMI.Invoke(obR,new object[] { i,elemFunc(rator.Current) });
                    }
                    return (MotherT)obR;
                };
                return F;
            }

        }
        // inelegant & copypasta - until there is an actual case of even dim3 ... not much of a point of doing the generic case 
        public class ConsumerIndexer_Dim2_New_LHS<MotherT, ElemT>:ConsumerBase where MotherT : new() {
            public ConsumerIndexer_Dim2_New_LHS(int len_outer , int len_inner ) {       // for now :  only indexers that have constant a [0..len) domain for the indexer argument, "len" can in general not be discovered from type or instance 
                LHS_Type = typeof(MotherT);
                expected_RHS_Type = typeof(IEnumerable<IEnumerable<ElemT>>);
                var elemConsumer = GetConsumer(typeof(ElemT));

                var closed_mkFun = GetType().GetMethod("MkFun").MakeGenericMethod(new[] { elemConsumer.expected_RHS_Type });
                F = closed_mkFun.Invoke(null,new[] { elemConsumer.F,len_outer , len_inner });
            }
            public static Func<IEnumerable<IEnumerable<RHS_ElemT>>,MotherT> MkFun<RHS_ElemT>(Func<RHS_ElemT,ElemT> elemFunc,int len_outer , int len_inner) {
                // can't really determine wheter indexer property is implemented from the type system 
                MethodInfo setterMI = typeof(MotherT).GetMethod("set_Item",new [] { typeof(int) , typeof(int) , typeof(ElemT) } ); 
                Func<IEnumerable<IEnumerable<RHS_ElemT>>,MotherT> F = (L_in) =>
                {
                    var R = new MotherT();
                    object obR = RuntimeHelpers.GetObjectValue(R);   // todo only for value types - how is this different from casting to (object) ?? 
                    var rator_outer = L_in.GetEnumerator();
                    for(int i_outer = 0;i_outer < len_outer;i_outer++) {

                        if ( ! rator_outer.MoveNext() ) throw new RHS_Shape_Exception();  // <- these don't fly properly they are somehow 
                        var rator_inner = rator_outer.Current.GetEnumerator();

                        for(int i_inner = 0;i_inner < len_inner;i_inner++) {
                            if ( ! rator_inner.MoveNext()  ) throw new RHS_Shape_Exception();
                            setterMI.Invoke(obR,new object[] { i_outer, i_inner,elemFunc(rator_inner.Current) });
                        }

                    }
                    return (MotherT)obR;
                };
                return F;
            }

        }

        #endregion 
        public static Dictionary<Type,ConsumerBase> _Consumers;
        public static bool                          _ConsumersIsInitialized = false ;
        public static Dictionary<Type,ConsumerBase> Consumers { get { if(!_ConsumersIsInitialized) { InitConsumersTable(); } return _Consumers;} }
        static void InitConsumersTable() {
            _Consumers = new Dictionary<Type,ConsumerBase>();
            _Consumers[typeof(int)]        = MkID<int>();
            _Consumers[typeof(float)]      = MkID<float>();
            _Consumers[typeof(string)]     = MkID<string>();
            _ConsumersIsInitialized = true ;               // cheating because recursively defined Consumers access this structure - this is also why this can't be a static constructor 
            
            _Consumers[typeof(Vector2)]                                      = new ConsumerIndexer_New_LHS<Vector2,float>(2);
            _Consumers[typeof(Vector3)]                                      = new ConsumerIndexer_New_LHS<Vector3,float>(3);
            _Consumers[typeof(Vector4)]                                      = new ConsumerIndexer_New_LHS<Vector4,float>(4);
            _Consumers[typeof(Quaternion)]                                   = new ConsumerIndexer_New_LHS<Quaternion,float>(4);
            _Consumers[typeof(Color)]                                        = new ConsumerIndexer_New_LHS<Color,float>(4);
            _Consumers[typeof(UnityEngine.Rendering.SphericalHarmonicsL2)]   = new ConsumerIndexer_Dim2_New_LHS<UnityEngine.Rendering.SphericalHarmonicsL2,float>(2,8);
            _Consumers[typeof(Matrix4x4)]                                    = new ConsumerIndexer_Dim2_New_LHS<Matrix4x4,float>(4,4);
            // AnimationCurve also has an indexer but read-only 
            // Animation has one but it's indexed by string and references an object -- no clue atm what to do about this 
            
        }
        #region interface 

        public static ConsumerBase MkID<T>() {
            Func<T,T> id = _ => _;
            return new ConsumerBase { LHS_Type = typeof(T),expected_RHS_Type = typeof(T),F = (object)id };
        }
        public static ConsumerBase GetConsumer(Type lhs_type) {
            if(Consumers.ContainsKey(lhs_type)) return Consumers[lhs_type];
            if(lhs_type.IsArray) {
                if ( lhs_type.GetArrayRank() == 1 ) { 
                    var Cons = (ConsumerBase)Activator.CreateInstance(typeof(ConsumerArrayLHS_LIST<>).MakeGenericType(new[] { lhs_type.GetElementType() }));
                    Consumers[Cons.LHS_Type] = Cons;
                    return Cons;
                } else {
                    throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }

        #endregion 

        // ---- 

        #region tests probably move to EditorlessTests project later 

        public static void  Test1() {

            var c = GetConsumer( typeof ( int[] ) ) ;
            var F = (Func<List<int>, int[]> ) c.F;
            Console.Write( F ( new List<int>( new [] { 1,2,3,3 } )));

            var c2 = GetConsumer( typeof ( Vector3[] ));
            var F2 = (Func< List<List<float>> ,Vector3[]> ) c2.F;

            var L = new List<float> ( new [] { 1f,2f,3f } );
            var LL = new List<List<float>> () ;
            LL.Add( L ) ; LL.Add( L ); 
            Console.Write( F2 ( LL ));

        }
        #endregion  

    }

    /*
        general idea is to have a two phase process for deserialization 
        TypeMapping.Mapping is like a normalization of c# types to small subset with an obvious JSON mapping 
        example  Vector3 => IEnumerable<float> , Matrix4x4 => IEnumerable<IEnumerable<float>>

        from that, ask the json library if it can provide a view, matching that normalized type, for each _particular instance_ of incoming JSON idividually 

        this means at parse time only whether the json is valid at all can be detected 
        whether it is assignable, has to be deferred until after typing 
    */


    public class LightJSonAdapter  {
        /*
                  the actual entry point : ---.
                                             \|/         
                                              '               for opcodes 
        */
        public static FullType FromJson<FullType> ( JsonValue JV ) {
            Mapping.ConsumerBase    Consumer            = Mapping.GetConsumer( typeof ( FullType ));
            Type                    normalizedType      = Consumer.expected_RHS_Type;
            SObject                 intermediateVal     = PLJGlue.Convert( normalizedType , JV );
            MethodInfo              closedRoundabout    = typeof ( LightJSonAdapter ).GetMethod("roundabout" ).MakeGenericMethod( new [] { normalizedType, typeof ( FullType ) } );
            try { 
                return (FullType) closedRoundabout.Invoke( null , new object [] { intermediateVal , Consumer.F} );
            } catch ( TargetInvocationException e ) {
                throw e.InnerException;         // the actual exception whithin the function being invoked ( gets wrapped by reflection  )
            }
            
        }
        public static Tres roundabout<Tin,Tres> ( Tin arg , object F) {
            return (F as Func<Tin,Tres>)(arg);  // because the instances of converter delegates in Consumer.F are explicitly typed ? yup 
        }
        // and from dynamic 
        static readonly MethodInfo  FromJsonMI = typeof(LightJSonAdapter).GetMethod("FromJson" , new [] { typeof ( JsonValue) } ) ;
        public static SObject FromJson ( JsonValue JV , Type fullType ) {
            try { 
                return FromJsonMI.MakeGenericMethod( new [] { fullType } ).Invoke( null, new object[] { JV } ) ;
            } catch ( TargetInvocationException e ) {  throw e.InnerException ; }

        }

        public static void Test1 () {

            var X = new Mapping.ConsumerIndexer_New_LHS<Vector3,float>(3);

            string dummy = "";
            JsonValue JV_dim1_arr3 = PLJGlue.ParseWithRest( "[1,2,3]" , out dummy ) ; 
            Console.WriteLine ( FromJson<Vector3> ( JV_dim1_arr3 ) );
            Console.WriteLine ( FromJson<float[]> ( JV_dim1_arr3 ) );

            

            JsonValue JV_dim2_arr44 = PLJGlue.ParseWithRest( "[[1,2,3,4],[1,2,3,4],[1,2,3,4],[1,2,3,4]]" , out dummy ) ; 
            JsonValue JV_dim2_arr28 = PLJGlue.ParseWithRest( "[[1,2,3,4,1,2,3,4],[1,2,3,4,1,2,3,4]]" , out dummy ) ; 
            
            
            Console.WriteLine ( FromJson<Matrix4x4> ( JV_dim2_arr44 ) );
            Console.WriteLine ( FromJson<UnityEngine.Rendering.SphericalHarmonicsL2> ( JV_dim2_arr28 ) );

        }
    }
    

}