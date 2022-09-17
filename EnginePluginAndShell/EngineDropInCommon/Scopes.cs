using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using SObject = System.Object;

using NLSPlain;

using TranslateAndEval ;
using D = System.Diagnostics.Debug;



namespace TranslateAndEval {

    

    public class LL<T> {               // structural sharing assoc list - a closed scope is identical to a reference to one of these nodes, plus some convenience funcs 
        public readonly LL<T> prev ;
        public readonly string         name ;
        public readonly T              pay ;

        public LL ( string name , T pay , LL<T> prev ) { this.prev = prev ; this.pay = pay ; this.name = name ;}
    }

    public class LL_Exception : Exception { }
  
    // this way nulls are valid empty lists 
    public static class LL_Extensions { 
        public static LL<T>              chain<T> ( this LL<T> orig , string name , T pay ) => new LL<T>( name ,pay , orig ) ;

        public static LL<T> findNode<T> ( this LL<T> node_in ,  string search_name ) {
            LL<T> node = node_in ;
            while(true ) {
                if ( node == null ) throw new LL_Exception();
                if ( search_name == node.name ) return node;
                node = node.prev ;
            }
        }
        public static T findPay<T> ( this LL<T> node_in ,  string search_name ) => node_in.findNode( search_name).pay;  // todo remove 

        public static IEnumerable<LL<T>> LIFO<T>  ( this LL<T> node_in)                       {
            for ( var node = node_in ; node != null  ; node = node.prev ) yield return node ;
        }
        public static IEnumerable<LL<T>> LIFO_shadowed<T> (this LL<T> node_in) {
            
            var seen = new HashSet<string>();
            foreach ( var node in node_in.LIFO() ) if ( ! seen.Contains( node.name ) ) {
                seen.Add( node.name );
                yield return node;
            }
        }
    }

    public class ScopeException : Exception          { public ScopeException() { } public ScopeException(string msg) : base ( msg ) { } } 
    public class ScopeFindException : ScopeException { } 

    public abstract class CH_Scope {
        public struct Ref { public string name ; public TypedCH CH ; }
        public static Ref LL2Ref(LL<TypedCH> ll ) => new Ref { name = ll.name , CH = ll.pay } ;
    }
    



    public class CH_closedScope : CH_Scope{
        public readonly LL<TypedCH> LL_head= null ;     // empty scope 


               CH_closedScope( LL<TypedCH> ll_in)                        { LL_head = ll_in ; }
        public CH_closedScope()                                          { }
        /*
         clones the entire chain 
        */
        public CH_closedScope( IEnumerable<Ref> items  ) {  
            foreach ( var itm in items ) LL_head = LL_head.chain(itm.name , itm.CH);
        }

        public CH_closedScope             decl ( string name , TypedCH CH ) => new CH_closedScope ( LL_head.chain(name , CH ) );
        public IEnumerable< Ref >         refs ()                           => LL_head.LIFO_shadowed().Select( LL2Ref );

        public Ref                        resolve ( string name ) {

            try                    { return LL2Ref ( LL_head.findNode(name) );                 } 
            catch ( LL_Exception ) { throw new ScopeFindException();  }
        }
    }
    




    public class CH_deltaScope : CH_Scope {

        public readonly CH_closedScope orig_scope ;       // must never be null 
        public readonly LL<TypedCH>    ownLL_head = null ;
        public readonly LL<TypedCH>    externals  = null ;

        public CH_deltaScope( CH_closedScope clsSc ) {
            if ( clsSc == null ) throw new ScopeException( "origin scope can't be null" );
            orig_scope = clsSc;
            // own and externals stay empty 
        }
        
        public CH_deltaScope( CH_closedScope clsSc , LL<TypedCH> ll , LL<TypedCH> externals )  {
            if ( clsSc == null ) throw new ScopeException( "origin scope can't be null" );
            orig_scope = clsSc;
            this.ownLL_head = ll ;
            this.externals = externals;
        }


