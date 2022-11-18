using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Shell;
using CoreTypes;


 class Program {
   
        /* 
            From the way this is currently written, one can inject a dummy scenegraph in UEngineMock by writing to 
            public class Resources { 
                 public static GameObject []  roots                     = new GameObject[0];
                 // [...] 
            }



        */ 
      


        static void Main(string[] args) {

            // dummy scenegraph 
            // var go = new GameObject() ;
            // go.name = "Cube_1" ; 
            Resources.roots = DummySceneGraph1.roots; 
            //



            var CState = new ConsoleState {
                Exec = ( str_in ) =>  { // "res dummy : (" + str_in + ")" 
                    var resp = Operations.Operations.EVAL_stateless( new EVAL_Req{ expr = str_in } );
                    return  "\n" + 
                        (resp.success ? "OK" : "Error" )+ 
                        resp.msg +  
                        string.Join ("\n" , resp.result.Select( _=>_.ToString()).ToArray()) ;
                },
                AC = ( str , pos ) => {                                                  
                    return  Operations.Operations.AC( new AC_Req { arg = str , offs = pos } , TMP_dumping_ground.GetAST_ptokBase ) ;
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

