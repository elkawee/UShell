
using NLSPlain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using D = System.Diagnostics.Debug;

namespace ParserComb { 

    public class NamedNode {
        public NamedNode []     children  = new NamedNode[0]  ; 
        public NamedNode        parent ; 
        public int              parent_I;
        public TaggedPINodeBase orig;

        public NamedNode        lSib { get { if ( parent_I < 1 ) return null ; else return parent.children[parent_I -1 ] ; } }

        public virtual void build () { }  // this is to defer initialization until the entire AST is present (parent and children fields are set) , called in leaf up order 
    } 

    public static class NamedNodeUtil { // because i want this in unit tests too 
        public static IEnumerable<NamedNode> Flatten( this IEnumerable<NamedNode> arg ) {
            IEnumerable<NamedNode> res = new NamedNode [0];
            foreach ( var A in arg ) {
                res = res.Concat( new [] { A } ) ; // topo sort order ( for P1 ancester P2 --> index(P1) < index(P2 ) ) 
                res = res.Concat( A.children.Flatten() );
            }
            return res; 
        }
        public static IEnumerable<NamedNode> Leafs ( this NamedNode arg )              { return new [] { arg }.Leafs(); }
        public static IEnumerable<NamedNode> Leafs ( this IEnumerable<NamedNode> arg ) {
            return arg.Flatten().Where( A => A.children.Count() == 0  ) ;
        }
        // A -> [ A , ancester(A) , ancester(ancester(A)) , ... ] ; null -> [] 
        public static IEnumerable<NamedNode> PathUp ( this NamedNode arg ) {
            if ( arg!= null ) yield return arg; else yield break;
            while ( arg.parent != null ) { arg = arg.parent ; yield return arg ; } 
        }
        // first in PathUp that matches condition or null 
        public static NamedNode PathUpTo ( this NamedNode arg , Func<NamedNode,bool> condition ) {
            foreach ( var n in PathUp( arg )) { if ( condition(n )) return n; }
            return null;
        }
        
    }

    public class AnonPINode {
        public AnonPINode [] childrenA = new  AnonPINode[0];
    }


    public abstract class TaggedPINodeBase:AnonPINode {
        // Todo sanity : leafs must be TaggedNodes 
        public static IEnumerable<TaggedPINodeBase> flattenRec(IEnumerable<AnonPINode> n_in) {
            var R = new List<TaggedPINodeBase>();
            foreach(var c in n_in) {
                if(c is TaggedPINodeBase) R.Add(c as TaggedPINodeBase);
                else R.AddRange(flattenRec(c.childrenA));
            }
            return R;
        }
        public Object payTN;               // only for terminals 
        public abstract NamedNode gen();

    }

    class TaggedPINode<NNT>:TaggedPINodeBase where NNT : NamedNode, new() {
        public override NamedNode gen() {
            var       NNs    =  flattenRec( childrenA ).Select( TNB => TNB.gen() ) ;
            NamedNode thisNN =  new NNT { children = NNs.ToArray() , orig = this } ;
            int pi=0;
            foreach ( NamedNode ch in thisNN.children ) {  ch.parent = thisNN; ch.parent_I = pi++ ; }
            return thisNN; 
        }
    }



    public class Parser<Token>  {

        public struct itm {
            public AnonPINode n; public IEnumerable<Token> toks;
            public override string ToString() { return new {
                n = n.GetType().IsGenericType ? n.GetType().GetGenericArguments()[0].Name : n.GetType().Name,
                toks = toks
            }.ToString(); }
        }



        public class OrC:PI {
            public PI PI1, PI2;
            public OrC ( PI PI1 , PI PI2 ) { this.PI1 = PI1 ; this.PI2 = PI2; can_epsilon = PI1.can_epsilon || PI2.can_epsilon ; }
            public override IEnumerable<itm> iter(IEnumerable<Token> toks ,  bool suppress_epsilon = false) {
                IEnumerable iter1 = PI1.iter(toks , suppress_epsilon);
                foreach(itm item in iter1) yield return item;
                IEnumerable iter2 = PI2.iter(toks , suppress_epsilon);
                foreach(itm item in iter2) yield return item;
            }
        }
        // todo : i assume OR is associative ? 
        public static OrC OR(PI PI1,PI PI2)               { return new OrC (  PI1,PI2  ); }
        public static OrC OR(PI PI1,PI PI2,PI PI3)        { return OR(PI1 , OR (PI2 , PI3 )); }
        public static OrC OR(PI PI1,PI PI2,PI PI3,PI PI4) { return OR(PI1 , OR (PI2 , OR ( PI3, PI4) )); }
        


        public abstract class PI {
            abstract public IEnumerable<itm> iter(IEnumerable<Token> toks , bool suppress_epsilon = false);
            public bool can_epsilon;
        }