        public CH_deltaScope decl ( string name , TypedCH CH ) {
            return new CH_deltaScope ( orig_scope , ownLL_head.chain( name , CH ) , externals  );
        }

        public CH_deltaScope addRef ( string name , out TypedCH CH ) {    // instead of resolve - but this one has "side effects" i.e. it returns an updated variant 
            try { CH = ownLL_head.findNode(name).pay; return this ; }
            catch ( LL_Exception ) { }
            try {
                CH = orig_scope.LL_head.findNode(name).pay;
                return new CH_deltaScope( orig_scope , ownLL_head , externals.chain(name, CH ) );
            } catch ( LL_Exception ) { }
            throw new ScopeException();
        }

        public IEnumerable<Ref>  refs() {
            var seen = new HashSet<string>();
            foreach ( var node in ownLL_head.          LIFO() ) if (!seen.Contains(node.name )) 
                    { seen.Add( node.name ) ; yield return LL2Ref(node); }
            foreach ( var node in orig_scope.LL_head.  LIFO() ) if (!seen.Contains(node.name )) 
                    { seen.Add( node.name ) ; yield return LL2Ref(node); }

        }
        public CH_closedScope   close() {
            var R = orig_scope ;
            foreach ( var n in ownLL_head.LIFO_shadowed().Reverse() ) R = R.decl(n.name , n.pay ); return R;
        }
        /*
        for externals there is no shadowing - they are outside references - using LIFO_shadowed simply because it removes duplicates 
        */
        public IEnumerable<Ref> external_refs () => externals.LIFO_shadowed().Select( LL2Ref );


    }
    public class preCH_deltaScope {            // interface wise the same as CH_deltaScope , but those preCHs need extra care 
        public struct Ref {
            public string name ;
            public preCH  pre_ch ;
        }
        static Ref LL2Ref ( LL<preCH> ll ) => new Ref { name = ll.name , pre_ch = ll.pay };

        public readonly CH_closedScope origScope ;      // never null this whole delta scoping business is only meaningful with a concrete scope that is delta-d to

        public readonly LL<preCH> origLL_head ;         // only null if the "to be delta-d" scope was empty ( contains a map of the closed scopes CHs into adapters ) 
        public readonly LL<preCH> ownLL_head  = null ;
        public readonly LL<preCH> externals  = null ;

               
        public preCH_deltaScope ( CH_closedScope clSC ) {
            D.Assert( clSC != null ) ;
            origScope = clSC;
            foreach ( var nodeCH in clSC.LL_head.LIFO_shadowed().Reverse() ) {
                origLL_head = origLL_head.chain( nodeCH.name , new adapter_preCH ( nodeCH.pay ) );
            }
        }

        preCH_deltaScope( CH_closedScope origScope ,LL<preCH> origLL , LL<preCH> own , LL<preCH> extnls ) {
            D.Assert( origScope != null );
            this.origScope = origScope ;
            origLL_head = origLL;
            ownLL_head  = own ;
            externals   = extnls ;
        } // the readonlys need a detour over constructor 
        
        public preCH_deltaScope decl ( string name , preCH pre_ch ) => new preCH_deltaScope ( 
            origScope , 
            origLL_head,
            ownLL_head.chain( name , pre_ch ) , 
            externals );

        public preCH_deltaScope addRef ( string name , out preCH pCH_out ) {  // this needs a more descriptive name 
            try {
                pCH_out = ownLL_head.findNode(name).pay ; 
                return this;
            }  catch ( LL_Exception ) { }
            try {
                pCH_out = origLL_head.findNode(name).pay ;
                return new preCH_deltaScope ( origScope , origLL_head, ownLL_head , externals.chain( name , pCH_out ) );
            } catch ( LL_Exception ) { }
            throw new ScopeException();
        }
        public IEnumerable<Ref> external_refs () => externals.LIFO_shadowed().Select( LL2Ref );
        public IEnumerable<Ref> refs() {
            var seen = new HashSet<string>();
            foreach ( var n in ownLL_head.LIFO() ) if ( ! seen.Contains( n.name )) {
                    seen.Add ( n.name ) ;
                    yield return LL2Ref ( n );
                }
            foreach ( var n in origLL_head.LIFO() ) if ( ! seen.Contains( n.name )) {
                    seen.Add ( n.name ) ;
                    yield return LL2Ref ( n );
                }
        }

