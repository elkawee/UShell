
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ParserComb;
using System.Reflection;



using D = System.Diagnostics.Debug;

using NLSPlain;
using System.IO;


namespace MainGrammar {

    public enum PTokE {
        OP_GT,OP_doubleGT,OP_dot,OP_percent,OP_star,OP_colon,OP_special_prop,OP_slash,OP_comma,
        OP_arrow_left , OP_arrow_right , 
        OP_equals ,
        OP_sharp , OP_dollar , 
        CS_name,
        squareBRL,squareBRR,
        curlyBRL,curlyBRR,
        plainBRL,plainBRR, 
        ErrT,
        JSON
    }

    public class PTokBase { }
    public class PTokWhitespace : PTokBase { 
        public int len ;
        public override string ToString() => "WS|"+len;
        
    }


    public class PTok : PTokBase , TokLen {
        public PTokE E;
        public string pay;
        public int len { get { return (int)pay.Length; } }
        public override string ToString() => E.ToString()+"|"+pay;
        
    }
    public class PTokJSON : PTok  {
        public object payJSON;
    }


    public class MainGrammar : ParserTM<PTok,PTokE> {
        public class NoACPossible : Exception { }  // typing and other exceptions in RX productions 
        public class TypingException : Exception { public TypingException(string arg ) :base(arg ){} } 

        #region type_complete
        static MainGrammar() { TokMatch = ( tok , ptoke ) => tok.E == ptoke ; }
        #endregion

        // as callbacks, because these things need typing to be done beforehand. Typing design details would radiate into this otherwise 
        public interface ACable { }
      
        public interface ACableMemb : ACable {
            // warum nicht einfach  :: callback -> type  , und den ganzen SuggTree/MembFilter-Kram hier drin? 
            Func<Type> ACMembTypingCallback {get;set;}
        }
        public interface ACableTypeName : ACable {  // typename completion is context free, in a sense. No callback needed
        }

        #region decl

        public class DeclNode : NamedNode {
            public string name ;
            public override void build () { name = ((TermNode)children[1]).tok.pay ; }
        }

        public static PI Decl = Prod<DeclNode> ( SEQ ( TermP ( PTokE.OP_arrow_right ) , TermP( PTokE.CS_name ) ));

        public class DeclStarNode : NamedNode {
            public string [] decls ; 
            public override void build() { decls = children.Select( ch => (ch as DeclNode ).name ).ToArray()  ; }
        }
        public static PI DeclStar = Prod<DeclStarNode> ( STAR (Decl ) );

        #endregion

        #region MemA
        public class MemANode : NamedNode {
            public enum kindE { any , val_field , ref_field , property };
            public kindE kind = kindE.any;
            public string name ;

            public static kindE kindE_from_PTokE ( PTokE ptokE ) {  // this does not destinguish between prop and special_prop 
                switch ( ptokE ) {
                    case PTokE.OP_dot:          return kindE.val_field;
                    case PTokE.OP_star:         return kindE.ref_field; 
                    case PTokE.OP_percent:      return kindE.property;
                    case PTokE.OP_special_prop: return kindE.property;
                    default: throw new Exception();
                }
            }
            
            public override void build() {
                
                if ( children.Length == 2 ) {              // unqualified
                    kind = kindE.any;
                    name = TermPay( children[1]);
                } else if ( children.Length == 3 ) {       // qualified 
                    kind = kindE_from_PTokE ( TermEnum( children[1] ) );
                    name = TermPay( children[2] );
                } else throw new Exception();
            }
        }
        public static PI MemA  = Prod<MemANode> ( SEQ ( TermP ( PTokE.OP_dot ) ,
                                                        OR ( TermP ( PTokE.OP_dot ) ,
                                                             TermP ( PTokE.OP_star ) ,  
                                                             TermP ( PTokE.OP_percent ),
                                                             TermP ( PTokE.OP_special_prop ),
                                                             EPSILON()
                                                            ),
                                                        TermP ( PTokE.CS_name ) ));
        public class MemAVTNode : NamedNode {
            public string[] decls ;
            public string name ;
            public override void build() {
                name =  ((MemANode)children[0]).name ;
                decls = children.Length > 1 ? (children[1] as DeclStarNode).decls : new string[0]; // interessante frage , wenn eine Node keine tokens konsumiert ist sie dann doch da? 
            }
        }
        public static PI MemAVT = Prod<MemAVTNode> ( SEQ ( MemA , DeclStar));

