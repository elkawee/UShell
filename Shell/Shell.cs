using System;
using System.Collections.Generic;
using System.Linq;


using SUH = Shell.ShellUtiltyHacks;
using System.Text.RegularExpressions;
using NLSPlain; 
using ParserComb;
using PTok = MainGrammar.PTok;
using PTokBase = MainGrammar.PTokBase;
using PTokWhitespace = MainGrammar.PTokWhitespace;
using MG = MainGrammar.MainGrammar;
using MGRX = MainGrammar.MainGrammarRX;
using D = System.Diagnostics.Debug;
using ShellCommon;

namespace Shell {

    public static class ShellUtiltyHacks {
            static System.Text.Encoding ASCII = System.Text.Encoding.ASCII;
            public static string Sanitize ( string arg ) {
                string res = "";
                foreach ( var C in arg ) {
                    if ( ASCII.GetString(  ASCII.GetBytes(""+C) ) == ""+C ) { // restrict to a realm, comprehensible to mere mortals with other stuff to do  
                        ushort code = C; // *should* to be UTF16 , which *should* have ASCII in its lower byte 
                        if ( code <  0x20 ) { res += " "; continue; } 
                        if ( code >= 0x80 ) { res += " "; continue; }
                        res += C;
                    }
                    else res += " ";
                }
                return res;
            }
            public static string NSpace ( int N) {
                string res = "" ; 
                for ( int i =0 ; i < N ; i++ ) res += " " ;
                return res;
            }
        }

    public class ShellTokenLineModel {                                        // for clarities sake, separate sync logic between different representations of a line from all the other state it holds 
        public string            _plain_string        = ""; 
        public PTokBase []       _toks                = new PTokBase[0];
        public struct col_itm {
            public string       str ;
            public ConsoleColor col;
        }
        public List<col_itm>     _col_itms            = new List<col_itm>();

        public bool plain_string_uptodate             = true ;
        public bool toks_uptodate                     = true ;
        public bool col_itms_uptodate                 = true ;                     // field initializers represent a valid empty tok_line - no sync needed 

        public void toks2plain_string() {
            
            _plain_string = "" ;
            foreach ( var tok in _toks ) 
                if      ( tok is PTokWhitespace ) _plain_string += SUH.NSpace( (tok as PTokWhitespace).len ) ;
                else if ( tok is PTok )           _plain_string += (tok as PTok ).pay;
                else if ( tok == null )           { _plain_string += "" ; "warnig null in tokens".NLSend(); } // TODO : policy decision 
                else throw new NotImplementedException();                          // and never will 
            
        }
        public void plain_string2toks() {
            _toks = MainGrammar.Lexer.Tokenize( _plain_string , relaxed: true )/*.NLSendRec("shell tokenize :: ")*/;
        }
        /*
            the usual lazy jazz 
            set of used/implemented dependency edges is limited though : C:\Users\User\SRC\UShell\CoreGrammar\MainGrammar.cs

            AC   case : toks   -> string | toks -> col_itm
            edit case : string -> toks   | toks -> col_itm 

            should any other direction be needed -> BUG in client code 
        */

        public string  plain_string {
            get {
                if ( ! plain_string_uptodate ) {
                    D.Assert( toks_uptodate );                                             // update _never_ comes from col_itms 
                    toks2plain_string() ;
                    plain_string_uptodate = true ;
                }
                return _plain_string; } 
            set {
                _plain_string = value ;
                plain_string_uptodate = true; toks_uptodate = false ; col_itms_uptodate = false ;
            }
        }
            

        public List<col_itm>   col_itms     { get {
                if ( ! col_itms_uptodate ) {
                    _col_itms = GrammarColorize( toks ).ToList(); 
                    col_itms_uptodate = true ;
                }
                return _col_itms ;
            }
        }
        public PTokBase [] toks { get {
                if ( ! toks_uptodate ) {
                    plain_string2toks();
                    toks_uptodate = true ;
                }
                return _toks;
            }
            set {
                _toks = value;
                toks_uptodate = true ; col_itms_uptodate = false ; plain_string_uptodate = false;
            }
        }

        public static ConsoleColor         defaultConsoleColor  = ConsoleColor.Gray;
        public static ConsoleColor         errorConsoleColor    = ConsoleColor.Red;
        public static MG.PI                ColorizeStartProd    = MGRX.ProvStartRX;

