using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Linq;
using System;
using System.Threading;
using System.IO;

using ShellCommon;

using OPS = Operations.Operations ;
using MainGrammar;  // Operations lives in this namespace atm -- TODO 

using ParserComb;
using TranslateAndEval;
using NLSPlain;

using MGRX = MainGrammar.MainGrammarRX;
using SGA = SuggestionTree.SuggTAdapter;

// for temporary glue-code sizzle that is supposed to be moved elsewhere eventually 
public static class TMP_dumping_ground {

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

    public readonly Thread thread;

    public ShellPeer ( TcpClient cl ) {
        this.CLI = cl ;
        JAdapter = new LightJsonTCPAdapter ( cl );
        thread = new Thread ( PeerLoop );
        thread.Start(); 
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
            e.StackTrace.NLSend();
        } catch ( Exception e ) {
            e.GetType().NLSend("exception in per-peer thread. terminating");
            e.NLSend();
            e.StackTrace.NLSend();
        }
    }

    // to be run in the normal Unity Script thread 

    #region unity_script_thread

    bool              network_failure_in_eval        = false; 
    System.Exception  network_failure_in_eval_Reason = null ;
    public bool EvalQueuedRequests() {
        bool had_request = false; 
        while ( true  ) {    // how exactly do locks interact with exceptions ? control flow statements like break ? 
            REQ_Base Msg = null ; 
            bool have_msg = false;
            lock ( requests ) {
                if ( requests.Count > 0 ) { have_msg = true ; Msg = requests.First() ; requests.RemoveFirst() ; }
            }
            if ( ! have_msg ) break; 

            try {
                had_request = true;      // <- this is for triggering in editor redraws - perfer fals positives over missing one ( a request might fail during execution but still cause side effects ) 
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
        return had_request;

    }

    public RESP_Base Multiplex ( REQ_Base request ) {
        if ( request is AC_Req ) { 
            return     OPS.AC( (AC_Req) request , TMP_dumping_ground.GetAST_ptokBase ) ;
        } else if ( request is EVAL_Req ) {
            return     OPS.EVAL_stateless ( (EVAL_Req) request , analyz0r: use_analyz0r) ;
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
        Peers           = new Dictionary<TcpClient, ShellPeer>();              // Todo look up Hash-function for TcpClient 
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


    static void killAllClients() {
        /*
            this kills pending unprocessed messages in the queues,
            but it's only ever used if server restart is forced upon from the outside
        */ 

        // foreach( var kv in Peers ) kv.Value.thread.Abort();  // this doesn't do the trick 
        foreach( var kv in Peers ) kv.Key.Close() ;          // but this does :)             maybe there are more elegant ways of waking up a thread from a Wait() on a socket - but i don't know em ... 
        Peers.Clear();
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
            e.Message.NLSend("exception in server listener thread");
        } finally {
            server_listener.Stop();
            killAllClients();
        }
    }
    public static bool EvalAll() {
        /*
            When entering playmode, Unity hangs, this is probalby where.
            The entire assembly is unloaded and threads are killed, but even if the ShellPeer-Thread died, 
            this should not affect reading the linked list at all ( unless it died during lock(), but it happens every time )

            this could only really be during unload - as everything is initialized afresh on playmode-enter dll-load

            the hang stops as soon as you kill the remote shells 
            ( unity forcibly aborts the server-listener thread but not the per-Peer threads ? ) 

            vll kann thread, der auf einem socket blockt nicht "aborted" und haengt endlos 
        */
        var remove = new HashSet<TcpClient>();
        bool had_update = false; 


        foreach ( var KV in ShellServer.Peers ) {
            ShellPeer shellPeer = KV.Value;
            if ( ! shellPeer.thread.IsAlive ) { remove.Add( KV.Key );  continue; }
            had_update = had_update || shellPeer.EvalQueuedRequests();
        }
        foreach( var tcp_cl in remove ) { Peers.Remove( tcp_cl) ; "removing shellPeer ( thread died )".NLSend(); }
        return had_update;
    }

}


