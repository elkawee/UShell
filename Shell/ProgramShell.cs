using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using System.Threading;
using System.IO;

using ShellCommon;
using System.Runtime.Serialization.Formatters.Binary;



using TE = ShellCommon.ShellTokenE;
using SUH = Shell.ShellUtiltyHacks;
using System.Text.RegularExpressions;


using System.Net;
using System.Net.Sockets;

using ShellCommon;
using NLSPlain;
//using Newtonsoft.Json;
#if haveServerTypes
using MainGrammar;
using EditorlessTests;
#endif 


namespace Shell {

    class Program {
        

        static void Main(string[] args) {
#if !fakeNetwork
            try {
                ShellNetworkGlue.Init();
            } catch ( Exception e ) { Console.WriteLine(e.Message); Console.ReadLine(); return ;}
            Console.WriteLine("connected");


            var CState = new ConsoleState {
                Exec            = ( str_in ) => "foo result( " + str_in + ")" ,
                HighlightDirect = ( str_in ) => {
                    return ShellParserGlue.Colorize( str_in ) ; 
                },
                AC              = ( str_in , offs ) => {
                    var req_ac = new AC_Req { arg = str_in , offs = offs };
                    AC_Resp resp =   ShellNetworkGlue.AC ( req_ac ) ; 
                    
                    return resp;
                }

            } ;
#else
            var CState = new ConsoleState {
                Exec = ( str_in ) => "res dummy : (" + str_in + ")" , 
                HighlightDirect = ( str_in ) => {
                    return ShellParserGlue.Colorize( str_in ) ; 
                },
                AC = (str_in , offs ) => {
                    #if haveServerTypes
                    // hackk 
                    Func<string, ParserComb.NamedNode> foo = (_str) => { throw new NotImplementedException() ; };
                    var op_resp = Operations.AC ( str_in , offs , foo );
                    return OperationsNetworkMap.OpAC_resp_to_Net_AC_resp( op_resp ).NLSend();
                    #endif

                }
            };
#endif 
            Action<string> DBG_push = ( in_str ) => {
                var TL = new TokenLine();
                var Tok = new ShellToken { s_offs = 0 , e_offs = in_str.Length , orig = in_str , id = ShellTokenE.Error };
                TL.SetTokens ( new [] { Tok } );
                CState.H_push( TL);
            };

            
            DBG_push( "ZingType ::  .lel" ) ;

            while ( true ) {
                CState.STEP();
            }

            
        }
    }

}