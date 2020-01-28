using System.Collections.Generic;
using System;
using System.Linq;

#if haveServerTypes

using MainGrammar;
using ShellCommon;

#endif



public static class OperationsNetworkMap {


    #region dunno ... should probably be moved elsewhere 
#if haveServerTypes
    static Dictionary<PTokE,ConsoleColor > PTok_to_SHTok ;
    static OperationsNetworkMap() {
            PTok_to_SHTok = new Dictionary<PTokE, ConsoleColor>();
            PTok_to_SHTok[PTokE.OP_GT] = ConsoleColor.Yellow;
            PTok_to_SHTok[PTokE.OP_doubleGT] = ConsoleColor.Yellow;
            PTok_to_SHTok[PTokE.OP_colon ]   = ConsoleColor.Yellow;
            PTok_to_SHTok[PTokE.ErrT ]   = ConsoleColor.Red;
            
            // ....and so on 
     }



     static class SUH { // repeated for decoupling and future refactoring 
        public static string NSpace ( int count ) {
            string R = "";
            for ( int i =0 ; i < count ; i++ ) {
                R  += " " ;
            }
            return R;
        }
    }


#endif
#endregion

#if null
    public static ShellCommon.AC_Resp OpAC_resp_to_Net_AC_resp (    Operations.OpAC_res op_AC_resp ) {
        try {
           var conv_toks = ConvertTokens( op_AC_resp.nu_toks ).ToArray(); 

                if ( ! op_AC_resp.AC_happend ) return new AC_Resp {
                    toks     = conv_toks,
                    suggs    = new string[0],
                    nu_offs  = op_AC_resp.nu_offs, // dieses AC_happend feld , und ob das jetzt in die Shell reinkommen soll muss ich noch mal ueberdenken 
                    error    = false 
                };

                string [] string_suggs;
                if ( op_AC_resp.isMemberAC ) {
                    string_suggs = op_AC_resp.memberSuggs.Select( mem_sugg => mem_sugg.str_op + " " + mem_sugg.mi.Name ).ToArray();
                } else {
                    string_suggs = op_AC_resp.typeSuggs;
                }

                return new AC_Resp {
                    suggs    = string_suggs,
                    nu_offs  = op_AC_resp.nu_offs,
                    toks     = conv_toks,
                    error    = false
                };
            } catch ( Exception e ) {
                return new AC_Resp {
                    toks  = new ShellToken[0], 
                    error = true,
                    err_string = e.ToString(),
                    suggs = new string[0]
                    
                };
            }
    }
#endif
}