        /*
            since there are little dependencies on the precise structure of col_itm sequence, 
            atm the simplest possible implementation : 

            every token gets exactly one col_itm 
            these could be compressed ( consequtive itms of same color into one ) - i do not yet know what makes the console window so riduculusly slow
            - for lazyness sake old code fragment for the "not even partial parse possible"-case is still present -> everything compressed into a single col_itm 
        */

        public static IEnumerable<col_itm> GrammarColorize(IEnumerable<PTokBase> _lexxed)
        {
            PTokBase [] lexxed   = _lexxed.ToArray();
            PTok     [] stripped = lexxed.Where( tok => tok is PTok ).Select( tok => tok as PTok ).ToArray();

            NamedNode    rootNode    = null;
            try {
                rootNode = MG.RUN_with_rest( ColorizeStartProd , stripped ).First().N ;
            } catch ( Exception ) { }

            if ( rootNode == null ) {
                string err_str = "";
                foreach ( var tok in lexxed ) {  // concatenate to a single error token -- maybe yield multiple ( for M-b,M-a style shortkeys ) ? 
                    if ( tok is PTokWhitespace ) err_str += SUH.NSpace( (tok as PTokWhitespace).len) ;
                    if ( tok is PTok           ) err_str += (tok as PTok ).pay;
                }
                yield return new col_itm { col = ConsoleColor.Red , str = err_str } ;
                yield break;
            }
            // ------ 
            MG.TermNode [] TNs = rootNode.Leafs().Where( nn => nn is MG.TermNode ) .Select( nn => (MG.TermNode) nn  ).ToArray(); // a NamedNode with STAR as topmost prod _CAN_ be a leaf ( TODO meditate on whether to allow such a construct in the final grammar ) 
            int lexx_i = 0 ; 
            int NN_i   = 0 ;
            while( true )  {
                if ( lexx_i == lexxed.Length ) break ;
                PTokBase tok_base = lexxed[lexx_i];

                if ( tok_base is PTokWhitespace ) {
                    #region Whitespace-Block
                    yield return new col_itm { str = SUH.NSpace( (tok_base as PTokWhitespace).len) , col = defaultConsoleColor } ;  
                    #endregion
                } else {
                    if ( NN_i == TNs.Length ) break;
                    #region Token-Block
                    PTok tok = (PTok) tok_base;
                    MG.TermNode TN = TNs[NN_i] ; 
                    D.Assert( TN.tok == tok ) ;
                    // ----------

                    yield return new col_itm { col = indicateCol( TN ) , str = tok.pay } ;
                    
                    #endregion 
                    NN_i ++ ; 
                }
                lexx_i ++ ; 
            }
            for (; lexx_i < lexxed.Length ; lexx_i++ ) {
                PTokBase tok = lexxed[lexx_i];
                if ( tok is PTokWhitespace )  yield return new col_itm { col = errorConsoleColor , str = SUH.NSpace( (tok as PTokWhitespace).len) };
                else                          yield return new col_itm { col = errorConsoleColor , str = (tok as PTok).pay } ; 
            }


        }

        #region Indicator boilerplate
        public static Dictionary<Type , Func<NamedNode , indicatorRS> > indicatorsD ;
        static ShellTokenLineModel() {
            indicatorsD = new Dictionary<Type, Func<NamedNode, indicatorRS>>();
            indicatorsD[ typeof ( MG.SingleAssignNode ) ] = node => {
                var SAN = node as MG.SingleAssignNode;
                if ( SAN.type == MG.SingleAssignNode.typeE.json ) return new indicatorRS { hit = true , col = ConsoleColor.Green } ;
                else                                              return new indicatorRS { hit = true , col = ConsoleColor.Yellow } ;
            };
            indicatorsD[ typeof( MG.TypeNameNode ) ] = node => new indicatorRS { hit = true , col = ConsoleColor.Magenta } ;
            indicatorsD[ typeof( MG.SG_EdgeNode ) ]  = node => new indicatorRS { hit = true , col = ConsoleColor.Yellow } ;  // due to how the lookup works, nodes from child prods override parent colors 
        }
        // this roundabout way allows more fine grained decisions then NamedNode-type alone, e.g. only color a subset of type instances 
        public struct indicatorRS {
            public bool hit ;              // if the node instance yields a color 
            public ConsoleColor col;       // if hit is false , contents are meaningless 
        }
        public static indicatorRS noHit = new indicatorRS { hit = false , col = ConsoleColor.Gray };
        public static indicatorRS indicate ( NamedNode nn ) => indicatorsD.ContainsKey( nn.GetType() ) ? indicatorsD[ nn.GetType() ]( nn ) : noHit ;