        // -- RX --


        

        #endregion

        #region refs 
        public abstract class RefNode : NamedNode { public abstract string name { get; } }
        public class SharpRefNode : RefNode {
            public override string name => TermPay(children[1]);
            public override void build () {} 
        }
        public class DollarRefNode : RefNode {
            public override string name => TermPay(children[1]);

            public override void build () { } 
        }
        
        public static PI SharpRef  = Prod<SharpRefNode > ( SEQ (  TermP( PTokE.OP_sharp ) , TermP  ( PTokE.CS_name )) );
        public static PI DollarRef = Prod<DollarRefNode> ( SEQ (  TermP( PTokE.OP_dollar ) , TermP  ( PTokE.CS_name )) );

        //public static PI
       
        #endregion

        #region assign

        public class SingleAssignNode : NamedNode {
            public enum typeE { sharp , dollar , json }
            public typeE type ;
            public string name ; 
            public object JSonVal ;  // atm this is PatchedLJ.JsonValue - but i don't want to have the decision on json library bound here 
            public override void build ( ) {
                NamedNode rhs = children[1];
                if ( rhs is SharpRefNode )  { name = (rhs as SharpRefNode  ).name ; type = typeE.sharp  ; return ;}
                if ( rhs is DollarRefNode ) { name = (rhs as DollarRefNode ).name ; type = typeE.dollar ; return ; }

                if ( rhs is TermNode && TermEnum( rhs) == PTokE.JSON ) {
                    type = typeE.json;
                    JSonVal = ((rhs as TermNode).tok as PTokJSON).payJSON;
                    return ;
                }
                throw new NotImplementedException();
            }
        }

        public static  PI SingleAssign = Prod<SingleAssignNode> ( 
            SEQ ( TermP( PTokE.OP_arrow_left ) ,
                OR ( SharpRef , DollarRef , TermP( PTokE.JSON ) ) ) );

        public class AssignVTNode : NamedNode {
            public SingleAssignNode SAN ; 
            public string [] decls ;
            public override void build() {
                SAN = (SingleAssignNode)children[0];
                decls = children.Length >1 ? (children[1] as DeclStarNode).decls : new string[0] ;
            }
        }
        public static PI AssignVT = Prod<AssignVTNode>( SEQ ( SingleAssign , DeclStar));

        #endregion

        public class RG_EdgeNode : NamedNode {
            public MemAVTNode    memAVT;
            public AssignVTNode  assignVT;
            public override void build() {
                memAVT = (MemAVTNode)children[0];
                if ( children.Length == 2 ) assignVT = (AssignVTNode) children[1];
                if ( children.Length >  2 ) throw new Exception();
            }
        }

        public static PI RG_Edge = Prod<RG_EdgeNode> ( SEQ ( MemAVT , OR ( AssignVT , EPSILON() ) ));

        public class TypeNameNode : NamedNode,ACableTypeName {
            
            public string [] names ;
            public override void build() => names = children.Where( nn => (nn as TermNode).tok.E == PTokE.CS_name ).Select ( TermPay ).ToArray();
        }
        // TODO qualified typenames, a la :      :Namespace/TN1/TN2    ( / instead of . for disambiguation with member access ) 
        public static PI TypeName = Prod<TypeNameNode> ( SEQ ( TermP( PTokE.OP_colon ) , TermP( PTokE.CS_name ) , 
                                                         STAR ( SEQ ( TermP ( PTokE.OP_slash ) , TermP( PTokE.CS_name  ) ))
                                                        ) ) ;
        

