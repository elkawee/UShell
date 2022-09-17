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

    public class BareTokenLine {                                        // for clarities sake, separate sync logic between different representations of a line from all the other state it holds 
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

            should any other direction be needed -> BUG in calling code 
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
        static BareTokenLine() {
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

        public BareTokenLine Copy() {
            return new BareTokenLine { _toks = toks , _plain_string = plain_string , _col_itms = col_itms } ;   // the uptodate-bools simply stay true from field initializers 
        }
    }

  
    public class RenderableTokenLine {
        // these two are the only state 
        private int           bare_line_CX = 0 ;          // model for cursor position is still only the pos relative to the edited bare-line ( window forced line breaks are incidental and ephemeral ) 
        public  BareTokenLine bareTokenLine;

        public int bare_line_clamped_CX { 
            get { return Math.Max(0,Math.Min(bare_line_CX , bareTokenLine.plain_string.Length)); }
            set { 
               if      ( value < 0 )                                 bare_line_CX = 0 ;
               else if ( value > bareTokenLine.plain_string.Length ) bare_line_CX = bareTokenLine.plain_string.Length ; // cursor-pos is immediately "after" bare line contents for append situations
               else                                                  bare_line_CX = value ;
               }
        }

        // ---------------------------------------

        public static string prompt = "[UShell] ";

        public void ScreenPos /* from bare_line_CX */ (int y_in,out int cx_out , out int cy_out ) {
            
            int w = Console.BufferWidth;
            int promptlen = bare_line_clamped_CX + prompt.Length; 
            cx_out = promptlen % w ; 
            cy_out = y_in + ( promptlen / w ) ;
        }
        
        public void RenderSelf(int requested_y , out int actual_y_start , out int actual_y_end ) {
            Console.CursorTop  = requested_y;
            Console.CursorLeft = 0;

            var col_itms = bareTokenLine.col_itms; // <- trigger parsing goes here 
            
            // since function entry explicitly sets CursorLeft, CursorLeft != 0, 
            // means a write was triggered by functions called from here -> error messages most likely -> overwrite of those is not the intent of the caller 
            if ( Console.CursorLeft != 0 ) Console.WriteLine();
            // actually do it 
            actual_y_start = Console.CursorTop ;
            Console.CursorLeft = 0 ;
            RenderInner(col_itms);
            actual_y_end   = Console.CursorTop ;
            // no assumptions on the in-buffer-state of the rest of the current line 
            ClearLineFromCursor();

            int tmp_x , tmp_y ; 
            ScreenPos( actual_y_start , out tmp_x , out tmp_y ); 
            Console.CursorLeft = tmp_x ; 
            Console.CursorTop  = tmp_y ; 
        }

        // do the absolute minimum possible around calls to Console.Write* , every write that does not render a token (or the prompt )  is considered a bug 
        public void RenderInner( List<BareTokenLine.col_itm> col_itms) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write( prompt ) ;
            foreach ( var col_itm in col_itms ) {
                Console.ForegroundColor = col_itm.col;
                Console.Write( col_itm.str ) ;
            }

            Console.ForegroundColor = BareTokenLine.defaultConsoleColor;  // concrete position of this member is TODO 
        }
        public void ClearLineFromCursor(){
            int cx = Console.CursorLeft;
            int cy = Console.CursorTop;
            int w  = Console.BufferWidth;

            // [ _ , _ , ... ,_ , X , X , ... , X   ] 
            //   0                cx          w-1
            // X's are unknown content to be overwritten, no of Xs = (w-1-cx+1) = cx - w , so yes always at least 1 ( cx <= w-1 ) 
            string WS = SUH.NSpace(w-cx ) ;
            Console.Write( WS ) ;
        }

        #region cursor manipulation boiler plate 

        public RenderableTokenLine() {
            bareTokenLine = new BareTokenLine() ;
            bare_line_CX  = 0;
        }

        public void SetTokens(PTokBase[] nu_toks) {
            bareTokenLine.toks = nu_toks;  // todo this is for autocompletion, in case of a completion cursor should move 
        }

        public void KillCharAtPoint(){
            var str = bareTokenLine.plain_string; 
            if( bare_line_clamped_CX >= str.Length ) return ;  // again, because CX == str.Length is allowed 

            var L = str.Substring(0,bare_line_clamped_CX ) ;
            var R = str.Substring(Math.Min( bare_line_clamped_CX+1, str.Length)); 
            bareTokenLine.plain_string = L+R;
        }
        public void KillPrevChar(){
            if ( bare_line_clamped_CX == 0 ) return ;

            var str = bareTokenLine.plain_string; 
            var L = str.Substring(0,bare_line_clamped_CX -1 ) ;
            var R = str.Substring(bare_line_clamped_CX); 
            bare_line_clamped_CX --;
            bareTokenLine.plain_string = L+R;
            

        }
        public void Home(){ bare_line_clamped_CX = 0 ; }
        public void End() { bare_line_clamped_CX = bareTokenLine.plain_string.Length; } 

        public void InsertAtPoint( string ins ) {
            var str = bareTokenLine.plain_string; 
            var L = str.Substring(0,bare_line_clamped_CX ) ;
            var R = str.Substring(bare_line_clamped_CX); 

            bareTokenLine.plain_string = L + ins + R ;
            bare_line_clamped_CX ++;
        }

        #endregion

        public RenderableTokenLine Copy() {
            return new RenderableTokenLine { bare_line_CX = bare_line_CX , bareTokenLine = bareTokenLine.Copy()  } ;
        }

    }

    public class ConsoleState {
        public List<RenderableTokenLine> Hist = new List<RenderableTokenLine>();
        public int hist_pos = 0 ; 
        public int hist_len { get { return Hist.Count() ; } }
        public int global_hor_pos = 0; 
        public RenderableTokenLine TL_e = new RenderableTokenLine ();  // token line currently edited
        public bool scroll = false;                          // internal state varies between scrolling through history and editing the current line
        
                                                             // (scroll == true , h_len == 0 ) considered illegal 

        public int last_render_Y_start = -1 , last_render_Y_end = -1 ;  // initialize to impossible value on purpose 

        public Func<string,string>                          Exec;
        public Func<string , int, AC_Resp >                 AC;

        public void DisplayCurrentLine() {
            if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");

            Console.CursorVisible = false ;

            int asking_y_start ;
            if ( Console.CursorTop >= last_render_Y_start && Console.CursorTop <= last_render_Y_end ) {  // heueristic for overdrawing the last rendered line
                asking_y_start = last_render_Y_start; 
                // clear everything first - wasteful, but covers most edge cases 
                for ( int i = last_render_Y_start ; i <= last_render_Y_end ; i ++ ) {
                    Console.CursorTop  = i ; 
                    Console.CursorLeft = 0 ; 
                    Console.Write( SUH.NSpace( Console.BufferWidth ) ) ;
                }
            } else {
                // in case of not overdrawing, same heueristic as in TokenLine.RenderSelf : don't kill contents on the current line 
                if ( Console.CursorLeft == 0 ) asking_y_start = Console.CursorTop; else asking_y_start = Console.CursorTop +1 ; 
            }
            int y_start, y_end ;
            currLine.RenderSelf( asking_y_start /* guesswork */ , out y_start , out y_end );

            last_render_Y_start = y_start ; 
            last_render_Y_end   = y_end; 

            Console.CursorVisible = true; 
        }


        public RenderableTokenLine currLine { get { return (scroll && hist_len > 0 ) ? Hist[hist_pos] : TL_e ; } }


        public void StartModify() {
            if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");
            if ( scroll ) {
                TL_e = Hist[hist_pos].Copy();  // <- this destroys any temporary changes to TL_e that are not committed hist histroy via Enter
                scroll = false;
            } // else do nothing the line to modify already is TL_e
        }
        
        // TODO: hard limit on hist size 
        public void H_push ( RenderableTokenLine TL ) { Hist.Insert ( 0 , TL.Copy() ) ; } 

        public void STEP() { // blocks on keyboard in addition to the blocking caused by the callback functions 

            ConsoleKeyInfo KeyInfo = Console.ReadKey(intercept:true);
            if ( KeyInfo.Key == ConsoleKey.UpArrow ) {
                if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");

                if      ( scroll       ) { hist_pos = Math.Min( hist_pos +1 , hist_len -1 ); }
                else if ( hist_len > 0 ) { scroll   = true; hist_pos = 0 ; }
                // else hist is empty. simply do nothing for vertical pos
                currLine.bare_line_clamped_CX = global_hor_pos;

            }
            else if ( KeyInfo.Key == ConsoleKey.DownArrow ) {
                if ( scroll && hist_pos == 0 ) scroll = false ;
                if ( scroll && hist_pos >  0 ) hist_pos --    ;
                // if state is edit, down arrow does nothing 
                currLine.bare_line_clamped_CX = global_hor_pos;
            }
            /*
                TokenLine, as currently implemented, holds the horizontal position of the cursor - each line its own
                they need this to "render themselves" properly (can't write to console without moving it ) 
            */
            else if ( KeyInfo.Key == ConsoleKey.LeftArrow ) {
                global_hor_pos      = Math.Max( 0 , global_hor_pos -1 );
                currLine.bare_line_clamped_CX = global_hor_pos;
            }
            else if ( KeyInfo.Key == ConsoleKey.RightArrow ) {
                currLine.bare_line_clamped_CX = (global_hor_pos +  1);
                global_hor_pos      = currLine.bare_line_clamped_CX;       // <- contains all the clamping and stuff needed
            }
            else if ( KeyInfo.Key == ConsoleKey.Delete ) {
                StartModify();
                TL_e.KillCharAtPoint();
                global_hor_pos = TL_e.bare_line_clamped_CX;
            }
            else if ( KeyInfo.Key == ConsoleKey.Backspace ) {
                StartModify();
                TL_e.KillPrevChar();
                global_hor_pos = TL_e.bare_line_clamped_CX;
            }
            else if ( KeyInfo.Key == ConsoleKey.Home ) {
                currLine.Home();
                global_hor_pos = 0 ;
            }
            else if ( KeyInfo.Key == ConsoleKey.End ) {
                currLine.End();
                global_hor_pos = currLine.bare_line_clamped_CX ; 
            }
            else if ( KeyInfo.Key == ConsoleKey.Enter ) {
                var string_arg = currLine.bareTokenLine.plain_string;
                if ( ! Regex.Match(string_arg , @"^\s*$").Success )   H_push(currLine ); // don't litter hist with empty lines
                scroll = false ;
                TL_e = new RenderableTokenLine();
                var string_res = Exec( string_arg );
                Console.WriteLine ( "\n" + string_res + "\n" ); 
            }
            else if ( KeyInfo.Key == ConsoleKey.Tab ) {
                
                AC_Resp res = AC ( TL_e.bareTokenLine.plain_string , TL_e.bare_line_clamped_CX ) ; 
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
                    TL_e.bare_line_clamped_CX  = res.nu_offs;
                    global_hor_pos   = TL_e.bare_line_clamped_CX;
                }

            }
            else { // actually edit something 
                StartModify();
                TL_e.InsertAtPoint( new string ( new [] {  KeyInfo.KeyChar } ) ) ;
                global_hor_pos = TL_e.bare_line_clamped_CX;
                
            }
            DisplayCurrentLine();
        }
    }

    
}
