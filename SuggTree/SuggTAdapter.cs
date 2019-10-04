


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SuggestionTree;

#if !fakeSuggTree

using UnityEngine ;
#endif 


#pragma warning disable CS0436
// temporary - this is probably because ShellCommonDummyProject references ParseComb and thus has two definitions of MembK
// what i don't get is why there is no complaint about SuggTreeAdapter which has the same problem 
// because one is a struct ? 

namespace SuggestionTree {


    /*
        this MembK stuff is the only thing that makes SuggTree depend on CoreGrammar 
        ... but i don't know where else to put it  
    */
   
    [Flags]
    public enum MembK_E {
        _val = 1 , _ref  = 2 ,_prop = 4 , _special = 8 
    }
    public struct MembK {
        static HashSet<MemberInfo> SpecialProps = new HashSet<MemberInfo>();
        static MembK () {
            BindingFlags bi = BindingFlags.Public | BindingFlags.Instance ;
#if !fakeSuggTree
            SpecialProps.Add( typeof(MeshRenderer).GetProperty("material",bi) ) ;
#endif
        }
        public MembK_E E ;
        public MembK ( MemberInfo mi ) {
            if ( mi is PropertyInfo ) {
                if ( SpecialProps.Contains( mi ) ) E = MembK_E._special;
                else                               E = MembK_E._prop;
            } else if ( mi is FieldInfo ) {
                var T = ( mi as FieldInfo ).FieldType;
                E = T.IsValueType ? MembK_E._val : MembK_E._ref ;
            } else {
                throw new ArgumentException () ;
            }
        }
        public MainGrammar.PTokE OpE () {
            switch ( E ) {
                case MembK_E._val: return MainGrammar.PTokE.OP_dot;
                case MembK_E._ref: return MainGrammar.PTokE.OP_star;
                case MembK_E._prop: return MainGrammar.PTokE.OP_percent;
                case MembK_E._special: return MainGrammar.PTokE.OP_special_prop;
                default: throw new ArgumentException();
            }
        }
    }

    public struct MembK_Filter {
        public static MembK_Filter Any = new MembK_Filter { E = MembK_E._val | MembK_E._ref | MembK_E._prop | MembK_E._special };
        public MembK_E E ;
        public MembK_Filter( MainGrammar.PTokE PE ) {
            switch ( PE ) {
                case MainGrammar.PTokE.OP_dot:          E = MembK_E._val | MembK_E._ref | MembK_E._prop | MembK_E._special ; break;
                case MainGrammar.PTokE.OP_star:         E = MembK_E._ref ;                                                   break;
                case MainGrammar.PTokE.OP_percent:      E = MembK_E._prop | MembK_E._special;                                break;
                case MainGrammar.PTokE.OP_special_prop: E = MembK_E._special;                                                break;
                default: throw new ArgumentException();
            }
        }
        public bool matches ( MembK other ) { return (E & other.E) != 0;  }
    }


    public static class SuggTAdapter {
        

        public class TypingException : Exception { };

        public struct typeAC_alt { public Type T ; public string [] steps ;}
        
#if !fakeSuggTree 
        static string [] usings = new [] { "UnityEngine" , "System"} ;       
        static SuggestionTree<System.Type> _TypeSG = null;
        public static SuggestionTree<System.Type> TypeSG { get { if ( _TypeSG == null ) _TypeSG =  SGs.BuildTypeSG(usings); return _TypeSG; } }
        const BindingFlags BI_inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance; // <- static hier ?

        

        public static MemberInfo[] MembAC ( Type T , string arg  ) { return MembAC( T, arg , MembK_Filter.Any ); }
        public static MemberInfo[] MembAC ( Type T , string arg ,  MembK_Filter filter ) {
            return MembAC_Impl(T,arg,filter);
        }

        public static Type MembType_Exact  ( Type T , string arg   ) { return MembType_Exact( T , arg , MembK_Filter.Any ); }
        public static Type MembType_Exact  ( Type T , string arg , MembK_Filter kind_filter  ) {
            return MembType_Exact_Impl( T , arg , kind_filter );
        }

        public static typeAC_alt[] QTN_AC ( string [] args , out string prefix  ) {
            return QTN_AC_Impl( args , out prefix );
        }
        public static Type QTN_Exact ( string [] args  ) {
            return QTN_Exact_Impl( args ) ;
        }
        public static Type SGDefaultType { get {
                return typeof ( UnityEngine.GameObject);
         } }
#else
        
