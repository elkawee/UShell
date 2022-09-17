using System.Linq;

using PTokBase = MainGrammar.PTokBase;



/* 
 * these types share no common theme
 * what they have in common, is that they can be defined without any dependency to the rest of the system 
 * 
 * point of this module is to be a leaf in inter module dependencies 
 * it can be included everywhere whithout second thought 
 */

namespace MainGrammar
{

    public class PTokBase { }

}

namespace ShellCommon
{



    public abstract class CMD_Base { }

    public abstract class REQ_Base : CMD_Base { }   // eventually these types are supposed to have some kind of UUID for matching req/resp pairs 
    public abstract class RESP_Base : CMD_Base { }

    #region AC 

    public class AC_Req : REQ_Base
    {
        public string arg;
        public int offs;
    }


    public class AC_Resp : RESP_Base
    {
        public string[] suggs = new string[0];
        public int nu_offs = 0;
        public PTokBase[] toks = new PTokBase[0];
        public string msg = null;
        public bool toks_changed = false;


        public override string ToString()
        {
            return new
            {
                suggs = string.Join(",", suggs),
                nu_offs,
                toks = string.Join(" ", toks.Select(t => t.ToString()).ToArray()),
                msg
            }.ToString();
        }
    }
    #endregion

    #region EVAL 
    public class EVAL_Req : REQ_Base
    {
        public string expr;
    }
    /*
        with the query terms defined as they currently are 
        every result is of uniform type
        thus it seems sensical to turn this into EVAL_Resp<T> { .... T[] result ; ... } 
    */
    public class EVAL_Resp : RESP_Base
    {
        public bool success = true;
        public string msg = "";
        public System.Object[] result = new System.Object[0];
    }
    #endregion

    #region TYPEINFO
    public class TYPEINFO_Req : REQ_Base
    {
        public string expression;             // the expression to be type-info'd , like AC_Req
    }



    public class TYPEINFO_Resp : RESP_Base
    {
        public bool success;
        public string msg;                   // for errors mostly 
        public System.Type expr_unity_type; // always the payload type of the result column ( since the result of a query is always a collection , there is no need to refect that ) 
        public bool unique;          // presence of single element column enforcing modifier acting on the result column
        public System.Reflection.MemberInfo[] members;       // for getting dockable slots in the UI 
    }

    #endregion 




}