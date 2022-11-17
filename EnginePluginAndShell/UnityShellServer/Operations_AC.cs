using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ParserComb;
using System.Reflection;

using SGA = SuggestionTree.SuggTAdapter;
using MG = MainGrammar.MainGrammar;
using MGRX = MainGrammar.MainGrammarRX;
using D = System.Diagnostics.Debug;
using MainGrammar ;

using CoreTypes;


// SuggTree decoupling types 
using MembK = SuggestionTree.MembK;
using MembK_E = SuggestionTree.MembK_E;

using NLSPlain;
using System.IO;

/*
    TODO rename this srcfile OperationsAC and move it to EngineWithServer - then direct knowledge of ShellCommon network types is no problem 
*/


namespace Operations { // todo maybe own namespace 

  
    public static partial class Operations {
        
        public struct mi_sugg {     // dummy tuple, for memb-type prefixes in return 
            public string str_op ; 
            public MemberInfo mi ;
        }

        #region request/response

        /*
            these encapsulate the to be serialized AC_Response type and its protocol 
            point being, to have a limited surface with direct knowlege of the network types and their invariants
            (1) -> (2) -> (3)           (1) Responder + AC_Req   (2) Responder + AC_Req + tokenized  (3) FinalizedResponse
              \___________/
        */
        public class Responder { 
            public AC_Req request;
            public PTokBase[] tokenizedRequest = null; // <- most responses require these to be set, null to deliberately crash the CF-paths that violate this 
            public Responder( AC_Req request ) {
            }

            string [] mi_suggs_to_string ( IEnumerable<mi_sugg> suggs ) { 
                return suggs.Select( sugg => sugg.str_op + " " + sugg.mi.Name ).ToArray();
            }
            string [] type_alts_to_string ( IEnumerable<SGA.typeAC_alt> suggs ) { 
                var L = new List<string>();
                foreach ( var typeAlt in suggs ) {
                    L.Add( string.Join("/",typeAlt.steps ) ) ;
                }
                return L.ToArray();
            }

            public FinalizedResponse NoAC( string msg=null ) {
                var resp = new AC_Resp { 
                    toks_changed = false ,
                    msg = msg ,
                };
                return new FinalizedResponse( resp );
            }

            public FinalizedResponse MembACNoSubst   ( IEnumerable<mi_sugg> suggs ) {
                var resp = new AC_Resp { 
                    toks_changed = false,
                    suggs = mi_suggs_to_string( suggs )
                };
                return new FinalizedResponse( resp ) ; 
            }
            public FinalizedResponse MembACWithSubst ( IEnumerable<mi_sugg> suggs , PTokBase[] new_toks , int nu_offs ) {
                var resp = new AC_Resp { 
                    toks_changed = true,
                    suggs = mi_suggs_to_string( suggs ),
                    toks  = new_toks,
                    nu_offs = nu_offs
                };
                return new FinalizedResponse( resp ) ; 
            }
            public FinalizedResponse TypeACNoSubst   ( IEnumerable<SGA.typeAC_alt> alts ) {
                var resp = new AC_Resp {
                    toks_changed = false,
                    suggs = type_alts_to_string( alts )
                };
                return new FinalizedResponse( resp );
            }
            public FinalizedResponse TypeACWithSubst ( IEnumerable<SGA.typeAC_alt> alts , PTokBase[] new_toks , int nu_offs ) {
                var resp = new AC_Resp {
                    toks_changed = true,
                    suggs = type_alts_to_string( alts ),
                    toks = new_toks,
                    nu_offs = nu_offs 
                };
                return new FinalizedResponse( resp );
            }
            public FinalizedResponse FuncACWithSubst( IEnumerable<MethodInfo> mis , PTokBase[] new_toks , int nu_offs ) {
                var resp = new AC_Resp {
                    toks_changed = true,
                    suggs = mis.Select( mi => mi.Name ).ToArray(),
                    toks = new_toks,
                    nu_offs = nu_offs 
                };
                return new FinalizedResponse( resp );
            }
            public FinalizedResponse FuncACNoSubst( IEnumerable<MethodInfo> mis  ) {
                var resp = new AC_Resp {
                    toks_changed = false,
                    suggs = mis.Select( mi => mi.Name ).ToArray()
                };
                return new FinalizedResponse( resp );
            }

        }
        public class FinalizedResponse { // extra type to force explicit setting of response for all CF-paths via compiler ( abuse type as finite-state-machine state (3)  ) 
            public AC_Resp ac_response;
            public FinalizedResponse( AC_Resp resp ) { ac_response = resp; }
        }