        public static ConsoleColor indicateCol ( NamedNode NN_in ) {
            // side channel the color result 
            indicatorRS indicator = noHit ;                                                                      // init is meaningless - just make the compiler shut up ( it can't know that PathUpTo can't yield empty sequences ) 
            var res_NamedNode = NN_in.PathUpTo( NN => { indicator = indicate( NN ) ; return indicator.hit ; } );
            if ( res_NamedNode == null ) return defaultConsoleColor ;
            return indicator.col;
        }
        #endregion


        // -------------------------------------------------------------------------------

        public ShellTokenLineModel Copy() {
            return new ShellTokenLineModel { _toks = toks , _plain_string = plain_string , _col_itms = col_itms } ;   // the uptodate-bools simply stay true from field initializers 
        }
    }

    public class ShellTokenLine {
        public ShellTokenLineModel MDL = new ShellTokenLineModel();

        int CX=0;
        public int clamped_CX { get { return CX; } set { CX = Math.Max ( 0 , Math.Min ( value , MDL.plain_string.Length )) ; } } // allow the rightmost CX position to be immediately after the last char

        public void Write ( int CY = -0x29a ) {
                Console.CursorVisible = false;
                if ( CY >= 0 ) Console.CursorTop = CY;
                Console.CursorLeft = 0 ;
                foreach ( var colI in MDL.col_itms ) {
                    Console.ForegroundColor = colI.col ;
                    Console.Write( colI.str );
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                int blank_out_width =   Math.Max ( 0 , Console.WindowWidth -   Console.CursorLeft -1 ) ; // guesswork 
                Console.Write( SUH.NSpace( blank_out_width )  ) ;                                        //overwrite any junk that might be to the right of this line in the console window 
                Console.CursorLeft = CX;
                Console.CursorVisible = true; 
        }
        public ShellTokenLine Copy () => new ShellTokenLine { MDL = MDL.Copy(), CX = CX } ;
        public string line { get { return MDL.plain_string ; } set { MDL.plain_string = value ; } }

        public void SetTokens ( PTokBase [] toks ) => MDL.toks = toks;

        public void MoveLeft()  { CX = Math.Max ( 0 , CX-1); Console.CursorLeft = CX ; }
        public void MoveRight() {
            int ocx = CX;
            try { 
                CX = Math.Min ( CX+1, MDL.plain_string.Length); Console.CursorLeft = CX ; // CursorLeft setter throws out of bounds ... sometimes 
            } catch ( Exception e )  {
                //e.NLSend(" EX in MoveRight");
                CX = ocx ;
            }
        } // todo clamp against console Window border thingie
        public void InsertAtPoint ( string ins )  {
            ins = SUH.Sanitize( ins );
            string o_line = MDL.plain_string;
            MDL.plain_string =  new string ( o_line.Take(CX).ToArray() ) 
                             + ins 
                             + new string ( o_line.Skip(CX).ToArray() )   ;
            MoveRight();
        }
        public void KillPrevChar ( ) {
            if ( CX == 0 ) return ;
            string o_line = MDL.plain_string;
            MDL.plain_string = new string ( o_line.Take(CX -1 ).ToArray() ) + new string ( o_line.Skip( CX ).ToArray() ) ;  
            MoveLeft();
        }
        public void KillCharAtPoint() {
            string o_line = MDL.plain_string;
            string n_line = new string ( o_line.Take(CX).ToArray() ) + new string ( o_line.Skip(CX+1).ToArray() ); 
            MDL.plain_string = n_line ;
            CX = Math.Min( CX , n_line.Length); 
        }
        public void Home() {
            CX =0;
        }
        public void End() {
            CX = MDL.plain_string.Length;
        }

    }

    public class ConsoleState {
        public List<ShellTokenLine> Hist = new List<ShellTokenLine>();
        public int hist_pos = 0 ; 
        public int hist_len { get { return Hist.Count() ; } }
        public int global_hor_pos = 0; 
        public ShellTokenLine TL_e = new ShellTokenLine ();  // token line currently edited
        public bool scroll = false;                          // internal state varies between scrolling through history and editing the current line
        
                                                             // (scroll == true , h_len == 0 ) considered illegal 

