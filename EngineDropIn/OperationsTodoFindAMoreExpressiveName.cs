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


        // depends on OLD grammar 

#if true   
        /*
            this whole thing nees fixing as soon as the optional Operator prefixes are introduced 
        */
        public static OpAC_res MembAcc_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC pos , NamedNode MA_node , OpAC_res noAC_happend ) {
            
            MemberInfo[] mis = new MemberInfo[0];
            try { 
                mis = (MA_node as MG.ACableMemb).ACMembTypingCallback();
            } catch ( MG.NoACPossible ) { } // simply leave mis == [] in this case 
             
            // mis is pre-filtered for MembKind with a filter derived from the parsed operator ( TODO: check if still true ;) ) 
            if ( mis.Length == 0 ) return noAC_happend;
            string prefix  = SGA.LongestCommonPrefix ( mis.Select(mi => mi.Name).ToArray()) ;


            Func<PTokE,string> PTokE_to_string = ( PE ) => {
                switch ( PE ) {
                    case PTokE.OP_dot:          return "." ;
                    case PTokE.OP_percent:      return "%" ;
                    case PTokE.OP_special_prop: return "%!";
                    case PTokE.OP_star:         return "*" ;
                    default: throw new ArgumentException ();
                }
            };

            #region common_kind 
            bool have_common_kind = true;
            var common_kind =  new SuggestionTree.MembK( mis[0] ) ;

            // assume member compare Equals for struct 
            foreach ( var mi in mis.Skip(1) ) if ( ! new SuggestionTree.MembK( mi ).Equals( common_kind ) ) { have_common_kind = false ; break ;}

            SuggestionTree.MembK res_kind = have_common_kind ? common_kind : new SuggestionTree.MembK { E = SuggestionTree.MembK_E._val } ;
            #endregion 
            
            var nu_op    = new PTok { E = res_kind.OpE() , pay = PTokE_to_string( res_kind.OpE() ) };
            var nu_name  = new PTok { E = PTokE.CS_name  , pay = prefix };

            // todo : Whitespace insert 
            /*
            orig_TL.ReplaceTok( (MA_node.children[0] as MG.TermNode ).tok, nu_op );
            uint nu_offs = noAC_happend.nu_offs;
            if ( MA_node is MG.MemANodeRX ) { 
                orig_TL.ReplaceTok( (MA_node.children[2] as MG.TermNode ).tok, nu_name );
                nu_offs = orig_TL.CPosAtEndOfTok( nu_name ).StringPos();
            }
            */
            return new OpAC_res {
                AC_happend = true ,
                prefix = prefix ,
                nu_toks = orig_TL.Serialize<PTokBase> ( onTok: t => t ,
                                                        onWS : ui => new PTokWhitespace { len = ui } ).ToArray(),
                isMemberAC = true,
                memberSuggs = mis.Select( mi => new mi_sugg { str_op = PTokE_to_string( new SuggestionTree.MembK(mi).OpE() ) ,
                                                              mi     = mi                                      } ).ToArray() , 
                nu_offs     = pos.ci_post // <- wrong , but round trip works 

            };

        }
#endif

#if false

        public static OpAC_res Type_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC CPos , NamedNode n ,  OpAC_res noAC_happend ) {
            OG.ACableType AC_node = (OG.ACableType) n;
            SGA.typeAC_alt [] sga_alts;
            try { 
                sga_alts = AC_node.AC();
            } catch ( OG.NoACPossible ) { return noAC_happend; }
            if ( sga_alts.Length == 0 ) { return noAC_happend; }

            /*
                   TypefiterRX 
                    |    |      \ 
                    [:]  [name]   QTN_Component  * 
                                   |      \
                                   [:]    [name] 
                    ACable node is either TypefilterRX or QTN_Component 
                    in both cases the cursor is either on whitespace ( because a name token is not present ) or on the [name] token 

                    for any of the QTN_Component * the length of SGA.typAC_alt elements tells which node it was 
            */
            string prefix = LongestCommonPrefixType ( sga_alts.Select( alt => alt.steps ).ToArray() ) ; 
            
            var RES = new OpAC_res {
                    prefix  = "",
                    nu_toks = new PTokBase[0],
                    AC_happend = true,
                    isMemberAC = false,
                    memberSuggs = new mi_sugg[0] ,
                    typeSuggs   = sga_alts.Select( alt => string.Join ( " " ,alt.steps )).ToArray() 
            };
            var nu_tok = new PTok { E = PTokE.CS_name , pay = prefix };
            uint nu_offs = 0 ;

            // checken ob cname present 
            bool has_cname ; 
            if ( AC_node is OG.TypefilterRX_Node ) has_cname = (AC_node as OG.TypefilterRX_Node ).first_name.Length > 0 ; // since zero length Token for cname is impossible 
            else if ( AC_node is OG.QTN_Component_NodeRX ) has_cname = ( AC_node as OG.QTN_Component_NodeRX ).name.Length > 0;
            else throw new Exception();

            
            
            if ( has_cname ) {
                if ( CPos.isTok ) {
                    if ( CPos.tok.E == PTokE.CS_name ) {
                        orig_TL.ReplaceTok ( CPos.tok , nu_tok );
                        
                    } else {
                        // tok is on the operator 
                        return noAC_happend ;
                    }
                } else {
                    D.Assert ( CPos.isWS ) ;
                    return noAC_happend;
                }
            } else {
                if ( CPos.isTok ) {
                    D.Assert ( (CPos.tok.E == PTokE.OP_colon) || (CPos.tok.E == PTokE.OP_dot) ) ; // hacktest , works but kind of ugly to test for tokens directly - when grammar changes: this breaks 
                    orig_TL.InsertTokAfterNode( CPos.N , nu_tok );
                } else {
                    D.Assert( CPos.isWS );
                    orig_TL.SplitWS( CPos , nu_tok ) ;
                }
            }
            RES.nu_offs = orig_TL.CPosAtEndOfTok ( nu_tok ).StringPos().NLSend("nu_pos");

            
            RES.nu_toks = orig_TL.Serialize<PTokBase> ( onTok: t => t ,
                                                        onWS : ui => new PTokWhitespace { len = ui } )
                                                        .ToArray();
            

            return RES;
            




        }
#endif 
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

                    also : how to deal with cursor pos beyond the end of an incomplete parse ? ( analog problen to synced walking for Colorize() in the console ) 
                    */
                return TranslateEntry.Scope( Stripped , GE , TR_lhs);
            };
        }

#if true 
        public static OpAC_res AC ( string str_in , uint cursor_pos , Func<string,NamedNode> GetAst ) {
            
            var l_toks = Lexer.Tokenize( str_in ) ;

            var TokL = new TokLinePlanB<PTok>();
            foreach ( PTokBase tb in l_toks ) {
                if ( tb is PTok ) {

                    TokL.AddTok( (PTok) tb ); 
                } else {
                    TokL.AddWS( (tb as PTokWhitespace).len ) ;
                }
            }
            var noAC_happend = MK_noAC_happend ( l_toks , cursor_pos );

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
            
            if      ( descrNode is MG.ACableMemb ) return MembAcc_AC( TokL , CPos , descrNode ,noAC_happend ); 
            //else if ( descrNode is OG.ACableType ) return Type_AC ( TokL, CPos , descrNode , noAC_happend);
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