        public class SG_EdgeNode : NamedNode {
            public enum kindE { immediate , all }
            public kindE kind;
            public string [] typefilter = null ;
            public override void build() {
                kind = TermEnum( children[0] ) == PTokE.OP_GT ? kindE.immediate : kindE.all ;
                if ( children.Length == 2 )  typefilter = (children[1] as TypeNameNode ).names;
                if ( children.Length >  2 )  throw new Exception();
            }
        }
        
        public static PI SG_Edge = Prod<SG_EdgeNode> ( 
            SEQ ( OR ( TermP(PTokE.OP_doubleGT) , TermP ( PTokE.OP_GT )   ) , 
                  OR ( TypeName , EPSILON() )                                 // <- plugging this in directly here makes the grammar abigous, but eh ... 
                ) ) ;



        #region FAN
        public static PI_defer  Fan = MKProdDefer<FanNode>();

        public class FanElemNode : NamedNode {
            public RG_EdgeNode [] rgEdges;
            public override void build() => rgEdges = children.Select( ch => ( RG_EdgeNode) ch ).ToArray();  
        }
        public static PI FanElem = Prod<FanElemNode> ( PLUS ( OR ( RG_Edge , SG_Edge , Fan ) ));

        public class FanNode : NamedNode {
            public FanElemNode [] elems ;
            public override void build() => elems = children.Where( ch => ! (ch is TermNode )) .Select( ch => ( FanElemNode) ch ).ToArray();  
        }
        
        static             PI  _Fan = SETProdDefer( Fan , 
            SEQ ( 
                TermP( PTokE.curlyBRL ) ,
                SEQ ( FanElem , STAR ( SEQ ( TermP( PTokE.OP_comma ) , FanElem ))),
                TermP( PTokE.curlyBRR) ) );
        

        #endregion
        
        #region Filter

        public class EqualsFilterNode : NamedNode {
            public bool isRef      => children[1] is RefNode      ;
            public bool isSharpRef => children[1] is SharpRefNode ;
            public bool isDollarRef=> children[1] is DollarRefNode ; 
            
            public RefNode RHS_ref => isRef? children[1] as RefNode : null ;
            public string  json    => isRef? null : TermPay( children[1] ) ;
        }
        public static PI EqualsFilter = Prod<EqualsFilterNode>( 
            SEQ ( TermP ( PTokE.OP_equals) , 
                  OR    ( SharpRef , DollarRef , TermP( PTokE.JSON ) ) 
                ));


        public class FilterNode : NamedNode {
        }
        public static PI Filter = Prod<FilterNode> ( 
            OR ( TypeName , 
                 EqualsFilter ) );

        #endregion 
                       
        public class PrimitiveStepNode : NamedNode {
            public AssignVTNode[] assigns;
            public DeclStarNode   primary_decl_node;
            public override void build()
            {
                // DeclStarNode is always present - might have consumed empty 
                primary_decl_node = (DeclStarNode)children[1];
                assigns           = children.Skip(2).Select( ch => (AssignVTNode) ch ).ToArray(); // hard cast as sanity check towards assumptions wrt AST-shape 
            }
        } 
        
        /* 
         * assignment of GameObjects or Components are a special, depending on the Assign-value-target 
         *   GO .*field <- GO          // simply assign a ref 
         *   GO .*field <- Component   // ditto 
         *   GO <- GO                  // add Child ?? 
         *   GO <- Component           // CreateComponent() 
         * 
         * for now, SG_Edge is not a valid Assign-value-target ( Assigning a Component to a GameObject needs special treatment ) 
         * - MemA is   -- always is
         * - Filter is -- depending on what's left of it 
         * - Fan       -- like Filter 
         */
        public static PI PrimitiveStep = Prod<PrimitiveStepNode> ( SEQ ( 
                OR (    SG_Edge , 
                        MemA    , 
                        Filter  , 
                        Fan 
                        // Todo : VarRef 
                        ) , 
                DeclStar ,
                STAR ( AssignVT )     // AssignVT includes DeclStar
                ) );