        public class NamedPI_C<NamedNodeT>:PI where NamedNodeT: NamedNode , new() {
            public PI subPI ;
            public override IEnumerable<itm> iter(IEnumerable<Token> toks , bool suppress_epsilon = false) {
                foreach (itm it in subPI.iter( toks , suppress_epsilon ) ){
                    var N = new TaggedPINode<NamedNodeT> { childrenA = new AnonPINode [] { it.n } } ;
                    yield return new itm { n = N , toks = it.toks };
                }
            }
        }
        public static PI Prod<NamedNodeT> ( PI pi ) where NamedNodeT : NamedNode , new () {
            return new NamedPI_C<NamedNodeT> { subPI = pi , can_epsilon =pi.can_epsilon } ;
        } 




        #region Terminals 
        public class TermNode : NamedNode {
            public Token tok; 
            public override void build() { tok = (Token) orig.payTN ;}
        }

        public class TermPI_C : PI {
            public Token alpha;  
            public override IEnumerable<itm> iter(IEnumerable<Token> toks , bool suppress_epsilon = false ) {
                if ( ! toks.Any()) yield break;
                // hmm? there is auto boxing for immediate types ? 
                if ( toks.First().Equals ( alpha ) )
                    yield return  new itm { n = new TaggedPINode<TermNode> { payTN = toks.First() } , toks = toks.Skip(1) } ;
            }
        }
        public static TermPI_C Term( Token alpha ) { return new TermPI_C { alpha = alpha , can_epsilon = false } ; }
        
        public abstract class TerminalMatch_PI_C : PI { /* todo less cryptic name */ 
            public abstract bool match ( Token _ );
            public override IEnumerable<itm> iter(IEnumerable<Token> toks , bool suppress_epsilon = false ) {
                if ( ! toks.Any()) yield break;
                Token cand = toks.First();
                if ( match (cand ) ) yield return new itm { n = new TaggedPINode<TermNode> { payTN = cand }, toks = toks.Skip(1) }.NLSend() ;
            }
        }
        #endregion

        #region SEQ
        /*
        public class Seq_LG_PI_C : PI {
            struct tbl_entry { public AnonPINode node_L ; public IEnumerator<itm> rator_R ;}
            public PI sub_L , sub_R ;
            public override IEnumerable<itm> iter(IEnumerable<Token> toks) {
                var table = new LinkedList<tbl_entry> () ;
                foreach ( var itm_L in sub_L.iter(toks ) ) {
                    table.AddLast( new tbl_entry { node_L = itm_L.n , rator_R = sub_R.iter( itm_L.toks ).GetEnumerator() } );
                }
                while ( table.Any() ) {
                    for ( LinkedListNode<tbl_entry> tbl_it = table.First ; tbl_it != null ; ) {
                        var rator_R = tbl_it.Value.rator_R;
                        if ( rator_R.MoveNext() ) {              // if RHS iter has a match yield a compound node 
                            var itmR = rator_R.Current;
                            var node_L = tbl_it.Value.node_L;
                            yield return new itm {
                                n    = new AnonPINode { childrenA = new [] { node_L , itmR.n } } ,
                                toks = itmR.toks
                            };
                            tbl_it = tbl_it.Next;
                        } else {                                // else kick LHS cand match - all possible RHS matches are exhausted 
                            LinkedListNode<tbl_entry> dummy = tbl_it ;
                            tbl_it = tbl_it.Next;
                            table.Remove( dummy );
                        }
                    }
                }
            }
        }
        
        public static Seq_LG_PI_C SEQ ( PI p1 , PI p2 ) {
            return new Seq_LG_PI_C { sub_L = p1 , sub_R = p2 } ; 
        }
        public static Seq_LG_PI_C SEQ ( PI p1 , PI p2 , PI p3 ) {
            return SEQ ( p1 , SEQ ( p2 , p3 ));
        }
        */

        public class Seq_RG_PI_C : PI {
            public PI sub_L , sub_R;
            public Seq_RG_PI_C ( PI sub_L , PI sub_R ) {
                this.sub_L  = sub_L ; this.sub_R = sub_R ; 
                can_epsilon = sub_L.can_epsilon && sub_R.can_epsilon;
            }
            public override IEnumerable<itm> iter(IEnumerable<Token> toks , bool suppress_epsilon = false ) {
                foreach ( itm itm1 in sub_L.iter( toks , suppress_epsilon && can_epsilon)  ) {
                    foreach ( itm itm2 in sub_R.iter ( itm1.toks , suppress_epsilon && can_epsilon) ) {
                        yield return new itm {
                            n    = new AnonPINode { childrenA = new [] { itm1.n , itm2.n } },
                            toks =  itm2.toks
                        }.NLSend("SeqItm");
                    }
                }
            }
        }
        public static Seq_RG_PI_C SEQ ( PI p1 , PI p2 ) {
            return new Seq_RG_PI_C (  p1 , p2 ) ; 
        }
        public static Seq_RG_PI_C SEQ ( PI p1 , PI p2 , PI p3 ) {
            return SEQ ( p1 , SEQ ( p2 , p3 ));
        }

        #endregion

