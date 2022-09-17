using System;
using C5;
using SCG = System.Collections.Generic;
using System.Linq;
using E = System.Linq.Enumerable;

using D = System.Diagnostics.Debug;
using System.Reflection;
using System.Text.RegularExpressions;
using NLSPlain;


#if UnityEngineMock
using UnityEngine = UnityEngine;
using GameObject = UnityEngine.GameObject;
#else
using UnityEngine;
#endif 




#pragma warning disable 649, 169, 168, 219 // declared but unused, assigned but not used .... 

namespace SuggestionTree
{
	public class SuggestionTree<Pay> where Pay : class // want payload to be nullable 
	{
		
        public class Bug : System.Exception { public int no ; public Bug ( int _no ) { no = _no ; } }
        public class InterfaceE   : System.Exception { }  ;
         
        
		public TreeDictionary<string,SuggestionTree<Pay>> D = new TreeDictionary<string, SuggestionTree<Pay>>();
        public Pay payload = null ;

        /*
            this whole thing is meant for fully qualified type names 
            since c# has inner types, both A and A.B could be a valid target for a completion with payload 
            -> an optional payload on inner nodes must be supported
            -> leafs must be non-empty 
        */
		public bool Add( SCG.IEnumerable<string> edges , Pay payload  ) {
            // TODO: exception on null payload, want null to be used internally and get away with not doing the maybe<T> dance 
			if ( !edges.Any()) throw new InterfaceE();
            if ( edges.Any( e => string.IsNullOrEmpty(e) /*|| string.IsNullOrWhiteSpace(e)*/ ) ) throw new InterfaceE(); // <- empty strings as keys destroy pretty much all assumptions 
            //                               TODO !!!                    ^._____ not available in Unity
			string first = edges.First();
			var Rest = edges.Skip(1);

			if ( !Rest.Any()) return AddLeaf( first , payload);

			if ( !D.Contains(first) ) D[first] = new SuggestionTree<Pay>();
            // D[first] is guaranteed to exist at this point, but it might be a leaf -> convert to inner node 
            // TODO this is prob. still not enough, depending on how this is supposed to behave upon input in which a path is duplicate
            //      for example when simulating "using directives", overrriding paths and even replacing whole subtrees must be supported 
            // ( das braucht wahrscheinlich einen Join( SuggTree1 , SuggTree2 ) siehe lustig bunte A4 blaetter ) 
            if ( D[first] is SuggestionLeaf<Pay> ) {
                var nu_tree = new SuggestionTree<Pay>() ; nu_tree.payload = D[first].payload ; D[first] = nu_tree ;
            } 
			return D[first].Add(Rest,payload);
		}
		public bool AddLeaf( string edge , Pay payload ) {
			bool present = D.Contains(edge);
			D[edge] = new SuggestionLeaf<Pay>( payload );  // allways overwrite - and report back in case of collision  
			return !present;
		}


        public void PullDownNamespaces(   SCG.IEnumerable<SCG.IEnumerable<string>> Namespaces ) {
            // collect everything into a non lazy structure first 
            var insertions = new SCG.List<  SuggestionTree<Pay>.IntermFResult.single_suggestion >() ; 

            foreach ( var ns in Namespaces ) {
                var namespace_node_res = FindSequence( ns.ToArray(), last_query_is_exact: true );
                if ( namespace_node_res.type == SuggestionTree<Pay>.FRType.unique_fit ) {

                    SuggestionTree<Pay> namespaceNode = namespace_node_res.suggs[0].val;
                    foreach ( var single_sugg  in namespaceNode.FindAllWithPayload() ) insertions.Add( single_sugg );

                } else {
                    string.Join(".", ns.ToArray()) .NLSend("skipping pulldown for ");  // todo undo this, this does not paper over a bug in the suggtree, but a case in which "UnityEditor" actually wasn't present at all in the type tree (from mixing mocking and actual build, makefile cleanup soon(tm) ) 
                    //throw new Exception( "no exact match for Namespace-pulling : " + string.Join(".", ns.ToArray() ) ); // <- turn this into consumer catchable Exception as soon as user defined "usings" are a thing 
                }
            }
            //  second iteration to not intertwine access and modifying - and avoid reasoning headaches 
            foreach ( var single_sugg in insertions ) {
                Add ( single_sugg.steps , single_sugg.val.payload );  // Add overrides the payload 
            }
        }



        // originally result type for the public API was planned to be named FResult - but i can't remember what the F stood for >_< 
	
