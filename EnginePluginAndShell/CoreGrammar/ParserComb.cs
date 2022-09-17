
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

        public struct alt {
            public AnonPINode n; public IEnumerable<Token> toks;
            public override string ToString() { return new {
                n = n.GetType().IsGenericType ? n.GetType().GetGenericArguments()[0].Name : n.GetType().Name,
                toks = toks
            }.ToString(); }
        }



        public class OrC:PI {
            public PI PI1, PI2;
            public OrC ( PI PI1 , PI PI2 ) { this.PI1 = PI1 ; this.PI2 = PI2; can_epsilon = PI1.can_epsilon || PI2.can_epsilon ; }
            public override IEnumerable<alt> iter(IEnumerable<Token> toks ,  bool suppress_epsilon = false) {
                IEnumerable iter1 = PI1.iter(toks , suppress_epsilon);
                foreach(alt item in iter1) yield return item;
                IEnumerable iter2 = PI2.iter(toks , suppress_epsilon);
                foreach(alt item in iter2) yield return item;
            }
        }
        // todo : i assume OR is associative ? 
        public static OrC OR(PI PI1,PI PI2)                        { return new OrC (  PI1,PI2  ); }
        public static OrC OR(PI PI1,PI PI2,PI PI3)                 { return OR(PI1 , OR (PI2 , PI3 )); }
        public static OrC OR(PI PI1,PI PI2,PI PI3,PI PI4)          { return OR(PI1 , OR (PI2 , OR ( PI3, PI4) )); }
        public static OrC OR(PI PI1,PI PI2,PI PI3,PI PI4 , PI PI5) { return OR(PI1 , OR (PI2 , OR ( PI3, OR( PI4, PI5 )) )); }
        


        public abstract class PI {
            abstract public IEnumerable<alt> iter(IEnumerable<Token> toks , bool suppress_epsilon = false);
            public bool can_epsilon;
        }

        public class NamedPI_C<NamedNodeT>:PI where NamedNodeT: NamedNode , new() {
            public PI subPI ;
            public override IEnumerable<alt> iter(IEnumerable<Token> toks , bool suppress_epsilon = false) {
                foreach (alt it in subPI.iter( toks , suppress_epsilon ) ){
                    var N = new TaggedPINode<NamedNodeT> { childrenA = new AnonPINode [] { it.n } } ;                // for each alt, yield one wrapper node ( type-tagged ) with the subPI's node as the only chlid 
                    yield return new alt { n = N , toks = it.toks };
                }
            }
        }
        public static PI Prod<NamedNodeT> ( PI pi ) where NamedNodeT : NamedNode , new () {
            return new NamedPI_C<NamedNodeT> { subPI = pi , can_epsilon =pi.can_epsilon } ;
        } 
        public abstract class PI_defer : PI {public PI deferred_subPI ; }
        public class NamedPIDefer_C<NamedNodeT> : PI_defer where NamedNodeT : NamedNode , new () {
            
            public override IEnumerable<alt> iter(IEnumerable<Token> toks , bool suppress_epsilon = false) {
                if ( deferred_subPI == null ) throw new Exception("trying to run a deferred Prod without assigning it");
                foreach (alt it in deferred_subPI.iter( toks , suppress_epsilon ) ){
                    var N = new TaggedPINode<NamedNodeT> { childrenA = new AnonPINode [] { it.n } } ;                // for each alt, yield one wrapper node ( type-tagged ) with the subPI's node as the only chlid 
                    yield return new alt { n = N , toks = it.toks };
                }
            }
            
        }
        public static NamedPIDefer_C<NamedNodeT> MKProdDefer<NamedNodeT> ( ) where NamedNodeT : NamedNode , new () => new NamedPIDefer_C<NamedNodeT>();
        public static PI                         SETProdDefer            (PI_defer deferredPI , PI rhs_PI ) { deferredPI.deferred_subPI = rhs_PI ; return rhs_PI ; }
        

        #region Terminals 
        public class TermNode : NamedNode {
            public Token tok; 
            public override void build() { tok = (Token) orig.payTN ;}
        }

        public class TermPI_C : PI {
            public Token alpha;  
            public override IEnumerable<alt> iter(IEnumerable<Token> toks , bool suppress_epsilon = false ) {
                if ( ! toks.Any()) yield break;
                // hmm? there is auto boxing for immediate types ? 
                if ( toks.First().Equals ( alpha ) )
                    yield return  new alt { n = new TaggedPINode<TermNode> { payTN = toks.First() } , toks = toks.Skip(1) } ;
            }
        }
        public static TermPI_C Term( Token alpha ) { return new TermPI_C { alpha = alpha , can_epsilon = false } ; }
        
      

        

        #endregion

      
        #region SEQ
      
        public class Seq_RG_PI_C : PI {
            public PI sub_L , sub_R;
            public Seq_RG_PI_C ( PI sub_L , PI sub_R ) {
                this.sub_L  = sub_L ; this.sub_R = sub_R ; 
                can_epsilon = sub_L.can_epsilon && sub_R.can_epsilon;
            }
            public override IEnumerable<alt> iter(IEnumerable<Token> toks , bool suppress_epsilon = false ) {
                foreach ( alt itm1 in sub_L.iter( toks , suppress_epsilon && can_epsilon)  ) {
                    foreach ( alt itm2 in sub_R.iter ( itm1.toks , suppress_epsilon && can_epsilon) ) {
                        yield return new alt {
                            n    = new AnonPINode { childrenA = new [] { itm1.n , itm2.n } },
                            toks =  itm2.toks
                        };
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
        public static Seq_RG_PI_C SEQ ( PI p1 , PI p2 , PI p3 , PI p4 ) {
            return SEQ ( p1 , SEQ ( p2 , SEQ (p3, p4 ) ));
        }

        #endregion

        #region STAR

        public static PI STAR ( PI sub_pi ) { return OR( PLUS ( sub_pi )  , EPSILON() ); }
        
        #endregion

        #region PLUS 
        public class PLUS_PI_C : PI {
            public PI sub_PI;
            public PLUS_PI_C ( PI sub_PI ) { this.sub_PI = sub_PI ; can_epsilon = sub_PI.can_epsilon ; }
            public override IEnumerable<alt> iter(IEnumerable<Token> toks,bool suppress_epsilon = false) {
                foreach ( var itm1 in sub_PI.iter( toks , suppress_epsilon: true ) ) {
                    foreach ( var itm2 in iter( itm1.toks , suppress_epsilon: true ) ) {
                        yield return 
                            new alt { 
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
            public override IEnumerable<alt> iter(IEnumerable<Token> toks,bool suppress_epsilon = false) {
                if ( suppress_epsilon ) yield break ;
                else yield return new alt {
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
            foreach ( alt it in startProd.iter(toks ) ) {
                TaggedPINodeBase start_ast = ( TaggedPINodeBase ) it.n;     // todo : potentially throws . atm there is no PI subtype to denote "only PIs that yield tagged nodes"
                NamedNode        NN        = start_ast.gen();
                buildRec( NN );
                yield return new parse_match { N = NN , rest = it.toks };
            }
        }
        #endregion

     
    }


    /*
        the primitve type to describe terminals to match against does not neccessarily need to be 
        the token type itself 
        for use cases like nameToken := ( someEnum , stringPayload ) 
        match only against the enum
        if terminals were to be defined  in terms of "nameToken"  Match(nameToken nt ) would need an override of Equals, 
            or other similarly ugly cargo culting 
    */
    public abstract class ParserTM<Tok,TokM> : Parser<Tok> { 
        

        public static Func<Tok,TokM,bool> TokMatch ;  // as Delegate because can't override statics - introducing instances would be even more ass backwards 

        public class TerminalMatch_PI_C : PI { /* todo less cryptic name */ 
            public TokM other;
            public override IEnumerable<alt> iter(IEnumerable<Tok> toks , bool suppress_epsilon = false ) {
                if ( ! toks.Any()) yield break;
                Tok cand = toks.First();
                bool matched = false ;
                try { 
                    matched = TokMatch (cand , other );
                } catch ( NullReferenceException ) { throw new Exception("to use TermP, 'TokMatch' delegate must be set" ); }

                if ( matched ) yield return new alt { n = new TaggedPINode<TermNode> { payTN = cand }, toks = toks.Skip(1) }/* .NLSend() */ ;
            }
        }
        public static PI TermP ( TokM other ) {
            return new TerminalMatch_PI_C { other = other } ;
        }
    }

    
    // --------------------------------- 
    
    public static class Tests { 
        
        
        public class Gramm1 : ParserTM<char , char> {
            static Gramm1() { 
                TokMatch = (char_tok , char_gramm) => char_tok == char_gramm ; 
            }
            

            public class PrimENode : NamedNode {} 
            public static PI PrimEdge = Prod<PrimENode> ( TermP('E') );

            public class FanNode : NamedNode {}
            public static PI_defer Fan = MKProdDefer<FanNode>();

            public class PrimStepNode : NamedNode {} 
            public static PI PrimStep = Prod<PrimStepNode> ( OR ( PrimEdge , Fan ) );

            public class BasicFanContentNode : NamedNode {} 
            public static PI BasicFanContent = Prod<BasicFanContentNode>( SEQ ( PrimStep , STAR( PrimStep )) );

            public static PI _Fan = SETProdDefer ( Fan , SEQ ( 
                                                                TermP('{') , 
                                                                BasicFanContent , 
                                                                STAR ( SEQ ( TermP(',' ), BasicFanContent)  ) , 
                                                                TermP('}') 
                                                                ));

            public class StartNode : NamedNode {} 
            public static PI Start = Prod<StartNode> ( SEQ( PrimStep , STAR( PrimStep) ) );


            public static StartNode run_unique ( string sentence ) {
                var matches = RUN_with_rest(Start , sentence ) ;
                var full_matches = matches.Where( match => match.rest.Count() == 0 ).ToArray() ;
                if ( ( ! full_matches.Any() ) || full_matches.Count() != 1 ) throw new Exception("could not match uniquely" ) ;
                return (StartNode)full_matches.First().N;
            }
        
        }

        public static void TestMutualRecursion () {
            Console.WriteLine ( Gramm1.run_unique("E{E}") );
            Console.WriteLine ( Gramm1.run_unique("E{{E}}") );
            Console.WriteLine ( Gramm1.run_unique("E{{{E}}}") );
            Console.WriteLine ( Gramm1.run_unique("E{{{E,EE,E{E}}}}") );
        }



    }
    

}