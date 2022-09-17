using D = System.Diagnostics.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace ParserComb {

    public interface TokLen {
            int len { get; }  
    }
   
    /* 
        TODO probably sound idea 
        complete clusterfuck in current execution 
        rewrite this ... _again_   :/ 

        provisionary solutions are long lived because provisionaries are adopted where there is a lack of motivation to do things properly 

        atm: use this as a way to prototype the _desired_ semantics of the implementation to come  >_< 
    */ 
    

    public class TokLinePlanB<Tok> where Tok: class, TokLen {
        public abstract class NodeBase {
            public bool is_valid = true;   // easiest way to ensure CPos pointers can be checked for validity , if a Node is removed it becomes "invalid" 
            public NodeBase left, right ;
            public NodeBase rightProper { get { 
                var n = this; 
                while (true ) { 
                    var right = n.right; 
                    if ( right == null ) return null ; // behave like basic .right in null case 
                    if ( right.len >0 ) return right ;
                    n = right;                         // skip 0 len tokens - which is the whole point 
                    } 
            } }
            public abstract int len { get; }
            public abstract Tok tok { get; }    // as part of protocol, all functions that ask for the token of a certain position return null if there is no obvious mapping from that position to a token ( Whitespace, Start, End, out of range, ... and whatever else ) 
        }
        public class NodeWS : NodeBase {
            public int _len;
            public override int len { get { return _len ; } }
            public override Tok tok => null;
        }
        public class NodeTok : NodeBase {
            public Tok _tok;
            public override int len { get { return tok.len ; } }
            public override Tok tok => _tok;

        }
        protected NodeWS Start = new NodeWS { _len = 0 };  // special nodes, that have no representation in input string, kinda like "^$" in regex 
        protected NodeWS End   = new NodeWS { _len = 0 };

        public class CPosC {
            /*  internal representation of a "tokenized" position
                (token , i )
                as if the character immediately before a token is used to chose the token part 
                cursor position immediatly after the last char of a token is considered as "on that token" 
                
                this allows for example to have a valid position in an emtpy token-sequence as: (start,0)
                EOL is  :                                                                       (tokens.Last() , tokens.Last().len )    or ( start,0) for empty seq
                ( End , _ ) is invalid as a CPosC

                so yeah: this is very much a lexicographic extension of c-string semantics 

                for disambiguation, this also means: (N , 0 ) is invalid for every N other than Start

                .... as far as i can remember zero length normal tokens were supposed to be allowed - how to normalize position representation in that case ? 
                */
            public TokLinePlanB<Tok> line;
            public NodeBase N ; 
            public int ci_post;     

            public CPosC(TokLinePlanB<Tok> line) { this.line = line; }
            public bool LAdj_isStart             { get { return N == line.Start ;                        } }
            public bool LAdj_isWS                { get { return N is NodeWS && N != line.Start;          } }
            public bool LAdj_isTok               { get { return N is NodeTok;                            } }

            // -> Tok|null   // null iff StringPos() does not correspond to a Token
            public Tok  immediateLAdj_tok        { get { return LAdj_isTok ? (N as NodeTok).tok : null ; } }


            /*
                the token the cursor is blinking over - if any 
            */
            public Tok  insideof_tok                   { get { 
                if ( ci_post >= N.len ) { // this includes ( Start,0)   --  ci_post > N.len is illegal in general, but eh ... 
                    var right = N.rightProper;
                    return right == null ? null : right.tok ;
                }                       
                return N.tok;
                    
            }}

            public bool isValid( TokLinePlanB<Tok> other_line ) { // mainly for unit tests n stuff 
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
            public Tok conflated_clusterF_AC_tok () {   // this conflates behavoiour specific to strats for USER-FACING behaviour of AutoCompletion - TODO deprecate+move elsewhere, or kick entirely 
                if ( LAdj_isTok ) return immediateLAdj_tok ;
                var p2  = LeftAdjTokenPos();
                if ( p2 == null ) return null ;
                return p2.immediateLAdj_tok;
            }
            public int StringPos () {                   
                // das stimmt ueberhaupt nicht !!!! 
                // der Fall ci_post != len ist gar nicht bearbeitet - bis jetzt nicht aufgefallen, weil StringPos nur befragt wird nach erfolgter substitution. 
                // Und dann eben nach der Postiion am Ende eines Tokens 
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
        public CPosC SplitWS ( CPosC CP_at , Tok tok  ) {         // returned new CPos is a the end of the inserted node
            if ( ! CP_at.isValid( this ) ) throw new Exception();
            if ( ! CP_at.LAdj_isWS )            throw new Exception();
            var nu_node = new NodeTok { _tok = tok } ;
            SplitWS( (NodeWS) CP_at.N , nu_node , CP_at.ci_post ) ; 
            return new CPosC (this){ N = nu_node , ci_post = nu_node.len } ; // todo wird wahrscheinlich nicht gebraucht 

        }

        #region public interface 
        /*
            some of these guys invalidate CPos, ( SplitWS unlinks+invalidates  the token CPos sits on ) 
        */
        public void InsertTokAfterNode( NodeBase N , NodeTok nodeTok ) {
            if ( N == End ) throw new Exception();
            var R = N.right;
            var Nu = nodeTok;
            connect( N , Nu ) ;
            connect( Nu , R );
        }
        public void InsertTokAfterNode( NodeBase N , Tok tok ) {
            InsertTokAfterNode(N , new NodeTok { _tok  = tok } );
        }

        public CPosC InsertAfterCPos ( CPosC pos , Tok tok  ) {
            if ( pos.LAdj_isWS ){
                return SplitWS(pos, tok);
            } else { 
                // todo probably sanity check if "pos" is at end of token and throw otherwise 
                InsertTokAfterNode ( pos.N , tok );
                return CPosAtEndOfTok( tok );
            }
        }
        // Todo rewrite InsertTokAfterNode to TokAfterTok - and not expose this function at all 
        public NodeTok findTok ( Tok target ) {
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
            var Nu = new NodeTok { _tok = newTok } ;
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
            Add ( new NodeTok { _tok = tok } );
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

    public static class TokLineTest { 
        class DTT : TokLen {   // Dummy Test Token 
            public string str;
            public int len => str.Length;
        }
        
        public static void TestAll() {
            // convenience for readable testcases, WARNING: does not respect \t n stuff ....
            // argument TokLine is assumed to be empty 
            Func<string [] , TokLinePlanB<DTT>, DTT []> fillFromArgs  = ( args , TokLine ) => {
                
                var L   = new List<DTT>();
                foreach ( var s in args ) 
                    if ( s.All( c => c == ' ' ) )   TokLine.AddWS( s.Length ) ;
                    else                            { var dtt = new DTT { str = s }; TokLine.AddTok( dtt  ) ; L.Add( dtt ) ; }
                return L.ToArray();
            };
            Func<IEnumerable<string>,int> sumLen = ( strs ) => strs.Select( s => s.Length ).Sum();

            var T1_arr     = new [] { "foo" , "  " } ;
            var T1_TokLine = new TokLinePlanB<DTT>();
            var T1_DDT_arr = fillFromArgs ( T1_arr , T1_TokLine) ;

            var T1_I_immediate_after_WS = sumLen ( T1_arr );

            // ---- nail down edge case behaviour : ----

            // [foo   ]
            //        _       | AC_tok() is foo 
            D.Assert ( T1_TokLine.CPosFromStringpos( T1_I_immediate_after_WS ).conflated_clusterF_AC_tok() == T1_DDT_arr[0] );


            // [foo   ]
            //             _   | StringPos is the same as above -- never outside of serialzed stringlen  ( CPosC creation implicitly clamps to special END node -> StringPos is derived from nodepointer ) 
            D.Assert ( T1_TokLine.CPosFromStringpos( T1_I_immediate_after_WS + 5 ).StringPos() == T1_I_immediate_after_WS ) ;


            // [foo  bar]
            //       _                        | AC_tok() is still foo in this case 
            // -> corrolary of things like : 
            // [.%foo.bar]  [.%foo.]          
            //        _            _          | want AC_tok() to point to the second '.' 
            // this behaviour was introduced for cases like : 
            // [.%foo.%bar]
            //       _        | where with this cursor pos one would want to AC on "foo"-token 

            T1_TokLine.AddTok( new DTT { str = "bar" } ) ;
            D.Assert ( T1_TokLine.CPosFromStringpos( T1_I_immediate_after_WS ).conflated_clusterF_AC_tok() == T1_DDT_arr[0] );




            Console.WriteLine( "TokLinePlanB<>  :: ok " ); 
        }
    }
}