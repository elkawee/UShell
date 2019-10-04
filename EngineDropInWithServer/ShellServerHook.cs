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
using Newtonsoft.Json;

//using System.Runtime.Serialization.Formatters.Binary;



using NLSPlain;

public class ShellPeer {
    public TcpClient CLI;
    public JsonTCPAdapter JAdapter;
    

    LinkedList<REQ_Base> requests = new LinkedList<REQ_Base>();

    public ShellPeer ( TcpClient cl ) {
        this.CLI = cl ;
        //cl.Client.Blocking = true ; // <-  old binary formatter impl 
        JAdapter = new JsonTCPAdapter ( cl );

        new Thread ( PeerLoop ).Start(); 
    }

    // to be run in per-peer thread 
    public void PeerLoop() {
        "peer loop start".NLSend();
        try { 
            JsonSerializer serializer = new JsonSerializer();
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

    bool              network_failure_in_eval        = false; 
    System.Exception  network_failure_in_eval_Reason = null ;
    public void EvalAll() {
        JsonSerializer serializer = new JsonSerializer();
        while ( true  ) {    // how exactly do locks interact with exceptions ? control flow statements like break ? 
            REQ_Base Msg = null ; 
            bool have_msg = false;
            lock ( requests ) {
                if ( requests.Count > 0 ) { have_msg = true ; Msg = requests.First() ; requests.RemoveFirst() ; }
            }
            if ( ! have_msg ) break; 

            try {
#if todo_fixme_CMD_is_not_defined
                // -- ( EVAL ) --
                RESP_Base Resp           =  CMD.RESP_multiplexer( Msg );

                Resp.GetType().NLSend("resp"); //  <---

                // -- ( PRINT ) --
                JAdapter.Write( Resp );
#else 
                throw new NotImplementedException();
#endif

            } catch ( System.IO.IOException io_e ) {
                network_failure_in_eval = true;
                network_failure_in_eval_Reason = io_e ;
                break;
            }
        }

    }
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
            KV.Value.EvalAll();
        }
    }

}



[ExecuteInEditMode]
public class ShellServerHook : MonoBehaviour {

    static ShellServerHook () {
        ShellServer.ding = true;
    }

	
    /*     // -- Coroutines don't work in the editor
    IEnumerator foo () {
        "from coroutine".NLSend();
        yield return new  WaitForSeconds( 0.1f );
        foreach ( var KV in ShellServer.Peers ) {
            KV.Value.EvalAll();
        }
        
    }
    */ 
	void Start () {
	    
	}
	
	public bool restart_server = false ;

	void Update () {
        if ( !Application.isEditor ) ShellServer.EvalAll();
        if ( restart_server ) { restart_server = false ; ShellServer.RestartServer(); } 
        
	}
}