        public static MemberInfo[] MembAC ( Type T , string arg ,  MembK_Filter filter ) {

            throw new NotImplementedException();

        }
        public static Type MembType_Exact  ( Type T , string arg , MembK_Filter mk_filter  ) {

            throw new NotImplementedException();

        }
        public static typeAC_alt[] QTN_AC ( string [] args , out string prefix  ) {

            throw new NotImplementedException();

        }
        public static Type QTN_Exact ( string [] args  ) {

            throw new NotImplementedException();

        }
        public static Type SGDefaultType { get {
           throw new NotImplementedException();
        }}
#endif





#if !fakeSuggTree

        static SuggestionTree<MemberInfo> BuildMembTree( Type T  ) {
            var R = new SuggestionTree<MemberInfo>();
            foreach ( var memI in T.GetFields( BI_inst ) ) {
                R.Add(new [] { memI.Name } , memI );
            }
            foreach ( var memI in T.GetProperties( BI_inst ) ) {
                R.Add(new [] { memI.Name } , memI );
            }
            return R;
        }
        


        static Dictionary<Type, SuggestionTree<MemberInfo>> Fields = new Dictionary<Type, SuggestionTree<MemberInfo>>();
        static SuggestionTree<MemberInfo> GetMembSG ( Type t ) {
            // assuming it's impossible to have an instance of System.Type that the Field tree can not be populated with 
            if ( Fields.ContainsKey ( t ) ) return Fields[t];
            var nuSG = BuildMembTree(t );
            Fields[t] = nuSG;
            return nuSG;
        }
      

        
        static Type QTN_Exact_Impl ( string [] args  ) {
            
            var res = TypeSG.FindSequence(args, last_query_is_exact:true );
            if ( res.type != SuggestionTree<Type>.FRType.unique_fit ) throw new TypingException();
            return res.suggs[0].val.payload;
            
        }

        static Type MembType_Exact_Impl  ( Type T , string arg , MembK_Filter mk_filter  ) {
            
            var MembSugg = GetMembSG ( T) ; 
            var res     = MembSugg.FindSingle( arg , exact_query:true );
            if ( res.type != SuggestionTree<MemberInfo>.FRType.unique_fit ) throw new TypingException();

            MemberInfo mi = res.suggs[0].val.payload; 
            MembK kind = new MembK(mi);
            if ( ! mk_filter.matches( kind )) throw new TypingException();
            
            if ( mi is FieldInfo )    return (mi as FieldInfo).FieldType;
            if ( mi is PropertyInfo ) return (mi as PropertyInfo).PropertyType; 
            throw new ArgumentException();
            
        }

        

        static typeAC_alt[] QTN_AC_Impl ( string [] args , out string prefix  ) {
            
            if ( args.Length == 0 ) throw new ArgumentException("empty argument is to be represented as [\"\"]");
            var res = TypeSG.FindSequence( args , last_query_is_exact:false ) ;
            if ( res.type == SuggestionTree<Type>.FRType.empty ) { prefix = "" ; return new typeAC_alt[0] ; }
            var alts = res.suggs.Select ( sugg => new typeAC_alt { T = sugg.val.payload , steps = sugg.steps } ).ToArray() ;
            int pref_idx = args.Length -1 ; 
            prefix = LongestCommonPrefix( alts.Select( sugg => sugg.steps[pref_idx]).ToArray() );                             /* assumes SuggTree invariant : FindSeq( seq_in ) -> [ steps ] :: len(steps) = len(seq_in) for all steps  */
            return alts ;
           
        }


        static MemberInfo[] MembAC_Impl ( Type T , string arg ,  MembK_Filter filter ) {
            
            var SG  = GetMembSG( T ) ;
            var res = SG.FindSingle( arg , exact_query:false ) ;
            MemberInfo[] R =  res.suggs.Select ( sugg => sugg.val.payload ).Where( (MemberInfo mi) => filter.matches(new MembK( mi ))  ).ToArray();
            return R;
        }

#endif



#region aux 
        // todo remove prefix stuff from SuggTreeAdapter 
        public static string LongestCommonPrefix ( string [] in_set ) {
            // dumb n real slo
            string pref = "";
            string cand_pref = "";
            if ( ! in_set.Any() ) return pref ; 
            int maxL = in_set.Select ( str => str.Length ).Min();
            for ( int i = 0 ; i < maxL ; i ++ ) {
                cand_pref += in_set[0][i];
                if ( ! in_set.All ( str => str.StartsWith( cand_pref ))) return pref;
                pref = cand_pref;
            }
            return pref;
        }
#endregion

    }
}