        public CH_deltaScope instantiate() {           // resolves all preCHs , triggers their instantiation to proper CHs
            
            // using the decl interface is too headscratchy, becuase the original order between decls and refs (not within them) would have to be reconstructed ( self shadowing ) 
            LL<TypedCH> CHdelt_own = null;
            foreach ( var n in ownLL_head.LIFO().Reverse() )
                CHdelt_own = CHdelt_own.chain( n.name , n.pay.CH ) ;

            LL<TypedCH> CHdelt_externals = null ;
            foreach ( var n in externals.LIFO().Reverse() ) CHdelt_externals = CHdelt_externals.chain( n.name , n.pay.CH ) ;
            return new CH_deltaScope(origScope , CHdelt_own , CHdelt_externals ) ;

        }
    }

    public static partial class AUX {
        public static void AssertThrows<E> ( Action A )  where E : Exception { try { A(); } catch ( E ) { return; } D.Assert( false ) ; }

        // result seq has len = min ( len ( A ) , len ( B ) ) -- superfluous elements in either are silently dropped, as usual 
        public static IEnumerable<C> Zip<A,B,C> ( this IEnumerable<A> l_seq , IEnumerable<B> r_seq , Func<A,B,C> fun ) {
            var r_rator = r_seq.GetEnumerator();
            foreach ( var l_item in l_seq ) {
                if ( ! r_rator.MoveNext() ) yield break;
                yield return fun( l_item , r_rator.Current );
            }
        }
    }


    public class ScopingTests {

        public static void TestAll() {
            Test1_basic_LL_tests();
            Test2_basic_scoping();
            Test3_preCH_delta();
            Test4_deltasWithFunkyAdapters(); 
        }
            
