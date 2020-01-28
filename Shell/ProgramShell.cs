using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.IO;

using ShellCommon;

using SUH = Shell.ShellUtiltyHacks;

using System.Net;
using System.Net.Sockets;


using NLSPlain;
//using Newtonsoft.Json;

#if haveServerTypes

using MainGrammar;

using MG = MainGrammar.MainGrammar;
using MGRX = MainGrammar.MainGrammarRX;
using ParserComb;
using TranslateAndEval;
using UnityEngine;



using SGA = SuggestionTree.SuggTAdapter;
using EditorlessTests;
#endif 


namespace Shell {
        
#if fakeNetwork   
    public class TestMG1 : MGRX {
            
            public class TestStartRXNode : NamedNode { 
                public SG_EdgeNodeRX              sgEdgeNode => (SG_EdgeNodeRX)children[0];
                public IEnumerable<FanElemNode>   fanNodes   => children.Where ( ch => ch is FanElemNode ).Select( ch => (FanElemNode) ch );
            }
            public static PI TestStartRX = Prod<TestStartRXNode> ( SEQ ( SG_EdgeRX , STAR( FanElemRX ) ) ) ;
        }


    

    public class TestStartRXTU : TU_RX
    {
        public FanElemRX_TU [] FanElemTUs ;

        public static preCH SG_Step_preCH_OUT ( MGRX.SG_EdgeNodeRX sg_edge_node ) {
            if ( sg_edge_node.typefilter.Length == 0  ) { // contract : never null , absence of CS_name toks -> Array.Len == 0
                return new explicit_preCH ( new TTuple{ isMulti = false , PayT = typeof ( GameObject ) } , _dataSrc: null ) ;
            } else { 
                return new deferred_preCH ( () => new TTuple { 
                        isMulti = false ,
                        PayT    = SGA.QTN_Exact( sg_edge_node.typefilter ) 
                        },
                    dataSrc: null );
            }

        }

        public TestStartRXTU ( TestMG1.TestStartRXNode startNode  ) { 

            preCH root_preCH = SG_Step_preCH_OUT ( startNode.sgEdgeNode ) ;
            var FEs = new List<FanElemRX_TU>();

            preCH lhs_preCH = root_preCH;
            foreach ( var FE_Node in startNode.fanNodes ) { var FE_TU = new FanElemRX_TU(  FE_Node , lhs_preCH ) ; FEs.Add( FE_TU) ; lhs_preCH = FE_TU.preCH_out ; }
            FanElemTUs = FEs.ToArray();
                
        }

        public override preCH_deltaScope scope(preCH_deltaScope c)
        {
            var c_out = c ;
            foreach ( var FE_TU in FanElemTUs ) c_out = FE_TU.scope( c_out ) ;
            return c_out ;
        }
    }

#endif

    class Program {
   
#if fakeNetwork
        public static NamedNode GetAST_ptokBase ( IEnumerable<PTokBase> toksBase ) {
            //var Stripped = TranslateEntry.LexxAndStripWS( str );

            var Stripped = toksBase.Where ( tok => tok is PTok).Select ( tok => (PTok) tok ) ;
            /*
                todo : this version of scope throws on incomplete parse - should be allowed for interactive 

                also : how to deal with cursor pos beyond the end of an incomplete parse ? ( analog problem to synced walking for Colorize() in the console ) 
                */
            GrammarEntry GE = new GrammarEntry { 
                StartProd       = TestMG1.TestStartRX , 
                /* 
                    preCH_in : 
                    the first mandatory SG operator acts on an implicit instance of a dummy type ( the PhantomRoot ) 
                */ 
                TR_constructor  = (NN ) => new TestStartRXTU((TestMG1.TestStartRXNode) NN ) 
            };
            TranslateLHS TR_lhs = new TranslateLHS { 
                preCH_LHS = new adapter_preCH ( new TypedSingleCH<GameObject>() ) ,
                scope     = new CH_closedScope()
            };
            return TranslateEntry.ScopePartial( Stripped , GE , TR_lhs);
        }   
#endif


        static void Main(string[] args) {
#if !fakeNetwork
            try {
                ShellNetworkGlue.Init();
            } catch ( Exception e ) { Console.WriteLine(e.Message); Console.ReadLine(); return ;}
            Console.WriteLine("connected");


            var CState = new ConsoleState {
                Exec            = ( str_in ) => "foo result( " + str_in + ")" ,

                AC              = ( str_in , offs ) => {
                    var req_ac = new AC_Req { arg = str_in , offs = offs };
                    AC_Resp resp =   ShellNetworkGlue.AC ( req_ac ) ; 
                    
                    return resp;
                }

            } ;
#else
            var CState = new ConsoleState {
                Exec = ( str_in ) => "res dummy : (" + str_in + ")" , 
                AC = ( str , pos ) => {
                    return  Operations.AC( new ShellCommon.AC_Req { arg = str , offs = pos } , GetAST_ptokBase ) ;
                }
            };
                

#endif 
            Action<string> DBG_push = ( in_str ) => {
                /*
                var TL = new TokenLine();
                var Tok = new ShellToken { s_offs = 0 , e_offs = in_str.Length , orig = in_str , id = ShellTokenE.Error };
                TL.SetTokens ( new [] { Tok } );
                CState.H_push( TL);
                */
            };

            
            DBG_push( "ZingType ::  .lel" ) ;

            while ( true ) {
                CState.STEP();
            }

            
        }
    }

}