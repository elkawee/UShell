



/* 

    for a first approximation most of this is a pasta from ShellServerHook.cs 


   */



using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Linq;
using System;
using System.Threading;
using System.IO;

using CoreTypes;

using OPS = Operations.Operations ;
using MainGrammar;  // Operations lives in this namespace atm -- TODO 

using ParserComb;
using TranslateAndEval;
using NLSPlain;

using MGRX = MainGrammar.MainGrammarRX;
using SGA = SuggestionTree.SuggTAdapter;

// for temporary glue-code sizzle that is supposed to be moved elsewhere eventually 
public static class TMP_dumping_ground {
    #if null 
    public class TMP_RX_Grammar : MGRX {

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

        public TestStartRXTU ( TMP_RX_Grammar.TestStartRXNode startNode  ) { 

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

    public static NamedNode GetAST_ptokBase ( IEnumerable<PTokBase> toksBase ) {

        var Stripped = toksBase.Where ( tok => tok is PTok).Select ( tok => (PTok) tok ) ;
            /*
            todo : this version of scope throws on incomplete parse - should be allowed for interactive 
            also : how to deal with cursor pos beyond the end of an incomplete parse ? ( analog problem to synced walking for Colorize() in the console ) 
            */
        GrammarEntry GE = new GrammarEntry { 
            StartProd       = MGRX.ProvStartRX , 
            /* 
                preCH_in : 
                the first mandatory SG operator acts on an implicit instance of a dummy type ( the PhantomRoot ) 
            */ 
            TR_constructor  = (NN ) => new ProvStartTU_RX((MainGrammarRX.ProvStartNodeRX) NN ) 
        };
        TranslateLHS TR_lhs = new TranslateLHS { 
            preCH_LHS = null ,  //new adapter_preCH ( new TypedSingleCH<GameObject>() ) ,
            scope     = new CH_closedScope()
        };
        return TranslateEntry.ScopePartial( Stripped , GE , TR_lhs);
    }   
}


public class ShellPeer {

    public static bool use_analyz0r = false ;

    public TcpClient CLI;
    public LightJsonTCPAdapter JAdapter;
    

    LinkedList<REQ_Base> requests = new LinkedList<REQ_Base>();

    public ShellPeer ( TcpClient cl ) {
        this.CLI = cl ;
        JAdapter = new LightJsonTCPAdapter ( cl );
        new Thread ( PeerLoop ).Start(); 
    }

    // to be run in per-peer thread 
    public void PeerLoop() {
        "peer loop start".NLSend();
        try { 
            
            while (true ) {
                if ( network_failure_in_eval ) throw network_failure_in_eval_Reason;
                // -- ( READ ) --
                
                var Msg            = (REQ_Base) (JAdapter.Read());

                lock ( requests ) {
                    requests.AddLast( Msg );
                }
            }
        } catch ( System.IO.IOException e ) {
            e.Message.NLSend( "peer dropped connection. ");
        } catch ( Exception e ) {
            e.GetType().NLSend("exception in per-peer thread. terminating");
            e.Message.NLSend();
            e.StackTrace.NLSend();
        }
    }

    // to be run in the normal Unity Script thread 

    #region unity_script_thread

    bool              network_failure_in_eval        = false; 
    System.Exception  network_failure_in_eval_Reason = null ;
    public void EvalQueuedRequests() {
       
        while ( true  ) {    // how exactly do locks interact with exceptions ? control flow statements like break ? 
            REQ_Base Msg = null ; 
            bool have_msg = false;
            lock ( requests ) {
                if ( requests.Count > 0 ) { have_msg = true ; Msg = requests.First() ; requests.RemoveFirst() ; }
            }
            if ( ! have_msg ) break; 

            try {

                // -- ( EVAL ) --
                RESP_Base Resp           =  Multiplex( Msg );

                // -- ( PRINT ) --
                JAdapter.Write( Resp );


            } catch ( System.IO.IOException io_e ) {
                network_failure_in_eval = true;
                network_failure_in_eval_Reason = io_e ;
                break;
            }
        }

    }

    public RESP_Base Multiplex ( REQ_Base request ) {
        if ( request is AC_Req ) { 
            return     OPS.AC( (AC_Req) request , TMP_dumping_ground.GetAST_ptokBase ) ;
        } else if ( request is EVAL_Req ) {
            return     OPS.EVAL_stateless ( (EVAL_Req) request , analyz0r: use_analyz0r) ;
        } else if ( request is TYPEINFO_Req ) {
            return OPS.TYPEINFO( (TYPEINFO_Req) request ) ;
        } else throw new NotImplementedException();
    }
    #endregion 
}


public class ShellServer {
    const int defaultPort = 13333;
    static TcpListener server_listener;
    public static Dictionary<TcpClient,ShellPeer> Peers;
    
    public static bool ding; // <- access this as a ghetto way of triggering class load and thus initialization 

    static ShellServer() {
        "hi from static constructor :) ".NLSend();     
        try { 
        Peers           = new Dictionary<TcpClient, ShellPeer>();
        var serverEP    = new IPEndPoint(IPAddress.Loopback , defaultPort );
        server_listener = new TcpListener( serverEP );
        server_listener.Start();
        new Thread( ServerLoop ).Start();
        "shell server listener thread started".NLSend("-----");
        } catch (Exception e ) {
            e.Message.NLSend("listener thread start failed");
        }
    }
    

    public static void  RestartServer() {
        var serverEP    = new IPEndPoint(IPAddress.Loopback , defaultPort );
        try { server_listener.Stop(); } catch { }  // <----  not yet sure about this one
        server_listener = new TcpListener( serverEP );
        server_listener.Start();
        new Thread( ServerLoop ).Start();
        "new listener thread spawned".NLSend();
    }


    static void ServerLoop () {
        
        try { 
            while (true ) {
                TcpClient cl =  server_listener.AcceptTcpClient();
                lock ( Peers ) {
                    Peers[cl]   = new ShellPeer(cl);
                }
            }
        } catch ( Exception e ) {
            e.Message.NLSend("exception in listener thread");
        } finally {
            server_listener.Stop();
        }
    }
    public static void EvalAll() {
        foreach ( var KV in ShellServer.Peers ) {
            KV.Value.EvalQueuedRequests();
        }
    }

}






class ProgramMockServer
{
    static void Main(string[] args)
    {
        Operations.AC_Test.TestAll();
        Resources.roots = DummySceneGraph1.roots ; 
        // since this is the design copied from the Unity plugin, which has the main script thread effectively polling EvalAll() 
        // ... just recreate the same situation 

        // server listener thread is started implicitly in static constructor 

        while(true ) {
            Thread.Sleep(50);  // 20 fps
            ShellServer.EvalAll();
        }

    }
}