        #endregion 
        
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
        
        public static PTokBase[] SerializeTokLine ( TokLinePlanB<PTok> line ) => line.Serialize<PTokBase>( onTok: _ => _ , onWS: i=> new PTokWhitespace {len = i } ).ToArray();
        
        public static FinalizedResponse MembAcc_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC pos , MGRX.MemANodeRX MA_node , Responder RESP ) {
            
            #region setup 
            
            string orig_prefix = MA_node.name ;                                  // <- .name is normalized to "" , never null 
            MembK orig_MembK   = MembKF_from_refineTok ( MA_node.refineOPTok ) ; // resolves to "_any" for null argument 


            MemberInfo[] mis  = new MemberInfo[0];
            Type     baseType = null ;
            try { 
                // i *THINK* ACMembTypingCallback is supposed to be set at creation time of the corresponding TranslationUnit ( i.e. its constructor)  - this is not happening atm 
                // fetch the type here to base SuggTreeFetching of MethodInfos on, the MembKFilter argument to that thing is something else entirely



                baseType = (MA_node as MG.ACableMemb).ACMembTypingCallback();



            } catch ( MG.NoACPossible ) {   // <- questionable if this exception is needed at all 
                return RESP.NoAC("excpt: MG.NoACPossible" );
            } catch ( Exception e ) {
                return RESP.NoAC("general typing exception: " + e.ToString() ) ;
            }
            if ( baseType == null  ) return RESP.NoAC("baseType == null " ); 

            try { 


                mis = SGA.MembAC( baseType , orig_prefix , orig_MembK );


            } catch ( Exception )  { return RESP.NoAC(" SuggTree exception " ); }
            if ( mis.Length == 0 ) return RESP.NoAC(" no suggs " );


            #endregion 

            string new_prefix      = SGA.LongestCommonPrefix ( mis.Select(mi => mi.Name).ToArray()) ;

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
            Func<MemberInfo,mi_sugg> conv_mi_sugg = ( MI ) => {
                var str_op = refineTok_from_MembK( new MembK ( MI ) ).pay;
                return new mi_sugg { 
                    str_op = str_op,
                    mi     = MI
                };
            };
            PTokBase [] nu_toks = orig_TL.Serialize<PTokBase>( onTok: _ => _ , onWS: i=> new PTokWhitespace {len = i } ).ToArray();

            return RESP.MembACWithSubst( mis.Select( conv_mi_sugg ) , nu_toks , pos.StringPos() );

        }