        #region STAR

        /* 
        public class STAR_PI_C : PI {
            public PI sub_PI;

            public IEnumerable<itm> rator_transpose ( IEnumerable<IEnumerable<itm>> L_arg ) {
                var L = new LinkedList<IEnumerator<itm>>( L_arg.Select( subL => subL.GetEnumerator() ));
                while ( L.Any() ) {
                    for ( LinkedListNode<IEnumerator<itm>> L_iter = L.First ; L_iter != null ; ) {
                        var curr_rator = L_iter.Value;
                        if ( curr_rator.MoveNext() ) {
                            yield return curr_rator.Current ;
                            L_iter = L_iter.Next;
                        } else {
                            var dummy = L_iter; 
                            L_iter = L_iter.Next;
                            L.Remove( dummy ) ;
                        }
                    }
                    
                }
            }
            
            public IEnumerable<itm> starStep( IEnumerable<itm> L_in ) {
                IEnumerable<IEnumerable<itm>> dummyApplied = L_in.Select ( 
                    ( it_L_in ) => 
                        sub_PI.iter( it_L_in.toks ).Select( (itm it_sub) => 
                            new itm { n = new AnonPINode { childrenA = new [] { it_L_in.n ,it_sub.n } }, toks = it_sub.toks } ) );
                List<itm> L_self = rator_transpose ( dummyApplied ).ToList() ;
                if ( ! L_self.Any() ) yield break;
                foreach ( var it in starStep ( L_self ) ) yield return it ;   // want maximum greedyness 
                foreach ( var it in L_self ) yield return it ; 
                    
            }
            public override IEnumerable<itm> iter(IEnumerable<Token> toks) {
                if ( sub_PI is STAR_PI_C ) throw new Exception ( "A** might not terminate" );
                // hack ( epsilon matches in inner star ) solving this properly prob. needs a whole pre processing phase  
                // it's also not enough : star ( seq ( star  , ... )) and star ( or ( ... , star , ... )) has the same problem 
                var L = sub_PI.iter(toks).ToList();
                foreach ( itm it in starStep( L )) yield return it;
                foreach ( itm it in L ) yield return it ; 
                yield return new itm { n = new AnonPINode () , toks = toks };  // yield epsilon at the very last  
            }
        }

        public static PI STAR ( PI sub_PI ) { return new STAR_PI_C { sub_PI = sub_PI } ; }
        */
        public static PI STAR ( PI sub_pi ) { return OR( PLUS ( sub_pi )  , EPSILON() ); }
        
        #endregion

        #region PLUS 
        public class PLUS_PI_C : PI {
            public PI sub_PI;
            public PLUS_PI_C ( PI sub_PI ) { this.sub_PI = sub_PI ; can_epsilon = sub_PI.can_epsilon ; }
            public override IEnumerable<itm> iter(IEnumerable<Token> toks,bool suppress_epsilon = false) {
                foreach ( var itm1 in sub_PI.iter( toks , suppress_epsilon: true ) ) {
                    foreach ( var itm2 in iter( itm1.toks , suppress_epsilon: true ) ) {
                        yield return 
                            new itm { 
                                n = new AnonPINode { childrenA = new [] { itm1.n , itm2.n } },
                                toks = itm2.toks
                            };
                    }
                    yield return itm1;
                }
                /*
                foreach ( var itm1 in sub_PI.iter( toks , suppress_epsilon: true ) ) {
                    yield return itm1 ;
                }
                */
            }
        }
        public static PLUS_PI_C  PLUS ( PI sub_PI ) { return new PLUS_PI_C( sub_PI ) ; }
        #endregion

        #region EPSILON 
        public class EPSILON_PI_C : PI {
            public override IEnumerable<itm> iter(IEnumerable<Token> toks,bool suppress_epsilon = false) {
                if ( suppress_epsilon ) yield break ;
                else yield return new itm {
                    n = new AnonPINode { childrenA = new AnonPINode[0] },
                    toks = toks
                };
            }
        }
        public static EPSILON_PI_C EPSILON() { return new EPSILON_PI_C { can_epsilon = true }; }
        #endregion

        #region RUN
        public struct parse_match {
            public NamedNode N;
            public IEnumerable<Token> rest;
        }
        static void buildRec( NamedNode n ) {
            foreach ( var ch in n.children ) buildRec( ch );
            n.build();
        }
        /*
            yields all matching alternatives, including those that do not fully consume the input 
        */
        public static IEnumerable<parse_match> RUN_with_rest ( PI startProd , IEnumerable<Token> toks ) {
            foreach ( itm it in startProd.iter(toks ) ) {
                TaggedPINodeBase start_ast = ( TaggedPINodeBase ) it.n;     // todo : potentially throws . atm there is no PI subtype to denote "only PIs that yield tagged nodes"
                NamedNode        NN        = start_ast.gen();
                buildRec( NN );
                yield return new parse_match { N = NN , rest = it.toks };
            }
        }
        #endregion

     
    }

    

}