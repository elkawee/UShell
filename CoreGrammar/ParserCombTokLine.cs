using D = System.Diagnostics.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace ParserComb {

    public interface TokLen {
            uint len { get; }  
    }
   

    public class TokLine<Tok> where Tok : class , TokLen   {

        public abstract class NodeBase {
            public NodeBase left, right ;
            public abstract uint len { get; }
        }
        public class NodeWS : NodeBase {
            public uint _len;
            public override uint len { get { return _len ; } }
        }
        public class NodeTok : NodeBase {
            public Tok tok;
            public override uint len { get { return tok.len ; } }

        }
        

        public NodeBase start,end; 
        
        

        public TokLine() {
            NodeWS dummy_start = new NodeWS { _len = 0  };
            start  = dummy_start;
            end    = dummy_start;
            CurPos = new CurPosS { isEOF = true } ;
        }

        public struct CurPosS {
            public NodeBase N;
            public uint offset;
            public bool isTok                 { get { return (!isEOF ) && (N is NodeTok ); } }
            public bool isWhiteSpace          { get { return (!isEOF) && (N is NodeWS) ; } }
            
            public bool isEOF;          // if true: N and offset are meaningless 
            public bool onN ( NodeBase N    ) { return (!isEOF) && ( this.N == N ); }
            public void move_right() {
                if (isEOF ) return;
                if (N.right == null ) { isEOF = true ; return ; }
                N= N.right;
                offset = 0;
            }                  
        }
        public CurPosS CurPos;

        public uint GetStringPos () {
            uint p_counter = 0 ;
            for ( var N = start ; N != null ; N=N.right ) {
                if ( CurPos.onN ( N ) ) return p_counter + CurPos.offset;    // assumes EOF and onN ( anyN ) can't be simultainously true 
                p_counter += N.len ;
            }
            D.Assert( CurPos.isEOF ) ;
            return p_counter;
        }

        // clamps to [0,EOF]
        public void SetStringPos ( int _strpos ) {
            if ( start == null ) throw new Exception();  // invalid state - even the empty imput string is to be represented as toks = [ WS { len = 0 } ] 
            
            if ( _strpos < 0 ) _strpos = 0 ;  // todo: ugly and likely incorrect 
            uint strpos = (uint) _strpos;
            uint counter = 0;
            var N = start;
            while ( true ) {
                if ( strpos < counter + N.len ) {
                    CurPos.N = N;
                    CurPos.isEOF  = false ;
                    CurPos.offset = strpos - counter ;
                    break;
                } else {
                    if ( N.right == null ) {
                        CurPos.isEOF = true; 
                        break;
                    }
                    counter += N.len ;
                    N=N.right;
                }
            }
            
        }
        /// <summary>
        /// the token, that is the entry point for auto complete 
        /// e.g. when cursor on whitespace or EOF: move left until a token is found 
        /// null when no such token exists  
        /// </summary>
        /// <returns>Tok|null</returns>
        public Tok AC_Token () {
            
            var N = CurPos.isEOF ? end : CurPos.N;
            while ( N is NodeWS ) {
                if ( N.left == null ) return null ;
                N = N.left;
            }
            return (N as NodeTok).tok;
        }

        public string Stringamalize (Func<Tok,string> toStr ) {        // todo : unsure about this: pro : doesn't bloat the TokLen interface , contra : unintuitive 
            string R = ""; 
            for ( var N = start; N != null ; N = N.right ) {
                if ( N is NodeWS ) for ( int i =0; i < N.len ; i ++ ) R += " ";
                else R += toStr( ( N as NodeTok ).tok) ;
            }
            return R; 
        }

         
        public void AddTok ( Tok tok )  { var N = new NodeTok { tok  = tok }; Add ( N ); }
        public void AddWS  ( uint len ) { var N = new NodeWS  { _len = len }; Add ( N ); }     
        void Add ( NodeBase N ) { 
            D.Assert( end != null ) ;
            end.right = N ; 
            N.left = end;
            end = N;
            
        }
        NodeTok Find ( Tok tok ) {
            
            for ( NodeBase pos = start; pos != null ; pos = pos.right ) {
                if ( pos is NodeTok && object.ReferenceEquals( (pos as NodeTok ).tok , tok )) return (pos as NodeTok );
            }
            throw new Exception(); // all use cases for Find(tok) assume tok is present  
        }
        public void ReplaceTok ( Tok origTok , Tok nuTok ) {
            
            var N = Find ( origTok );
            if ( CurPos.onN ( N ) ) CurPos.move_right();
            N.tok = nuTok;
        }
        void InsertSplitWS ( NodeWS oldWS , Tok nu_Tok ) {
            D.Assert( CurPos.isWhiteSpace );
            var leftWS  = new NodeWS { _len = CurPos.offset  };
            var rightWS = new NodeWS { _len = oldWS.len - CurPos.offset };
            leftWS.left   = oldWS.left ;
            rightWS.right = oldWS.right;
            var tokN = new NodeTok { tok = nu_Tok , left = leftWS , right = rightWS } ;
            leftWS.right = tokN;
            rightWS.left = tokN;

        }
        public void InsertOrReplaceAtCursor ( Tok nu_Tok ) {
            if ( CurPos.isEOF ) { AddTok( nu_Tok ); return ; } // todo this is double insertino happens completion fucks up 
            if ( CurPos.isTok ) { ReplaceTok( (CurPos.N as NodeTok).tok , nu_Tok ); return ; } 
            InsertSplitWS ( (NodeWS) CurPos.N , nu_Tok ) ; 
        }
        public IEnumerable<T> Serialize<T>( Func<Tok,T> onTok , Func<uint,T> onWS ) {
            for ( var N = start; N != null ; N = N.right ) {
                if ( N is NodeTok ) yield return onTok ( (N as NodeTok).tok ) ;
                else                yield return onWS  ( (N as NodeWS ).len ) ;
            }
        }
        





    }



    public class TokLinePlanB<Tok> where Tok: class, TokLen {
        public abstract class NodeBase {
            public bool is_valid = true;   // easiest way to ensure CPos pointers can be checked for validity , if a Node is removed it becomes "invalid" 
            public NodeBase left, right ;
            public abstract uint len { get; }
        }
        public class NodeWS : NodeBase {
            public uint _len;
            public override uint len { get { return _len ; } }
        }
        public class NodeTok : NodeBase {
            public Tok tok;
            public override uint len { get { return tok.len ; } }

        }
        NodeWS Start = new NodeWS { _len = 0 };
        NodeWS End   = new NodeWS { _len = 0 };

        public class CPosC {
            public TokLinePlanB<Tok> line;
            public NodeBase N ; 
            public uint ci_post;

            public CPosC(TokLinePlanB<Tok> line) { this.line = line; }
            public bool isStart { get { return N == line.Start ; } }
            public bool isWS { get { return N is NodeWS && N != line.Start; } }
            public bool isTok { get { return N is NodeTok; } }
            public Tok  tok { get { return isTok ? (N as NodeTok).tok : null ; } }
            public bool isValid( TokLinePlanB<Tok> other_line ) {
                return other_line== line && 
                    ( N == line.Start || N.is_valid ) ;
            }
            public CPosC LeftAdjTokenPos () { // todo better name for this 
                if ( N == line.Start ) return null ; 
                NodeBase cand = N.left;
                while ( true ) {
                    if ( N == line.Start ) return null; 
                    if ( N is NodeWS ) { N = N.left ; continue; }
                    return new CPosC(line) { N = N , ci_post = N.len } ;
                }
            }
            public Tok AC_tok () {
                if ( isTok ) return tok ;
                var p2  = LeftAdjTokenPos();
                if ( p2 == null ) return null ;
                return p2.tok;
            }
            public uint StringPos () {
                uint cnt=0;
                NodeBase rator = N ;
                while ( rator != line.Start ) { cnt += rator.len ; rator = rator.left ; }
                return cnt;
            }
        }

        public TokLinePlanB () {
            connect ( Start , End );
        }

        static void connect ( NodeBase N1 , NodeBase N2 ) {
            N1.right = N2 ; N2.left = N1;
        }
        public void InsertTokAfterNode( NodeBase N , NodeTok nodeTok ) {
            if ( N == End ) throw new Exception();
            var R = N.right;
            var Nu = nodeTok;
            connect( N , Nu ) ;
            connect( Nu , R );
        }
        public void InsertTokAfterNode( NodeBase N , Tok tok ) {
            InsertTokAfterNode(N , new NodeTok { tok  = tok } );
        }
        public void SplitWS ( NodeWS origWS , NodeTok insertNode , uint ci_post ) {
            
            if ( ci_post == 0 ) throw new Exception();    // CPos invariant 
            if ( origWS.len < ci_post ) throw new Exception();
            uint len_R = origWS.len - ci_post;
            if ( len_R == 0 ) {  InsertTokAfterNode( origWS , insertNode ) ; return ; }
            NodeWS WS_L = new NodeWS { _len = ci_post };
            NodeWS WS_R = new NodeWS { _len = len_R };
            NodeBase L = origWS.left ;
            NodeBase R = origWS.right; 
            NodeTok  Nu = insertNode ;
            connect ( L , WS_L );
            connect ( WS_L , Nu );
            connect ( Nu , WS_R );
            connect ( WS_R , R );

            origWS.is_valid = false ;
        }

        public CPosC SplitWS ( CPosC CP_at , Tok tok  ) {
            if ( ! CP_at.isValid( this ) ) throw new Exception();
            if ( ! CP_at.isWS )            throw new Exception();
            var nu_node = new NodeTok { tok = tok } ;
            SplitWS( (NodeWS) CP_at.N , nu_node , CP_at.ci_post ) ; 
            return new CPosC (this){ N = nu_node , ci_post = nu_node.len } ; // todo wird wahrscheinlich nicht gebraucht 

        }
        public void InsertAfterCPos ( CPosC pos , Tok tok  ) {
            if ( pos.isWS ) SplitWS ( pos , tok );
            else InsertTokAfterNode ( pos.N , tok );
        }

        NodeTok findTok ( Tok target ) {
            for ( NodeBase N = Start ; N != End ; N = N.right ) {
                if ( N is NodeTok && object.ReferenceEquals( (N as NodeTok ).tok , target )) {
                    return (NodeTok) N ;
                }
            }
            throw new Exception("Tok not present"); 
        }

        public void ReplaceTok ( Tok origTok , Tok newTok ) {
            var N = findTok ( origTok ) ;
            N.is_valid = false; 
            var L = N.left;
            var R = N.right;
            var Nu = new NodeTok { tok = newTok } ;
            connect ( L , Nu ) ;
            connect ( Nu , R ); 

        }

        void Add( NodeBase N ) {
            var after = End.left;
            connect ( after , N );
            connect ( N , End ) ;
        }
        public void AddWS( uint len ) {
            if ( len == 0 ) throw new Exception();
            Add ( new NodeWS { _len = len } ); 
        }
        public void AddTok ( Tok tok ) {
            if ( tok.len == 0 ) throw new Exception();
            Add ( new NodeTok { tok = tok } );
        }
        public CPosC CPosFromStringpos ( uint str_pos ) {
            uint cnt = 0 ; 
            NodeBase N = Start;
            
            while ( true ) {
                if ( str_pos <= cnt + N.len ) {
                    return new CPosC(this) { N = N , ci_post = str_pos - cnt };
                }
                if ( N.right == End ) {
                    return new CPosC(this) { N = N , ci_post = N.len } ;
                }
                cnt += N.len ;
                N = N.right;
            }
        }
        public CPosC CPosAtEndOfTok ( Tok tok ) {
            var N = findTok( tok ) ;
            return new CPosC(this) { N = N , ci_post = N.len } ; 
        }

        public IEnumerable<RType> Serialize<RType> ( Func<Tok,RType> onTok , Func<uint,RType> onWS ) {
            for ( NodeBase N = Start.right ; N != End ; N = N.right ) {
                if( N is NodeWS ) yield return onWS ( N.len );
                if( N is NodeTok ) yield return onTok ( (N as NodeTok).tok );
            }
        }
        
        
        

    }




}