        public static void Test1_basic_LL_tests() {
            LL<int> empty_int = null ;

            foreach ( var node in empty_int.chain("foo" , 3 ).chain("bar" , 4 ).LIFO()  ) Console.WriteLine ( node.name + " " + node.pay ) ;   // mainly to test chaining from null 
            // hmm thats neat :) 
            // also single assignment. empty int is still null 

            D.Assert ( empty_int.LIFO()         .ToArray().SequenceEqual( new LL<int>[0] ) );
            D.Assert ( empty_int.LIFO_shadowed().ToArray().SequenceEqual( new LL<int>[0] ) );
            AUX.AssertThrows< LL_Exception> ( () => empty_int.findPay("any key -- the LL is empty " ) );

            var zing = ((LL<string>)null).
                chain("v1" , "val1").
                chain("v2","boioioioioiing" ).
                chain("v1","shadowed");

            foreach ( var node in zing.LIFO_shadowed() ) Console.WriteLine ( node.name + " " + node.pay ) ;
            
            AUX.AssertThrows< LL_Exception> ( () => zing.findNode("non present key" ) );
            
            Console.WriteLine ( "================= LL ok ============= " ) ;
        }
        public static void Test2_basic_scoping () {
            var closA = new TypedSingleCH<int>() ;
            var closB = new TypedMultiCH<string>() ;  // types and kinds don't matter 

            var closedSC = new CH_closedScope()
                .decl("cA" , closA )
                .decl("cB" , closB );

            var D1 = new TypedSingleCH<int>() ;
            var D2 = new TypedMultiCH<string>() ;  // types and kinds don't matter 

            TypedCH dummy1 ;
            TypedCH dummy2 ;

            var deltaCH = new CH_deltaScope( closedSC )
                .decl("new name" , D1 )
                .addRef("cA" , out dummy1 )
                .addRef("new name" , out dummy2 );

            D.Assert( object.ReferenceEquals( dummy1 , closA ) );
            D.Assert( object.ReferenceEquals( dummy2 , D1 ) );

            D.Assert( 
                deltaCH.external_refs().Select( _ref => _ref.name ).
                SequenceEqual( new [] { "cA" } )                      // ref for "new name" is not external 
                ) ;

            // ---------------------------- 

            var deltaCH2 = new CH_deltaScope( closedSC )
                .decl("cA" , D1 )
                .decl("cA" , D1 )
                .decl("cA" , D1 )
                .decl("cB" , D1 )         // shadow both, multiple "cA"s are compressed to one in LIFO_shadowed
                .decl("cA" , D2 )         // shadow cA again with a different target 
                .addRef("cA", out dummy1 );

            var expected_names = new []        { "cA" , "cB" };  // in reverse of insertion order 
            var expected_chs   = new TypedCH[] {   D2 ,   D1 };  // for cA , cB resp.

            D.Assert ( deltaCH2.refs().Select( r => r.name).SequenceEqual( expected_names ) );
            D.Assert ( deltaCH2.refs().Select( r => r.CH  ).SequenceEqual( expected_chs ) );

            // ------------------------------------- 

           
            var closFunky = new TypedSingleCH<object>();


            var origSC = new CH_closedScope()
                .decl("cA" , closFunky)        // never see this guy 
                .decl("cA" , closA )
                .decl("cB" , closB );

            var sh1  = new TypedMultiCH<float>();
            TypedCH sh_dummy1 = null;
            TypedCH sh_dummy2 = null;

            var shadowDelta1 = new CH_deltaScope ( origSC )
                .decl("sh1" , sh1 )
                .addRef("cA" , out sh_dummy1 )  // external ref 
                .decl("cA"   , sh1 )            
                .addRef("cA" , out sh_dummy2 )   // now cA is both an external and internal ref
                ;
            // cB is not touched , thus : 
            // refs in order : [  "cA" -> sh_dummy2 == sh1 | "sh1" -> sh1 | "cB" -> closB ] 
            //                 ["cA" -> closA == sh_dummy1] is preserved in external_refs

            D.Assert( ReferenceEquals( sh_dummy1 , closA )  );
            D.Assert( ReferenceEquals( sh_dummy2 , sh1   )  );

            expected_names = new []        { "cA" , "sh1" , "cB" };  
            expected_chs   = new TypedCH[] {  sh1 ,  sh1  , closB }; 
                
            D.Assert ( shadowDelta1.refs().Select( r => r.name).SequenceEqual( expected_names ) );
            D.Assert ( shadowDelta1.refs().Select( r => r.CH  ).SequenceEqual( expected_chs   ) );

            D.Assert ( shadowDelta1.external_refs().Select( r => r.name).SequenceEqual( new        [] { "cA" } ) );
            D.Assert ( shadowDelta1.external_refs().Select( r => r.CH  ).SequenceEqual( new TypedCH[] { closA }  ) );

             // heheh :) two chaining deltaScopes need no extra testing   new DeltaScope ( fromDeltaScope ) is identical to its original in all fields - there is no point to this 

            Console.WriteLine ( "================= basicScoping ok ============= " ) ;

        }

        public static void Equivalent ( CH_closedScope cls , preCH_deltaScope deltaSC ) {
            int common_count = cls.refs().Count();
            var test = cls.refs().Zip( deltaSC.refs() , ( ref_L , ref_R ) => ReferenceEquals( ref_L.CH , ref_R.pre_ch.CH ) );
            D.Assert( test.All( _ => _) );             // all true 
            D.Assert( test.Count() == common_count );  // re-evals ... stress testing is never bad 
        }

        public static string doing ( SObject obj ) {    // unused ? 
            CH_Scope.Ref _ref;
            if ( obj is CH_Scope.Ref ) {
                _ref = (CH_Scope.Ref) obj;
                return new { n = _ref.name , t = _ref.CH.GetType().GetGenericArguments()[0].Name } .ToString();
            }
            return  "" ;
        }

