using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.IO;

using CoreTypes;

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
        


    class Program {
   
        public static void Test1()
        {

        }

        static void Main(string[] args) {




            IPEndPoint EP = new IPEndPoint( IPAddress.Loopback , 13333 ); ;
            TcpClient CL  = new TcpClient();
            LightJsonTCPAdapter adapter ;

            

            try {
                CL.Connect(EP);
                adapter = new LightJsonTCPAdapter( CL );
                // ShellNetworkGlue.Init();
            } catch ( Exception e ) { Console.WriteLine(e.Message); Console.ReadLine(); return ;}
            Console.WriteLine("connected");


            var CState = new ConsoleState {
                Exec            = ( str_in ) => {
                    var req_eval = new EVAL_Req { 
                        expr = str_in 
                    };

                    // copy pasta of old ShellNetworkGlue.Eval 
                    adapter.Write(req_eval);
                    EVAL_Resp resp = (EVAL_Resp)adapter.Read();
                    // e.o. pasta 

                    return 
                        "\n" + 
                        (resp.success ? "OK" : "Error" )+ 
                        resp.msg +  
                        string.Join ("\n" , resp.result.Select( _=>_.ToString()).ToArray()) ;
                },

                AC              = ( str_in , offs ) => {
                    var req_ac = new AC_Req { arg = str_in , offs = offs };
                    // pasta again 
                    adapter.Write( req_ac ); 
                    AC_Resp resp =   (AC_Resp)adapter.Read();
                    // e.o. pasta 

                    return resp;
                }

            } ;
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