        #region Start
        public class ProvStartNode : NamedNode {
            public SG_EdgeNode startSG ;
            public PrimitiveStepNode [] primSteps ;
            public override void build() { 
                startSG = (SG_EdgeNode) children[0];
                primSteps = children.Skip(1).Select( nn => (PrimitiveStepNode) nn ).ToArray();
            }

        }

        // Subquery : must have non empty LHS , PLUS( PrimitiveStep ) 

        public static PI ProvStart = Prod<ProvStartNode> ( SEQ ( SG_Edge , STAR ( PrimitiveStep ))); // todo : or open with wariable ref 
        
        #endregion 


        #region UTIL
        public class NNShapeException : Exception { }

        // todo : as Extension method ?? 
        public static PTokE TermEnum ( NamedNode N ) {
            try { return (N as TermNode).tok.E ;} catch ( Exception ) { throw new NNShapeException() ;}
        }
        public static string TermPay ( NamedNode N ) {
            try { return (N as TermNode).tok.pay ;} catch ( Exception ) { throw new NNShapeException() ;}
        }
        public static PTok TermTok ( NamedNode N ) {
            try { return (N as TermNode).tok ;} catch ( Exception ) { throw new NNShapeException() ;}
        }

        #endregion 


    }

    


    public class Lexer {

        const string cSharp_basic_identifierS     = @"(?'pay'[\w_0-9]+)";    // todo
        const string SingleCharOpS = @"(?'pay'%|\*|\.|:|\[|\]|\{|\}|\(|\)|/|\$|#|,)";

        const string SG_Operator_S                = @"(?'pay'>{1,2})";                        // andere idee : @"((?'pay'>{1,3})[^>]|$)"  , aktuelle variante matcht auch >>>> , bin noch nicht sicher, ob das ein Problem ist 
        const string SpecialPropOP_S              = @"(?'pay'%!)";
        const string WhitespaceS                  = @"(?'pay'\s+)"   ;
        const string JSonLiteral                  = @"(?'pay'@)" ; 
        const string AssignmentOP_S               = @"(?'pay'<-|<=)";
        const string DeclOP_S                     = @"(?'pay'->)";
        const string Equals_Operator_S            = @"(?'pay'==)";
        