		public enum FRType {
            // this roughly matches the situations that switch between behaviours of an auto-completion
            unique_fit ,      // <- do the completion
            ambigous_fit ,    // <- print a list of possible completions
            empty }           // <- can't really do anything at this point
		public struct IntermFResult {
			public struct single_suggestion { 
				public string [] steps ; 
				public  SuggestionTree<Pay> val ;
                public override string ToString() {
                    return new {
                        res_type = val is SuggestionLeaf<Pay> ? "leaf" : "node" ,
                        steps    = string.Join( "->" , steps )+"\t",
                        pay      = val.payload
                    }.ToString();
                }
			} 
			public FRType type;
			public single_suggestion [] suggs;
            public override String ToString() {
                return new { type = type , sugg = suggs == null ? "__empty__" :  "\n\t" + string.Join("\n\t" , suggs.Select(x=>x.ToString()).ToArray() )  }.ToString();
            }
		}
		public IntermFResult FindSingle ( string query_elem , bool exact_query = false ) {
            /*
                There is ambiguity in the notion of whether a query against the current node is ambigous or not :/ 

                namespaces of the form Foo , FooBar are legal -> a set of keys where k1 is prefix of k2 is legal 
                - this set queried for "Foo" is ambigous if it is the tail end of a query
                - it is unique or empty , with a solution of ("Foo" , subtree_of ( "Foo" )) if it has successor elements in the query ( e.g. [ "Foo" , "T1" ] ) 
            */
			var RgFrom = D.RangeFrom(query_elem);
			var RgFil  = exact_query ? 
                RgFrom.Where( kv => kv.Key == query_elem ) :
                RgFrom.Where( kv => kv.Key.StartsWith(query_elem ));

			var Res    = new IntermFResult();
			
			if( !RgFil.Any()) {
                Res.type = FRType.empty;
                Res.suggs = new IntermFResult.single_suggestion[0];
                return Res; } 
			else {
				Res.suggs = RgFil.Select( kv => { 
					var ss   = new IntermFResult.single_suggestion(); 
					ss.steps = new [] { kv.Key } ;
					ss.val   = kv.Value;
					return ss;}).ToArray();
				if( Res.suggs.Count() > 1 ) Res.type = FRType.ambigous_fit; else Res.type = FRType.unique_fit;
				return Res;
			}
		}
        public IntermFResult FindSequence ( string [] query , bool last_query_is_exact = false) {
            if ( query.Length == 0 ) throw new InterfaceE();          // <- form of an empty query is [""]  , anythning special to do with ["" , "" , ... ] ???
            if ( query.Length == 1 ) return FindSingle ( query[0] , exact_query: last_query_is_exact );  // this stuff is kind of confusing. see : T4_exact_queries()

            // --- 
            var sub_res_here = FindSingle( query[0] , exact_query: true ) ;
            if ( sub_res_here.type == FRType.ambigous_fit || sub_res_here.type == FRType.empty ) return sub_res_here ;
            // -- 
            // at this point sub_res_here is unique -> exactly one suggestion with steplength >= 1 
            if ( ! ( sub_res_here.suggs.Length == 1 && sub_res_here.suggs[0].steps.Length >= 1 ) ) throw new Bug (2) ; 

            var sub_res_down = sub_res_here.suggs[0].val.FindSequence( query.Skip(1).ToArray() , last_query_is_exact ) ;
            var own_res = sub_res_down ; // assuming shallow copy for structs 

            foreach ( var i  in E.Range (  0 , own_res.suggs.Length ) ) { // prepend own step to results at recur up 
                own_res.suggs[i].steps = new [] { sub_res_here.suggs[0].steps[0] }.Concat ( sub_res_down.suggs[i].steps ).ToArray();
            }
            return own_res;
        }

        SCG.IEnumerable<IntermFResult.single_suggestion> rec_down ( SCG.IEnumerable<string>  pref) {
            if ( payload != null ) yield return new IntermFResult.single_suggestion{ steps = pref.ToArray() , val = this } ; 
            foreach ( var kv in D.RangeAll() ) {
                string                name_edge = kv.Key;
                SuggestionTree<Pay>   subtree   = kv.Value;
                foreach ( var sub_res in subtree.rec_down( pref.Concat( new [] { name_edge } ).ToArray() ) ) {
                    yield return sub_res;
                }
            }
        }
        public SCG.IEnumerable< IntermFResult.single_suggestion>  FindAllWithPayload () {    
            return rec_down( new string[0] ).ToArray();
        }
	}
    // since inner nodes can have a payload too - this distinction is not strictly neccessary
    // but it adds structure that is nice to sanity check against, so leave it for now 
	public class SuggestionLeaf<Pay> : SuggestionTree<Pay>  where Pay : class {
		public SuggestionLeaf(Pay payload){ this.payload = payload ;}
	}

    
}
