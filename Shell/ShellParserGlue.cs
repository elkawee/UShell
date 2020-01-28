using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MainGrammar;
//using SUH = Shell.ShellUtiltyHacks;

    /* 
    all of this was moved to ServerNetworkGlue 
    */ 

namespace ShellCommon {

    static class SUH { // repeated for decoupling and future refactoring 
        public static string NSpace ( int count ) {
            string R = "";
            for ( int i =0 ; i < count ; i++ ) {
                R  += " " ;
            }
            return R;
        }
    }

    public class ShellParserGlue {

        static Dictionary<PTokE,ConsoleColor > PTok_to_SHTok ;
        static ShellParserGlue() {
            PTok_to_SHTok = new Dictionary<PTokE, ConsoleColor>();
            PTok_to_SHTok[PTokE.OP_GT] = ConsoleColor.Yellow;
            PTok_to_SHTok[PTokE.OP_doubleGT] = ConsoleColor.Yellow;
            PTok_to_SHTok[PTokE.OP_colon ]   = ConsoleColor.Yellow;
            PTok_to_SHTok[PTokE.ErrT ]   = ConsoleColor.Red;
            
            // ....and so on 
        }
        static ConsoleColor Color ( PTokE tokE ) {
            try {
                return PTok_to_SHTok[tokE];
            } catch (Exception ) {
                return ConsoleColor.Gray;
            }
        }

   

    }
}