        public Func<string,string>                          Exec;
        public Func<string , int, AC_Resp >                 AC;

        public void DisplayCurrentLine() {
            if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");
            currLine.Write();
        }


        public ShellTokenLine currLine { get { return (scroll && hist_len > 0 ) ? Hist[hist_pos] : TL_e ; } }


        public void StartModify() {
            if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");
            if ( scroll ) {
                TL_e = Hist[hist_pos].Copy();  // <- this destroys any temporary changes to TL_e that are not committed hist histroy via Enter
                scroll = false;
            } // else do nothing the line to modify already is TL_e
        }
        
        // TODO: hard limit on hist size 
        public void H_push ( ShellTokenLine TL ) { Hist.Insert ( 0 , TL.Copy() ) ; } 

        public void STEP() { // blocks on keyboard in addition to the blocking caused by the callback functions 
            ConsoleKeyInfo KeyInfo = Console.ReadKey(intercept:true);
            if ( KeyInfo.Key == ConsoleKey.UpArrow ) {
                if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");

                if      ( scroll       ) { hist_pos = Math.Min( hist_pos +1 , hist_len -1 ); }
                else if ( hist_len > 0 ) { scroll   = true; hist_pos = 0 ; }
                // else hist is empty. simply do nothing for vertical pos
                currLine.clamped_CX = global_hor_pos;

            }
            else if ( KeyInfo.Key == ConsoleKey.DownArrow ) {
                if ( scroll && hist_pos == 0 ) scroll = false ;
                if ( scroll && hist_pos >  0 ) hist_pos --    ;
                // if state is edit, down arrow does nothing 
                currLine.clamped_CX = global_hor_pos;
            }
            /*
                TokenLine, as currently implemented, holds the horizontal position of the cursor - each line its own
                they need this to "render themselves" properly (can't write to console without moving it ) 
            */
            else if ( KeyInfo.Key == ConsoleKey.LeftArrow ) {
                global_hor_pos      = Math.Max( 0 , global_hor_pos -1 );
                currLine.clamped_CX = global_hor_pos;
            }
            else if ( KeyInfo.Key == ConsoleKey.RightArrow ) {
                currLine.clamped_CX = (global_hor_pos +  1);
                global_hor_pos      = currLine.clamped_CX;       // <- contains all the clamping and stuff needed
            }
            else if ( KeyInfo.Key == ConsoleKey.Delete ) {
                StartModify();
                TL_e.KillCharAtPoint();
                global_hor_pos = TL_e.clamped_CX;
            }
            else if ( KeyInfo.Key == ConsoleKey.Backspace ) {
                StartModify();
                TL_e.KillPrevChar();
                global_hor_pos = TL_e.clamped_CX;
            }
            else if ( KeyInfo.Key == ConsoleKey.Home ) {
                currLine.Home();
            }
            else if ( KeyInfo.Key == ConsoleKey.End ) {
                currLine.End();
            }
            else if ( KeyInfo.Key == ConsoleKey.Enter ) {
                var string_arg = currLine.line;
                if ( ! Regex.Match(currLine.line , @"^\s*$").Success )   H_push(currLine ); // don't litter hist with empty lines
                scroll = false ;
                TL_e = new ShellTokenLine();
                var string_res = Exec( string_arg );
                Console.WriteLine ( "\n" + string_res + "\n" ); 
            }
            else if ( KeyInfo.Key == ConsoleKey.Tab ) {
                
                AC_Resp res = AC ( TL_e.line , TL_e.clamped_CX ) ; 
                if ( res.msg != null ) {
                    Console.WriteLine();
                    Console.WriteLine("MSG : " + res.msg);
                }
                if ( res.suggs != null && res.suggs.Length > 0  ) {    // overapproximate current protocoll (null should not occur ) 
                    Console.WriteLine();
                    foreach ( var sugg in res.suggs ) Console.WriteLine( sugg );
                }
                if ( res.toks_changed ) {
                    StartModify();     
                    TL_e.SetTokens( res.toks ) ;
                    TL_e.clamped_CX  = res.nu_offs;
                    global_hor_pos   = TL_e.clamped_CX;
                }

            }
            else { // actually edit something 
                StartModify();
                TL_e.InsertAtPoint( new string ( new [] {  KeyInfo.KeyChar } ) ) ;
                global_hor_pos = TL_e.clamped_CX;
                
            }
            DisplayCurrentLine();
        }
    }

    
}
