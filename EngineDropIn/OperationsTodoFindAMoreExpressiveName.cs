using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ParserComb;
using System.Reflection;

using SGA = SuggestionTree.SuggTAdapter;
using MG = MainGrammar.MainGrammar;
using D = System.Diagnostics.Debug;
// SuggTree decoupling types 
using MembK = SuggestionTree.MembK;
using MembK_E = SuggestionTree.MembK_E;

using NLSPlain;
using System.IO;

namespace MainGrammar { // todo maybe own namespace 

    /*
    return types decoupled from everything network  
    */

    public class Operations {

        public struct mi_sugg {
            public string str_op ; 
            public MemberInfo mi ;
        }

        public struct OpAC_res {
            public string      prefix         ; 
            public PTokBase [] nu_toks        ;
            public bool        AC_happend     ; 
            public bool        isMemberAC     ;
            public mi_sugg []  memberSuggs    ;
            public string []   typeSuggs      ; 
            public int         nu_offs        ;
        } 
        static OpAC_res MK_noAC_happend ( IEnumerable<PTokBase> origTokens , uint offset ) {
            return  new OpAC_res {
                prefix  = "",
                nu_offs = (int)offset ,
                nu_toks = origTokens.ToArray(),
                AC_happend = false,
                isMemberAC = false,
                memberSuggs = new mi_sugg[0],
                typeSuggs   = new string[0]
            };
        }

        
        public static MembK MembKF_from_refineTok ( PTok ptok ) {
            if ( ptok == null ) return MembK.Any();
            switch ( ptok.E ) { 
                case PTokE.OP_dot          : return MembK.Val() ;
                case PTokE.OP_star         : return MembK.Ref() ;
                case PTokE.OP_percent      : return MembK.Prop() ;
                case PTokE.OP_special_prop : return MembK.Special() ;
                default : throw new NotImplementedException();
            }
        }
        public static PTok refineTok_from_MembK ( MembK membK ) { 
            switch ( membK.E ) {      // only the subset of MembK_E that designates concrete "kinds" - no filter members - this parallels the MembK constructor for MemberInfo , as it must 
                                                                                                       // payloads are needed for stringification, as well as for proper stringPos calculation in TokLine 
                case MembK_E._val     : return new PTok { E = PTokE.OP_dot            , pay = "."   } ;
                case MembK_E._ref     : return new PTok { E = PTokE.OP_star           , pay = "*"   } ;
                case MembK_E._prop    : return new PTok { E = PTokE.OP_percent        , pay = "%"   } ;
                case MembK_E._special : return new PTok { E = PTokE.OP_special_prop   , pay = "%!"  } ;
                default : throw new NotImplementedException();
            }
        }
#if true   
        
        public static OpAC_res MembAcc_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC pos , MG.MemANodeRX MA_node , OpAC_res noAC_happend ) {
            
            #region setup 
            
            string orig_prefix = MA_node.name ; // <- .name is normalized to "" , never null 
            MembK orig_MembK   = MembKF_from_refineTok ( MA_node.refineOPTok ) ; // resolves to "_any" for null argument 


            MemberInfo[] mis  = new MemberInfo[0];
            Type     baseType = null ;
            try { 
                // i *THINK* ACMembTypingCallback is supposed to be set at creation time of the corresponding TranslationUnit ( i.e. its constructor)  - this is not happening atm 
                // fetch the type here to base SuggTreeFetching of MethodInfos on, the MembKFilter argument to that thing is something else entirely
                baseType = (MA_node as MG.ACableMemb).ACMembTypingCallback();
            } catch ( MG.NoACPossible ) {   // <- questionable if this exception is needed at all 
                return noAC_happend;
            } 
            if ( baseType == null  ) return noAC_happend;

            try { 
                mis = SGA.MembAC( baseType , orig_prefix , orig_MembK );
            } catch ( Exception ) {
                return noAC_happend;
            }
            if ( mis.Length == 0 ) return noAC_happend;

            #endregion 

            string new_prefix  = SGA.LongestCommonPrefix ( mis.Select(mi => mi.Name).ToArray()) ;

            bool insert_refinement = (MA_node.refineOPTok == null ) && ( mis.Length == 1 );                 // current strat : only augment refinement op if suggestions are unique 
            bool insert_name       = (MA_node.nameTok     == null )  /* && ( new_prefix.Length > 0 ) */ ;   // actually ... it's easier to always insert 

            // --- if both refinement, and name are to be inserted: insert refinement first

            if ( insert_refinement ) {
                orig_TL.InsertTokAfterNode ( 
                    orig_TL.findTok(  MA_node.initialOpTok ) , 
                    refineTok_from_MembK( new MembK( mis[0] ))   // implicitly assuming mis[] to be of length 1, or their MembKs to be identical in case of several 
                    );
            }

            if ( insert_name ) {
                var new_nameTok = new PTok { E = PTokE.CS_name , pay = new_prefix }; 
                pos             = orig_TL.InsertAfterCPos( pos , new_nameTok );

            } else { 
                // a nice invariant to fuzz against :  for all x ::   SG.MembAcc(x). map ( -> name ). LongestCommonPrefix().Length >= x.Length  ( this is implitily assumed here ) 
                // counter case : len ( longestCommonPrefix ) < len( MA_node.name ) should never reach this ( cuz mis == [] triggers return further up ) 
                D.Assert ( new_prefix.Length >= orig_prefix.Length );
                MA_node.nameTok.pay = new_prefix;
                pos                 = orig_TL.CPosAtEndOfTok( MA_node.nameTok );

            }
            
            return new OpAC_res {
                AC_happend = true ,
                prefix = new_prefix ,
                nu_toks = orig_TL.Serialize<PTokBase> ( onTok: t => t ,
                                                        onWS : ui => new PTokWhitespace { len = ui } ).ToArray(),
                isMemberAC = true,
                memberSuggs = mis.Select( mi => new mi_sugg { str_op =  "foo "/*PTokE_to_string(    new SuggestionTree.MembK(mi).OpE()  ) */ ,      // TODO 
                                                              mi     = mi                                      } ).ToArray() , 
                nu_offs     = pos.ci_post // <- wrong , but round trip works 

            };

        }
#endif



