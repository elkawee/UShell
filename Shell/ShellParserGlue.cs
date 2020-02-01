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

 
}
