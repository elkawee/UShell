using System;
using C5;
using SCG = System.Collections.Generic;
using System.Linq;
using E = System.Linq.Enumerable;

using D = System.Diagnostics.Debug;
using System.Reflection;
using System.Text.RegularExpressions;


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
	}
    // since inner nodes can have a payload too - this distinction is not strictly neccessary
    // but it adds structure that is nice to sanity check against, so leave it for now 
	public class SuggestionLeaf<Pay> : SuggestionTree<Pay>  where Pay : class {
		public SuggestionLeaf(Pay payload){ this.payload = payload ;}
	}


    public static class SGs {
        public static string [] usings = new [] { "UnityEngine" , "System"} ;
        static SuggestionTree<System.Type> _TypeSG = null;
        public static SuggestionTree<System.Type> TypeSG { get { if ( _TypeSG == null ) _TypeSG =  BuildTypeSG(usings); return _TypeSG; } }
        static Assembly [] _AllAssemblies = null;

        public static Assembly [] AllAssemblies { get { // delay initialization - can't be at Assembly load time
                if ( _AllAssemblies == null ) _AllAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies().
                    Select ( ass_name => 
                        Assembly.Load(ass_name)
                    ).Concat( new [] { Assembly.GetExecutingAssembly()/*.NLSend("executing Assembly [____] ")*/ } ) .ToArray();
                return _AllAssemblies ; } }
        // ARGGGG, figgn "ExecutingAssembly" ist mit nichten die Binary, die als Einsprungspunkt fuer den Loader genommen wurde, sondern i-was anderes 

        public static SuggestionTree<Type> BuildTypeSG(SCG.IEnumerable<string > usingsE ) { return BuildTypeSG( usingsE , AllAssemblies ) ; }

        public static SuggestionTree<Type> BuildTypeSG(SCG.IEnumerable<string > usingsE , SCG.IEnumerable<Assembly> Assemblies)
		{
            #if !fakeSuggTree
            var V = new UnityEngine.Vector3();  // otherwise UnityEngine does not turn up in GetReferencedAssemblies() , prob. only on OSX 
            #endif

            Func< Assembly, string, bool> name_match = ( ass , name ) => {
                var n  =  ass.GetName().Name; // <- TODO dunno how safe this is 
                //Console.WriteLine( n ) ; 
                return n == name ; };


            // Come to think of it - this way of doing "using"s is complete nonsense
            // it only mirrors the behaviour of a using declaration for An Assembly that declares a namespace name equal to its Assembly name 
            // _and_ has all its types declared within that 
            // _and_ no other assembly declares stuff in that namespace 
            // ... luckily this is quite common >_< 

            var regular_Assemblies = Assemblies.Where ( ass => ! usingsE.Any( us => name_match(ass , us ) )  ) ;
            var using_Assemblies   = Assemblies.Where ( ass => usingsE.Any( us => name_match(ass , us ) ) );

            var RES = new SuggestionTree<Type>();

            const string name_split_pattern = @"\.|\+" ; // <- prob not enough

			foreach (var Ass in regular_Assemblies.Concat ( using_Assemblies ) )  // want both, such that a full name request still shows up 
			{
				Console.Write("fetching types from : " + Ass.GetName() + " ");
				SCG.IEnumerable<Type> ts;

				try
				{
					ts = Ass.GetTypes();
				}
				catch (System.Exception e)
				{
					Console.WriteLine("\n\n  !!! skipping Assembly !!! (" + e.Message+ ")");
					continue;
				}
				foreach (var t in ts) RES.Add( Regex.Split(t.FullName, name_split_pattern) , t );
				Console.WriteLine("\n-- ok (regular import)");
			}
            foreach (var Ass in using_Assemblies ) // <- do the thing twice , due to how SuggTree<>.Add works this overwrites names with the "using" ones should they collide  
			{
				Console.Write("fetching types from : " + Ass.GetName() + " ");
				SCG.IEnumerable<Type> ts;

				try
				{
					ts = Ass.GetTypes();
				}
				catch (System.Exception e)
				{
					Console.WriteLine("\n\n !!! skipping Assembly !!! (" + e.Message + ")");
					continue;
				}
                
				foreach (var t in ts) {
                    var parts = Regex.Split(t.FullName, name_split_pattern).Skip(1); // <- TODO strip "using'd" part properly
                    if ( parts.Count() > 0 ) {
                        RES.Add( parts , t );  
                    } else {
                        Console.WriteLine( "skipped type :" + t.FullName +  "(" + t + ")" );
                    }
                }
				Console.WriteLine("\n-- ok (using import)");
			}
            return RES;

		}

        const BindingFlags BI_inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance; // <- static hier ?

        public static SuggestionTree<Type> BuildMemTree( Type T , Func<Type,SCG.IEnumerable<MemberInfo>> edges ) {
            var R = new SuggestionTree<Type>();
            foreach ( var memI in edges(T) ) {
                Type TargetT ; 
                if      ( memI is FieldInfo    ) TargetT = ((FieldInfo   ) memI ).FieldType;
                else if ( memI is PropertyInfo ) TargetT = ((PropertyInfo) memI ).PropertyType;
                else throw new NotImplementedException();
                R.Add(new [] { memI.Name } , TargetT );
            }
            return R;
        }

        public static SuggestionTree<Type> BuildFieldTree( Type T ) {
            return BuildMemTree( T , T_ => T_.GetFields(BI_inst) );
        }
        public static SuggestionTree<Type> BuildPropTree( Type T ) {
            return BuildMemTree( T , T_ => T_.GetProperties(BI_inst) );
        }

    }


}