        public static OpAC_res Type_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC CPos , NamedNode n ,  OpAC_res noAC_happend ) {
            return noAC_happend;
        }

        /* 
            functor to decouple the intricacies of translation and scoping 
            to be moved elswhere later 

            passing in an already generated AST does not make sense because this only this part is supposed to know how to react to errors ( how pass them on to the interactive shell and so forth ) 
        */ 
        public static Func<string,NamedNode> MK_GetAst (GrammarEntry GE , TranslateLHS TR_lhs) {
            
            return ( str_in ) => { 
                var Stripped = TranslateEntry.LexxAndStripWS( str_in );
                /*
                    todo : this version of scope throws on incomplete parse - should be allowed for interactive 

                    also : how to deal with cursor pos beyond the end of an incomplete parse ? ( analog problem to synced walking for Colorize() in the console ) 
                    */
                return TranslateEntry.Scope( Stripped , GE , TR_lhs);
            };
        }

#if true 
        public static OpAC_res AC ( string str_in , int cursor_pos , Func<string,NamedNode> GetAst ) {
            
            var l_toks = Lexer.Tokenize( str_in ) ;

            var TokL = new TokLinePlanB<PTok>();
            foreach ( PTokBase tb in l_toks ) {
                if ( tb is PTok ) {

                    TokL.AddTok( (PTok) tb ); 
                } else {
                    TokL.AddWS( (tb as PTokWhitespace).len ) ;
                }
            }
            var noAC_happend = MK_noAC_happend ( l_toks , (uint) cursor_pos );

            var CPos = TokL.CPosFromStringpos( (int)cursor_pos ) ;
            
            PTok AC_Tok = CPos.AC_tok();

            if ( AC_Tok == null ) { return noAC_happend; }
#if null
            bool rest;
            var AST = MG.RunRX ( StrippedA , EVTest.TestMG1.TestStart /* <- fixme , temporary hack  */  ,  out rest ); 
            if ( rest ) return noAC_happend ;                     // RX Grammar could not parse 
#endif 
            NamedNode AST = GetAst( str_in ) ;
            if ( AST == null ) return noAC_happend ;
            
            NamedNode AC_Node = null;
            try { 
                AC_Node = AST.Leafs().Where( nn => (nn as MG.TermNode).tok == AC_Tok ).First();
            } catch ( InvalidOperationException ) { // First() on empty sequence 
                D.Assert( false ) ;                 // BUG. And a fat one. meaning a token present in input could not be found in any of the leafs --- incomplete token consumption , or the parser internal trees structures are fucked 
            }

            NamedNode descrNode = AC_Node.PathUpTo( (n) => ( n is MG.ACable ) ) ;
            if ( descrNode == null ) return noAC_happend;
            
            if      ( descrNode is MG.ACableMemb )     return MembAcc_AC( TokL , CPos , descrNode as MG.MemANodeRX ,noAC_happend ); // <- todo cast not typesafe 
            else if ( descrNode is MG.ACableTypeNmae ) return Type_AC ( TokL, CPos , descrNode , noAC_happend);
            else throw new NotImplementedException(); 
            

        }
#endif

            #region aux 
            // todo: i want to move all prefix calulations here 
        public static string LongestCommonPrefixType ( IEnumerable<string []> in_setS ) {
            if ( !in_setS.Any()              ) throw new Exception() ;
            if ( in_setS.First().Length == 0 ) throw new Exception();
            int prefix_indx = in_setS.First().Length -1 ; 
            return LongestCommonPrefix( in_setS.Select( strings => strings[prefix_indx]).ToArray() ); // potentially throws index_out_of_range - this is a contract violation with SGA ( all string seqs are supposed to have the same length ) 
        }
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