        /*
            relaxed tokenizes every string , stuff that would be otherwise not tokenizable is included as special Error Tokens 
            some extra shizzle for json literals too , but i forgot 
        */
        public static PTokBase[] Tokenize ( string str_in , bool relaxed = false ) { 


            int    arg_offs_S   = 0   ; // if successful match [arg_offs_S , arg_offs ) is the interval of indices that hold the matched value in the original string
            int    arg_offs_E   = 0   ;
            string rest         = str_in ;
            string payl = null; // <- always set to null on non match, which the rest of the implementation must consider invalid 
           
            List<PTokBase> R = new List<PTokBase>();

            Func<string ,  bool > Eat = (  RE ) => { 
                
                Match M = Regex.Match(rest,@"^"+RE+@"(?'REST'.*)");
                if ( M.Success ) {
                    payl = M.Groups["pay" ].Value;
                    int   rest_i =  M.Groups["REST"].Index;
                    
                    arg_offs_S   =  arg_offs_E;
                    arg_offs_E   += rest_i;

                    rest         = M.Groups["REST"].Value;
                    return true;
                } else { 
                    payl         = null;
                    return false;
                }
            };
            
            while ( true ) {
                if ( Eat ( WhitespaceS )) {
                    
                    R.Add( new PTokWhitespace { len = payl.Length } ) ;      // todo : i guess "\t".Length == 1 ? that would be a problem here
                    continue;
                }
                if ( Eat ( cSharp_basic_identifierS )) {
                    R.Add( new PTok { E = PTokE.CS_name , pay = payl } );
                    continue;
                }
                if ( Eat ( AssignmentOP_S ) ) {
                    PTok op = new PTok { pay = payl };
                    if      ( payl == "<-" ) op.E = PTokE.OP_arrow_left ;
                    //else if ( payl == "<=" ) op.E = PTokE.OP_assign_collection;
                    else throw new Exception();
                    R.Add( op ) ;
                    continue;
                }
                if ( Eat ( DeclOP_S ) ) {
                    PTok op = new PTok { pay = payl };
                    if      ( payl == "->" ) op.E = PTokE.OP_arrow_right ;
                    else throw new Exception();
                    R.Add( op ) ;
                    continue;
                }
                if ( Eat ( SG_Operator_S ) ) {
                    PTok op = new PTok { pay = payl };
                    if      ( payl == ">"  ) op.E = PTokE.OP_GT;
                    else if ( payl == ">>" ) op.E = PTokE.OP_doubleGT;
                    else throw new Exception();
                    R.Add( op );
                    continue;
                }

                if ( Eat ( Equals_Operator_S ) ) {
                    PTok op = new PTok { pay = payl };
                    if      ( payl == "==" ) op.E = PTokE.OP_equals ;
                    else throw new Exception();
                    R.Add( op ) ;
                    continue;
                }

                if ( Eat( SpecialPropOP_S )) {
                    R.Add ( new PTok { E = PTokE.OP_special_prop , pay = payl } );
                    continue;
                }
                if ( Eat( SingleCharOpS ) ) {
                    PTok op = new PTok { pay = payl };
                    if      ( payl == "." ) op.E = PTokE.OP_dot;
                    else if ( payl == "*" ) op.E = PTokE.OP_star;
                    else if ( payl == "%" ) op.E = PTokE.OP_percent;
                    else if ( payl == ":" ) op.E = PTokE.OP_colon;
                    else if ( payl == "[" ) op.E = PTokE.squareBRL;
                    else if ( payl == "]" ) op.E = PTokE.squareBRR;
                    else if ( payl == "{" ) op.E = PTokE.curlyBRL;
                    else if ( payl == "}" ) op.E = PTokE.curlyBRR;
                    else if ( payl == "(" ) op.E = PTokE.plainBRL;
                    else if ( payl == ")" ) op.E = PTokE.plainBRR;
                    else if ( payl == "/" ) op.E = PTokE.OP_slash;
                    else if ( payl == "$" ) op.E = PTokE.OP_dollar;
                    else if ( payl == "#" ) op.E = PTokE.OP_sharp;
                    else if ( payl == "," ) op.E = PTokE.OP_comma;
                    
                    else throw new Exception();
                    R.Add( op);
                    continue;
                }
                if ( Eat ( JSonLiteral ) ) {  // consumation length determined by json parser 
                    object JResult = null ;
                    string new_rest = "";
                    if ( readJSON( rest , out JResult , out new_rest) ) { 
                        R.Add ( new PTokJSON { E = PTokE.JSON , pay = payl + rest.Substring(0,rest.Length-new_rest.Length) /* <- todo: quick guess  */ , payJSON = JResult} );
                        rest = new_rest;
                    } else {                  // json parsing fails i have no way of knowing where this thing was supposed to end -> can't continue tokenization 
                        if ( relaxed ) {
                            R.Add ( new PTok { E =PTokE.ErrT , pay = payl + rest } );
                            return R.ToArray();                                    
                        }
                        else throw new Exception();
                    }
                    continue;
                }
                if ( relaxed ) if (Eat( @"(?'pay'.)" ) ) {
                        R.Add( new PTok { E = PTokE.ErrT , pay = payl } ); // consume arbitrary char and tag it as tokenization error
                        continue;
                }

                if ( rest == "" ) return R.ToArray(); // parse success
                throw new Exception () ; // todo : non tokenizable string 
            }

        }

        
        public static bool readJSON ( string arg , out object JResult , out string rest ) {
            JResult = null ;

            try { 
                JResult = LightJson.Glue.ParseWithRest( arg , out rest ) ;
                return true ;
            } catch ( Exception ) {
                rest = arg;
                return false ;
            }

        }

    }
}