        public static FinalizedResponse Type_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC CPos , NamedNode n ,  Responder RESP ) {
            
            Func<PTokE,bool> ON    = tokE => { var tok = CPos.insideof_tok ;            if ( tok == null ) return false ; return tok.E == tokE ; }; 
            Func<PTokE,bool> AFTER = tokE => { var tok = CPos.immediateLAdj_tok ; if ( tok == null ) return false ; return tok.E == tokE ; }; 

            MGRX.TypeNameNodeRX TNRX_node = (MGRX.TypeNameNodeRX) n;

            if ( ON( PTokE.CS_name ) || AFTER ( PTokE.CS_name )) { 
                PTok targetTok = null ;
                if ( (CPos.insideof_tok != null) && (CPos.insideof_tok.E == PTokE.CS_name ) ) targetTok = CPos.insideof_tok; 
                else targetTok = CPos.immediateLAdj_tok;  // todo: unsafe 

                var Largs = new List<string>();
                foreach ( var tok in TNRX_node.nameToks ) { Largs.Add( tok.pay ); if ( tok == targetTok ) break; } // targetTok might not be the last in sequence - collect all CS_names upto and including 

                string prefix = null ;
                var type_alts = SGA.QTN_AC ( Largs.ToArray() , out prefix );

                if ( prefix.Length > targetTok.pay.Length ) { 
                    var new_tok = new PTok { E =  PTokE.CS_name , pay = prefix };
                    orig_TL.ReplaceTok( targetTok , new_tok ) ;
                    return RESP.TypeACWithSubst ( type_alts , SerializeTokLine( orig_TL ) ,  orig_TL.CPosAtEndOfTok( new_tok ).StringPos() ) ;
                } else {
                    return RESP.TypeACNoSubst ( type_alts );
                }

            } else if ( CPos.immediateLAdj_tok == null ) {  // abuse this as "on whitespace or EOL" - probably incomplete
                // problem : 
                // TokLine was not designed with the possibility in mind that Tokens change without notice
                // thus: insert dummy token | do stuff | replace dummy token with the final one 

                PTok placeholderTok = new PTok { E = PTokE.CS_name , pay = "" } ;
                var placeholderCpos =  orig_TL.InsertAfterCPos( CPos, placeholderTok );

                // extra evil - accessing internal Node structure directly - wo way to iterate from CPosC yet 
                PTok delim = null ; // non whitespace token to the left of insertion point 
                var cand = placeholderCpos.N.left;
                while ( true ) { if ( cand is TokLinePlanB<PTok>.NodeTok ) { delim = cand.tok ; break ; } cand = cand.left; }  

                var Largs = new List<string>();
                foreach ( var tok in TNRX_node.Leafs().Where(N => N is MG.TermNode ).Select( MG.TermTok ) ) { // have to iterate over all terminals instead of just CS_names , because delimiter might be some other kind 
                    if ( tok.E == PTokE.CS_name ) Largs.Add( tok.pay );  
                    if ( tok == delim ) break ; // collect inclusive delimiter 
                }
                Largs.Add(""); // last arg to SuggTree is an empty prefix 
                string prefix ;
                var alts = SGA.QTN_AC ( Largs.ToArray() , out prefix ) ;
                if ( prefix.Length >0 ) { 
                    return RESP.TypeACNoSubst ( alts ) ;
                } else {
                    var final_tok = new PTok { E = PTokE.CS_name , pay = prefix }; 
                    orig_TL.ReplaceTok( placeholderTok , final_tok ) ; 
                    return RESP.TypeACWithSubst ( alts , SerializeTokLine( orig_TL ) , orig_TL.CPosAtEndOfTok( final_tok ).StringPos() );
                }
            }

            
            
            
            return RESP.NoAC("kind of type AC not implemented atm" );
        }

        public static FinalizedResponse FuncName_AC ( TokLinePlanB<PTok> orig_TL , TokLinePlanB<PTok>.CPosC CPos , MGRX.FuncNameNodeRX funcNameNodeRX , Responder RESP ){
            Func<PTokE,bool> ON    = tokE => { var tok = CPos.insideof_tok ;            if ( tok == null ) return false ; return tok.E == tokE ; };
            Func<PTokE,bool> LADJ  = tokE => { var tok = CPos.immediateLAdj_tok ;       if ( tok == null ) return false ; return tok.E == tokE ; };

            Func<TokLinePlanB<PTok>,PTokBase[]> SER = tokLine => tokLine.Serialize<PTokBase>( onTok: _ => _ , onWS: i=> new PTokWhitespace {len = i } ).ToArray();

            Type hosting_type;
            try { 
                hosting_type = funcNameNodeRX.AC_FuncHostingType_Callback();
                if ( hosting_type == null ) throw new Exception( "hosting type null" ) ; 
            } catch ( Exception e ) {
                return RESP.NoAC( e.Message ) ;
            }
            string name_fragment ;
            if( LADJ( PTokE.CS_name ) ) {
                name_fragment = CPos.immediateLAdj_tok.pay ;

                MethodInfo [] raw_suggs_MI = SGA.MethodAC( hosting_type , name_fragment , ( funcNameNodeRX.parent as MG.FunCallNode) .isStatic );
                if ( raw_suggs_MI.Length == 0 ) return RESP.NoAC( "no matching alternatives" ); 

                string new_prefix      = SGA.LongestCommonPrefix ( raw_suggs_MI.Select(mi => mi.Name).ToArray()) ;

                if( new_prefix.Length > name_fragment.Length ) {

                    PTok new_token = new PTok { pay = new_prefix , E = PTokE.CS_name } ;
                    orig_TL.ReplaceTok( CPos.immediateLAdj_tok , new_token );

                    var nu_Cpos = orig_TL.CPosAtEndOfTok( new_token );
                    return RESP.FuncACWithSubst( raw_suggs_MI , SER( orig_TL ) , nu_Cpos.StringPos() );
                } else { 
                    return RESP.FuncACNoSubst( raw_suggs_MI ); 
                }

            } else if ( LADJ( PTokE.OP_backslash ) ) {
                
                //  no check for whether the curser is under a CS_name token , like so 
                //  [>> [_] :Tyname\Funcname]
                //              _
                // in this case junk gets substituted left of `Funcname`
                // idc rn - there are bigger fish to fry 
                MethodInfo [] raw_suggs_MI = SGA.MethodAC( hosting_type , "" , ( funcNameNodeRX.parent as MG.FunCallNode) .isStatic );
                if ( raw_suggs_MI.Length == 0 ) return RESP.NoAC( "no matching alternatives" );

                string new_prefix          = SGA.LongestCommonPrefix ( raw_suggs_MI.Select(mi => mi.Name).ToArray()) ;
                if( new_prefix.Length > 0 ) {
                    var nu_tok = new PTok{ E = PTokE.CS_name , pay = new_prefix } ; 
                    orig_TL.InsertTokAfterNode(  CPos.N , nu_tok );                // this is the precise behaviour of IsertAfterCPos when its not over whitespace 

                    var nu_Cpos = orig_TL.CPosAtEndOfTok( nu_tok ) ;
                    var nu_offs = nu_Cpos.StringPos();

                    return RESP.FuncACWithSubst( raw_suggs_MI, SER( orig_TL ) , nu_offs ) ;

                } else { 
                    return RESP.FuncACNoSubst( raw_suggs_MI ) ; 
                } 


            } else return RESP.NoAC("useless position" ); 

            
            // currrently only doing ON( CS_name ) 


        }

        /* 

            GetAst 
            
            functor to decouple the intricacies of translation and scoping ( particularly scoping and choice of grammar start production ) 
            to be moved elswhere later 

            passing in an already generated AST does not make sense because this only this part is supposed to know how to react to errors ( how pass them on to the interactive shell and so forth ) 
        */ 
       


        public static AC_Resp AC (  AC_Req shell_ac_request , 
                                                Func<IEnumerable<PTokBase>,NamedNode> GetAst  // Shell needs to do its own tokenization 
                                                ) {
            
            var RESPONDER = new Responder ( shell_ac_request );
            string  str_in     = shell_ac_request.arg ;
            int     cursor_pos = shell_ac_request.offs ;

            /*
                atm error tokens are normal PTok's with E == PTokE.ErrT
                no special treatment in stripping needed - there are simply no productions that recognize token sequences with ErrT's among them 
            */
            var l_toks = Lexer.Tokenize( str_in , relaxed: true  ) ;  

            var TokL = new TokLinePlanB<PTok>();
            foreach ( PTokBase tb in l_toks ) {
                if ( tb is PTok ) {
                    TokL.AddTok( (PTok) tb ); 
                } else {
                    TokL.AddWS( (tb as PTokWhitespace).len ) ;
                }
            }
            

            var CPos = TokL.CPosFromStringpos( cursor_pos ) ;
            
            PTok AC_Tok = CPos.conflated_clusterF_AC_tok();

            if ( AC_Tok == null ) return RESPONDER.NoAC(" no acable tok " ).ac_response;
            NamedNode AST;
            try { 
                 AST = GetAst(  l_toks ) ;                 
            } catch ( Exception e ) {
                return RESPONDER.NoAC( e.ToString() ).ac_response;        // <-- this uses includes variable resolution and all kinds of other shenanigans, that are only needed for membAC, typeAC is a lot simpler - could do with only parsing 
            }
            if ( AST == null ) return RESPONDER.NoAC(" GetAst() == null  " ).ac_response;

            
            
            // with epsilon consuming productions ( ex: DeclStar ) "all Leafs are TermNodes" can not be relied on anymore 
            var TermLeafs = AST.Leafs().Where ( nn => nn is MG.TermNode ).Select( n => (MG.TermNode)n ).ToArray().NLSendRec("term leafs");

            NamedNode AC_Node = null;
            int i = 0;
            for (; i < TermLeafs.Length ; i++ ) if ( TermLeafs[i].tok == AC_Tok) { AC_Node = TermLeafs[i]; break; }

            if ( AC_Node == null ) return RESPONDER.NoAC("ac beyond parsable").ac_response;  // TODO :  D.Assert() that this is actually true with something like :  PToks.Skip(i).Where( term.tok == AC_Tok).Single

            NamedNode descrNode = AC_Node.PathUpTo( (n) => ( n is MG.ACable ) ) ;
            if ( descrNode == null ) return RESPONDER.NoAC(" descrNode == null  " ).ac_response;
            
            
            if      ( descrNode is MG.ACableMemb )     return MembAcc_AC  ( TokL , CPos , (MGRX.MemANodeRX) descrNode     , RESPONDER ).ac_response; // <- todo cast not typesafe 
            else if ( descrNode is MG.ACableTypeName ) return Type_AC     ( TokL , CPos , descrNode                       , RESPONDER).ac_response;
            else if ( descrNode is MG.ACableFuncName ) return FuncName_AC ( TokL , CPos , (MGRX.FuncNameNodeRX)descrNode  , RESPONDER).ac_response;
            else throw new NotImplementedException(); 
            

        }


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



    public class AC_Test {
        public static void TestAll() { 
            Console.WriteLine( "------ AC Test ------ ") ;
            Test_Memb1();
            Test_Func1();
            Console.WriteLine( " ==== ac ok ===== " ) ; 
        }
        public static void Test_Func1(){
            var req  = AC_Req_From_String( " [] :FuncAC_Test\\F" , "  " );
            var resp = Operations.AC( req , TMP_dumping_ground.GetAST_ptokBase ) ;
            
            D.Assert( resp.toks_changed == false );
            D.Assert( resp.suggs.Length == 3 ) ;      // namely [ F1 , F2 , F22 ] 
            
            
            var req2  = AC_Req_From_String( " [] :FuncAC_Test\\" , "  " );
            var resp2 = Operations.AC( req2 , TMP_dumping_ground.GetAST_ptokBase ) ;

            D.Assert( resp2.toks_changed == true );
            D.Assert( resp2.suggs.Length >= 3 ) ;      // <- adjust if static members of test type change 
            D.Assert( ToksToString( resp2.toks ) == ( " [] :FuncAC_Test\\" + "F" + "  " ) ) ;  // "F" is the common prefix of [ F1 , F2 , F22 ] - everything else should be exactly the same 

        }

        public static void Test_Memb1() {
            var req = AC_Req_From_String( " >> :Animator ." , "" ) ;
            var resp = Operations.AC( req , TMP_dumping_ground.GetAST_ptokBase ) ;
        }

        // from a given expression separated as "left_half" "right_half" calculate AC_Request 
        // cursor pos is set to "left_half".Length 
        // ( this corresponds with how everything else treats cursor positions , ( the pos is the index at which it would insert a char, possibly invalid index in case of append  ) 
        //   e.g. for a cursor at the end of a line     ("foo bar" "") 
        public static AC_Req   AC_Req_From_String ( string left_expr , string right_expr  ) {
            
            return new AC_Req { arg = left_expr + right_expr , offs = left_expr.Length } ;
        }
        // for easy comparison in testcases 
        public static string ToksToString ( IEnumerable<PTokBase> toks_in ) { 
            string R = "" ; 
            foreach( var tok in toks_in ) {
                if      ( tok is PTokWhitespace ) for ( int i=0 ; i < ((PTokWhitespace)tok).len ; i ++ ) R +=  " " ; 
                else if ( tok is PTok )           R += ((PTok) tok ).pay ;
                else  D.Assert(false) ;
            }
            return R;
        }
    }


}

// outside of namespaces for easier testamathings 
public class FuncAC_Test { 
    public static void  F1() {}
    public static void  F2() {}
    public static void  F22() {}
}

