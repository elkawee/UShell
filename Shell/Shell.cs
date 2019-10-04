using System;
using System.Collections.Generic;
using System.Linq;


using ShellCommon;




using TE = ShellCommon.ShellTokenE;
using SUH = Shell.ShellUtiltyHacks;
using System.Text.RegularExpressions;
using NLSPlain; 

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

    public class TokenLine {
            // best would prob. be to have a whitelist of chars like Western ISO
            public string line = "";
            int CX=0;
            public int clamped_CX { get { return CX; } set { CX = Math.Max ( 0 , Math.Min ( value , line.Length )) ; } } // allow the rightmost CX position to be immediately after the last char

            ShellToken [] tokens = new ShellToken[0];

            public TokenLine Copy() {
                return new TokenLine { line = line , CX=CX , tokens = (ShellToken[]) tokens.Clone() } ;
            }

            public void SetTokens( IEnumerable<ShellToken> tokens ) {
                line = string.Join("", tokens.Select ( t => t.orig ).ToArray());
                this.tokens = tokens.ToArray();
            }
            

            public void WritePlain (int CY = -0x29a ) { // CY for later
                Console.CursorVisible = false;
                if ( CY >= 0 ) Console.CursorTop = CY; 
                Console.CursorLeft = 0 ;
                Console.Write( line ) ;
                int blank_out_width =   Math.Max ( 0 , Console.WindowWidth -   Console.CursorLeft -1 ) ; // guesswork 
                Console.Write( SUH.NSpace( blank_out_width ) ) ; //overwrite any junk that might be to the right of this line in the console window 
                Console.CursorLeft = CX;
                Console.CursorVisible = true; 
            }

            public void Write ( int CY = -0x29a ) {
                Console.CursorVisible = false;
                if ( CY >= 0 ) Console.CursorTop = CY;
                Console.CursorLeft = 0 ;
                foreach ( var t in tokens.NLSendRec("tokens") ) {
                    var col = t.col.NLSend();
                    //Console.ForegroundColor = col ;
                    Console.ForegroundColor = col == ConsoleColor.Black ? ConsoleColor.Gray : col ; // <- hack bis ich den ShellToken typ gefixt hab 
                    Console.Write( t.orig );
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                int blank_out_width =   Math.Max ( 0 , Console.WindowWidth -   Console.CursorLeft -1 ) ; // guesswork 
                Console.Write( SUH.NSpace( blank_out_width )  ) ; //overwrite any junk that might be to the right of this line in the console window 
                Console.CursorLeft = CX;
                Console.CursorVisible = true; 
            }
            public void MoveLeft()  { CX = Math.Max ( 0 , CX-1); Console.CursorLeft = CX ; }
            public void MoveRight() {
                int ocx = CX;
                try { 
                    CX = Math.Min ( CX+1, line.Length); Console.CursorLeft = CX ; // CursorLeft setter throws out of bounds ... sometimes 
                } catch ( Exception e )  {
                    //e.NLSend(" EX in MoveRight");
                    CX = ocx ;
                }
            } // todo clamp against console Window border thingie

            public void InsertAtPoint ( string ins )  {
                ins = SUH.Sanitize( ins );
                line =  new string ( line.Take(CX).ToArray() ) 
                        + ins 
                        + new string ( line.Skip(CX).ToArray() )   ;
                MoveRight();
            }
            public void KillPrevChar ( ) {
                if ( CX == 0 ) return ;
                line = new string ( line.Take(CX -1 ).ToArray() ) + new string ( line.Skip( CX ).ToArray() ) ;  
                MoveLeft();
            }
            public void KillCharAtPoint() {
                line = new string ( line.Take(CX).ToArray() ) + new string ( line.Skip(CX+1).ToArray() ); 
                CX = Math.Min( CX , line.Length); 
            }
            public void Home() {
                CX =0;
            }
            public void End() {
                CX = line.Length;
            }
            public void stat() { /* new { X = CX ,  WinX = Console.CursorLeft , WinY = Console.CursorTop , line = line }.NLSend("stat"); */}  // no NLSend() for now 
        }


    public class ConsoleState {
        public List<TokenLine> Hist = new List<TokenLine>();
        public int hist_pos = 0 ; 
        public int hist_len { get { return Hist.Count() ; } }
        public int global_hor_pos = 0; 
        public TokenLine TL_e = new TokenLine ();  // token line currently edited
        public bool scroll = false; // internal state varies between scrolling through history and editing the current line
        
        // (scroll == true , h_len == 0 ) is illegal state 

        public Func<string,string>                            Exec;
        public Func<string,IEnumerable<ShellToken>>           HighlightDirect;
        
        public Func<string , int, AC_Resp > AC;

        public void DisplayCurrentLine() {
            if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");
            currLine.Write();
        }
        public TokenLine currLine { get { return (scroll && hist_len > 0 ) ? Hist[hist_pos] : TL_e ; } }
        public void StartModify() {
            if ( scroll && hist_len == 0 ) throw new Exception("inv1 bug");
            if ( scroll ) {
                TL_e = Hist[hist_pos].Copy();  // <- this destroys any temporary changes to TL_e that are not committed hist histroy via Enter
                scroll = false;
            } // else do nothing the line to modify already is TL_e
        }
        // TODO: hard limit on hist size 
        public void H_push ( TokenLine TL ) { Hist.Insert ( 0 , TL.Copy() ) ; } 

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
                TokenLine as currently implemented holds the horizontal position of the cursor - each line its own
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
                TL_e = new TokenLine();
                var string_res = Exec( string_arg );
                Console.WriteLine ( "\n" + string_res + "\n" ); 
            }
            else if ( KeyInfo.Key == ConsoleKey.Tab ) {
                StartModify();
                AC_Resp res = AC ( TL_e.line , TL_e.clamped_CX ) ; 
                if ( res.error ) {
                    Console.WriteLine("\n err \n");
                    Console.WriteLine( res.err_string ) ;

                } else { 
                
                    bool modified = TL_e.clamped_CX != res.nu_offs;   // <- this kind of sucks protocols need some serious redesign , atm nu_offs is properly calculated in MainGrammar.Operations but killed in ShellParserGlue because there is no field for it 
                    if ( modified ) {
                        TL_e.SetTokens( res.toks ) ;
                        TL_e.clamped_CX = (int)res.nu_offs;
                    }
                    global_hor_pos = TL_e.clamped_CX;
                    Console.WriteLine();
                    foreach ( var sugg in res.suggs ) Console.WriteLine( sugg );
                }
            }
            else { // actually edit something 
                StartModify();
                TL_e.InsertAtPoint( new string ( new [] {  KeyInfo.KeyChar } ) ) ;
                global_hor_pos = TL_e.clamped_CX;
                
            }

            if ( ! scroll ) TL_e.SetTokens( HighlightDirect( TL_e.line ) );  // <--- this needs to become some kind of merge operation , the tokenization obtained from the sever contains more info, than what can be determined limited local parsing
            DisplayCurrentLine();
        }
    }

    
}
