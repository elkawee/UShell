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
        

    class Program {
   

      



        static void Main(string[] args) {

            var CState = new ConsoleState {
                Exec = ( str_in ) => "res dummy : (" + str_in + ")" , 
                AC = ( str , pos ) => {                                                  
                    return  Operations.Operations.AC( new ShellCommon.AC_Req { arg = str , offs = pos } , TMP_dumping_ground.GetAST_ptokBase ) ;
                }                                                                                       //   ^. from ShellServerHook.cs
            };
                


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