        // to be able to write down expected result in a readable way 
        public static void AssertEquivalent ( CH_closedScope cls , string [] names , TypedCH [] CHs ) {
            D.Assert( names.Length == CHs.Length ) ;
            // cls.refs().NLSendRec("scope refs" , 1 , (_obj) => doing( (CH_Scope.Ref) _obj ) ); // git fucked this prototype
            var zipped_refs = names
                .Zip( CHs , ( n , ch ) => new CH_Scope.Ref { name = n , CH = ch } )
                /*.NLSendRec("zpipped" , 1 , doing )*/;


            var test = cls.refs().Zip( zipped_refs ,
                ( r_left , r_right ) => 
                    (r_left.name == r_right.name)                                       //.NLSend("name_equal")  
                    && ReferenceEquals ( r_left.CH , r_right.CH )                       //.NLSend("ref_equal")
                ).NLSendRec();
            D.Assert( names.Length == test.Count() ) ;
            D.Assert( test.All( _ => _ ) ) ;
        }

        // make it easier to see what's waht 
        public struct sC1 { } public struct sC2 { }  public struct sC3 { }
        public struct sD1 { } public struct sD2 { }  public struct sD3 { }

        public static void Test3_preCH_delta () {
            // basic 
            var C1 = new TypedSingleCH<sC1>();
            var C2 = new TypedSingleCH<sC2>();
            var C3 = new TypedSingleCH<sC3>();

            var clSC = new CH_closedScope()
                .decl("c1" ,C1 )
                .decl("c2" , C2 )
                .decl("c3" , C3 );

            var D1 = new TypedSingleCH<sD1>();
            var D2 = new TypedSingleCH<sD2>();
            var D3 = new TypedSingleCH<sD3>();

            var pD1 = new adapter_preCH ( D1 );
            var pD2 = new adapter_preCH ( D2 );
            var pD3 = new adapter_preCH ( D3 );

            var pdeltaSC = new preCH_deltaScope( clSC )
                .decl("c1", pD1)
                .decl("delta_2", pD2);

            AssertEquivalent ( pdeltaSC.instantiate().close(),
                new []        { "delta_2" , "c1" , "c3" , "c2" } ,
                new TypedCH[] { D2        ,  D1  ,  C3   , C2  } );

            // todo : MOAR!
            Console.WriteLine ( "================= preDeltaScoping ok ============= " ) ;
        }

        /*
             point of the deferred_adapter is that normal adapter_preCH ( CH ) can not be initialized like:
             adapter_preCH ( some_preCH_instance.CH )          
             without immediately triggering instantiation of the CH and the whole chain of events that might trigger 
             -- before the constructor even runs 
                ( as far as i'm aware, there is no syntax to assign an accessor backing function to a delegate --  X.prop is always transated to a call ) 
        */

        public static LL<preCH> checkClosureTripwires ( CH_deltaScope ch_dScope ) {
            var R = (LL<preCH>)null;
            foreach ( var _ref in ch_dScope.refs().Reverse() ) {
                R = R.chain ( _ref.name , new deferred_adapter_preCH( () =>  _ref.CH ) );  // yup proper _ref is captured here -- there were some subtle tripwires in c# closure building semantics, not unlike those of javascript, but i forgot which 
            }
            return R;
        }

        public static void Test4_deltasWithFunkyAdapters () {
            var D1 = new TypedSingleCH<sD1>();
            var D2 = new TypedSingleCH<sD2>();
            var D3 = new TypedSingleCH<sD3>();

            var CHdelta = new CH_deltaScope( new CH_closedScope()  )
                .decl("D1" , D1 )
                .decl("D2" , D2 )
                ;
            var LLchain = checkClosureTripwires( CHdelta );
            LLchain.LIFO()/*.NLSendRec("foo",1, obj => (obj as LL<preCH>).pay.CH )*/;

            D.Assert( LLchain.LIFO().Select( node => node.pay.CH ).SequenceEqual ( new TypedCH[] { D2 , D1 } )    ) ;
            Console.WriteLine ( "================= funky adapters ok ============= " ) ;
            
        }

    }


}
