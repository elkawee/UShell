using D = System.Diagnostics.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace ParserComb {

    public interface TokLen {
            int len { get; }  
    }
   
    

    public class TokLinePlanB<Tok> where Tok: class, TokLen {
        public abstract class NodeBase {
            public bool is_valid = true;   // easiest way to ensure CPos pointers can be checked for validity , if a Node is removed it becomes "invalid" 
            public NodeBase left, right ;
            public abstract int len { get; }
        }
        public class NodeWS : NodeBase {
            public int _len;
            public override int len { get { return _len ; } }
        }
        public class NodeTok : NodeBase {
            public Tok tok;
            public override int len { get { return tok.len ; } }

        }
        protected NodeWS Start = new NodeWS { _len = 0 };
        protected NodeWS End   = new NodeWS { _len = 0 };

        public class CPosC {
            public TokLinePlanB<Tok> line;
            public NodeBase N ; 
            public int ci_post;

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
            public int StringPos () {
                int cnt=0;
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
        public void SplitWS ( NodeWS origWS , NodeTok insertNode , int ci_post ) {
            
            if ( ci_post == 0 ) throw new Exception();    // CPos invariant 
            if ( origWS.len < ci_post ) throw new Exception();
            int len_R = origWS.len - ci_post;
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

        #region public interface 
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
        #endregion

        void Add( NodeBase N ) {
            var after = End.left;
            connect ( after , N );
            connect ( N , End ) ;
        }
        public void AddWS( int len ) {
            if ( len == 0 ) throw new Exception();
            Add ( new NodeWS { _len = len } ); 
        }
        public void AddTok ( Tok tok ) {
            if ( tok.len == 0 ) throw new Exception();
            Add ( new NodeTok { tok = tok } );
        }
        public CPosC CPosFromStringpos ( int str_pos ) {
            int cnt = 0 ; 
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

        public IEnumerable<RType> Serialize<RType> ( Func<Tok,RType> onTok , Func<int,RType> onWS ) {
            for ( NodeBase N = Start.right ; N != End ; N = N.right ) {
                if( N is NodeWS ) yield return onWS ( N.len );
                if( N is NodeTok ) yield return onTok ( (N as NodeTok).tok );
            }
        }
    }
}