using System;

using System.Linq;
using System.IO;


using PTokBase = MainGrammar.PTokBase;

namespace ShellCommon {

    

    public abstract class CMD_Base { }

    public abstract class REQ_Base : CMD_Base { }
    public abstract class RESP_Base : CMD_Base { }

    #region AC 

    public class AC_Req : REQ_Base {
        public string arg;
        public int offs;
    }


    public class AC_Resp : RESP_Base {
        public string []       suggs        = new string [0];
        public int             nu_offs      = 0;
        public PTokBase []     toks         = new PTokBase[0];
        public string          msg          = null;
        public bool            toks_changed = false;


        public override string ToString () {
            return new {
                suggs = string.Join(",",suggs),
                nu_offs ,
                toks = string.Join(" " , toks.Select( t => t.ToString()).ToArray()),
                msg
            }.ToString();
        }
    }
    #endregion

    #region EVAL 
    public class EVAL_Req : REQ_Base {} 
    public class EVAL_Resp : RESP_Base {}
    #endregion





}