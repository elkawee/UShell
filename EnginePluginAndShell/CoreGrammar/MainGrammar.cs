
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
        OP_GT,OP_doubleGT,OP_dot,OP_percent,OP_star,OP_colon,OP_special_prop,OP_slash,OP_backslash,OP_comma,
        OP_arrow_left , OP_arrow_right , 
        OP_equals ,
        OP_sharp , OP_dollar , 
        OP_lift_up , OP_lift_down,
        CS_name,
        squareBRL,squareBRR,
        curlyBRL,curlyBRR,
        plainBRL,plainBRR, 
        ErrT,
        JSON
    }

    //public class PTokBase { }
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
        public DeserCapsule deser_capsule;
        public PTokJSON ( string pay , DeserCapsule capsule)
        {
            this.pay = pay;
            deser_capsule = capsule;
            E = PTokE.JSON;
        }
    }

    public class PTokBase_Serializer : CustomSerializerT<PTokBase>
    {
        // TODO : eventually tis exp_type field is supposed to be used for serious calculations 
        //        with  exp_type being a constant function this only works if there really is exactly one serializer per recognized type 
        //        unless ... class-types probably need to be treated extra for a number of other reasons 

        public override ExpType exp_type => new StructExpType( new KV[0]);

        PTokWhitespace_Serializer WS_serializer   = new PTokWhitespace_Serializer();
        PTok_Serializer           tok_serializer  = new PTok_Serializer();
        PTokJSON_Serializer       json_Serializer = new PTokJSON_Serializer();

        public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
        {
            Type rttype = arg.GetType();
            if( rttype == typeof(PTokWhitespace) ) return WS_serializer.SER(arg,channel);
            if( rttype == typeof(PTokJSON))        return json_Serializer.SER(arg,channel);    // order matters ptok :> tok_json 
            if( rttype == typeof(PTok))            return tok_serializer.SER(arg,channel);

            throw new NotImplementedException("unimplemented subtype of PTokBase");
        }

        public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
        {
            // try deser as PTok , if it fails assume Whitespace 
            try
            {
                var sview  = channel.GetStructView_RO( (StructExpType) tok_serializer.exp_type, arg );
                PTokE e    = (PTokE) channel.DESER_Primitive ( new EnumExpType(typeof(PTokE)) ,  sview.Get("E") );

                if ( e == PTokE.JSON) return json_Serializer.DESER(arg, channel);
                else                  return tok_serializer.DESER(arg, channel);
            } catch ( Exception )
            {
                return WS_serializer.DESER(arg,channel);
            }
        }

    }
    public class PTokWhitespace_Serializer : StructLikeClassSerializer<PTokWhitespace> { }
    public class PTok_Serializer           : StructLikeClassSerializer<PTok> { }

    public class PTokJSON_Serializer : CustomSerializerT<PTokJSON>
    {

        public static PTok_Serializer                   ptok_serializer    = new PTok_Serializer();
        public static DeserCapsuleLightJson_Serializer  capsule_serializer = new DeserCapsuleLightJson_Serializer();


        static StructExpType _exp_type = new StructExpType( new [] {
                new KV { name = "deser_capsule" , exp = capsule_serializer.exp_type       }  ,
                new KV { name = "E"             , exp = new EnumExpType( typeof(PTokE) )  }  , // TODO 
                new KV { name = "pay"           , exp = SerPrimitiveExpType.STRING           }
            });

        public override ExpType exp_type => _exp_type;

        

        public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
        {
            
            StructView_RO<TMPVAL_Type> sview   = channel.GetStructView_RO(_exp_type , arg);
            string                     pay     = (string)       channel.DESER_Primitive( SerPrimitiveExpType.STRING, sview.Get("pay") );
            DeserCapsule               capsule = (DeserCapsule) capsule_serializer.DESER(sview.Get("deser_capsule"), channel );
            return new PTokJSON( pay , capsule);
        }

        public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
        {
            var ptok_json = (PTokJSON) arg;
            var sview = channel.GetStructBuilder( _exp_type );
            sview.Add("deser_capsule" , capsule_serializer.SER(ptok_json.deser_capsule, channel ));
            //sview.Add("E"             , 
            return sview.Final();
        }
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
            
            Func<Type> ACMembTypingCallback {get;set;}  // type from which member lists are supposed to be matched against
        }
        public interface ACableTypeName : ACable {  // typename completion is context free, in a sense. No callback needed
        }

        public interface ACableFuncName : ACable {
            Func<Type> AC_FuncHostingType_Callback {get;set;}
        }

        #region decl

        public class DeclNode : NamedNode {
            public string name ;
            public override void build () { name = ((TermNode)children[1]).tok.pay ; }
        }

        public static PI Decl = Prod<DeclNode> ( SEQ ( TermP ( PTokE.OP_arrow_right ) , TermP( PTokE.CS_name ) ));

        public class DeclStarNode : NamedNode {
            public string [] decls ;            // contract : never null always at least a zero elem array ( used by PrimitveStepTU::lastmostDecl() ) 
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
            //public object JSonVal ;  // atm this is PatchedLJ.JsonValue - but i don't want to have the decision on json library bound here 
            public DeserCapsule capsule ; 
            public override void build ( ) {
                NamedNode rhs = children[1];
                if ( rhs is SharpRefNode )  { name = (rhs as SharpRefNode  ).name ; type = typeE.sharp  ; return ;}
                if ( rhs is DollarRefNode ) { name = (rhs as DollarRefNode ).name ; type = typeE.dollar ; return ; }

                if ( rhs is TermNode && TermEnum( rhs) == PTokE.JSON ) {
                    type = typeE.json;
                    capsule = ((rhs as TermNode).tok as PTokJSON).deser_capsule;
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

        // todo : i do not know anymore, why the ACAble interface is included already in this type and not in its RX variant 
        public class TypeNameNode : NamedNode,ACableTypeName {
            
            public string [] names ;
            public override void build() => names = children.Where( nn => (nn as TermNode).tok.E == PTokE.CS_name ).Select ( TermPay ).ToArray();
        }
        // qualified typenames, a la :      :Namespace/TN1/TN2    ( / instead of . for disambiguation with member access ) 
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



        #region FAN_forward_decl
        public static PI_defer  Fan   = MKProdDefer<FanNode>();
        public static PI_defer  Frame = MKProdDefer<FrameNode>();    // atm frame is not recursive - but most likely will be at some point 
        

        #endregion
        
        #region Filter

        public class EqualsFilterNode : NamedNode {
            public bool isRef      => children[1] is RefNode      ;
            public bool isSharpRef => children[1] is SharpRefNode ;
            public bool isDollarRef=> children[1] is DollarRefNode ; 
            
            public RefNode       RHS_ref => children[1] as RefNode  ;
            public string        json_with_at    => isRef? null : TermPay( children[1] ) ; // raw incluing the "@" 
            public DeserCapsule  capsule         => isRef? null : TermJSONCapsule( children[1] );
            //public object  json_value
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
         *   GO <- GO                  // add Child ?? ( replaceChild seems more reasonable ) 
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
                        Fan     , 
                        TermP( PTokE.OP_lift_up ) 
                        // Todo : VarRef 
                        ) , 
                DeclStar ,
                STAR ( AssignVT )     // AssignVT includes DeclStar
                ) );
        
        #region FAN

        public class FanElemNode : NamedNode {

            public PrimitiveStepNode [] primStepNodes ;
            public override void build() {

                    primStepNodes = children.Select( _=> (PrimitiveStepNode)_).ToArray();
                }
        }

        public static PI FanElem = Prod<FanElemNode> ( PLUS ( PrimitiveStep ));

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
        
        // -------------------------------------------------------------------------------------------------------
         
        #region - F R A M E - 
      

        public static PI FrameElem = Prod<FrameElemNode>( OR(PLUS ( PrimitiveStep ) , TermP( PTokE.JSON) , SharpRef , DollarRef ) );
        
        /*
            i *could* most likely recycle FanElem here, both in the definition of the production 
            as well as in the used Node type ...
            *could* ... *most likely* ... =) 

            i true anti-DRY fashion: let's not - cuz it never works out in practice 
        */  
        public class FrameElemNode : NamedNode {
            public bool isConstant => children.Length == 1 && children[0] is TermNode ;
            public bool isRef      => children.Length == 1 && children[0] is RefNode  ;

            public PrimitiveStepNode [] primSteps  = new PrimitiveStepNode[0]; 
            public TermNode             jsonLit ;
            public RefNode              refNode ;
            public override void build() {
                if ( isConstant ) {
                    jsonLit = (TermNode)children[0] ;
                } else if ( isRef ) {
                    refNode = (RefNode) children[0] ; 
                } else { 
                    primSteps = children.Select( _ => (PrimitiveStepNode) _ ).ToArray();
                }
            }
        }


        public static PI _Frame = SETProdDefer( Frame , 
                                                SEQ ( TermP( PTokE.squareBRL ) ,
                                                      OR(SEQ( FrameElem , STAR( SEQ ( TermP( PTokE.OP_comma ) , FrameElem ))),
                                                         EPSILON()                                                          ),     // <- zero `FrameElem`s need to be possible in order for the frame to be useful as function argument
                                                      TermP( PTokE.squareBRR ))
                                                );

        public class FrameNode : NamedNode {
            public FrameElemNode [] frameElemNodes ; 
            public override void build() => frameElemNodes = children.Where( ch => ( ch is FrameElemNode) ).Select( fr => ( FrameElemNode) fr ).ToArray();
        }

        #endregion

        public class FunCallNode : NamedNode {
            public TypeNameNode typeNameNode = null ; 
            public string       methodName   ;
            public FrameNode    frameNode    ;
            public virtual bool isStatic => children.Length == 4 ;

            public override void build() {
                
                if ( children.Length == 4 ) {
                    typeNameNode = (TypeNameNode)children[1];
                    methodName   = TermPay( children[3] );
                } else if ( children.Length == 3 ) { 
                    methodName   = TermPay( children[2] ) ; 
                } else throw new Exception();

                frameNode = (FrameNode) children[0];
                // Todo: i am pretty sure the payload of Term<CS_name> can never be null or "" , ... like 95% or  so 
            }
        }

        public static PI FunCall = Prod<FunCallNode>( SEQ( Frame , OR( 
                                                                        SEQ ( TypeName , TermP( PTokE.OP_backslash ) , TermP( PTokE.CS_name )),
                                                                        SEQ (            TermP( PTokE.OP_backslash ) , TermP( PTokE.CS_name ))
                                                                        )));

        #region Start
        public class ProvStartNode : NamedNode {
            public SG_EdgeNode startSG ;
            public FunCallNode startFuncall ; 
            public bool rootIsSG => startSG != null ; 

            

            public NamedNode         [] subsequentSteps ; // PrimStepNode|FunCallNode
        

            public override void build() { 
                if( children[0] is SG_EdgeNode ) 
                    startSG      = (SG_EdgeNode) children[0];
                else 
                    startFuncall = (FunCallNode ) children[0]; 

            
                subsequentSteps = children.Skip(1).ToArray();
            }

        }

        

        public static PI ProvStart = Prod<ProvStartNode> ( SEQ ( OR( FunCall, SG_Edge ) , STAR ( OR ( PrimitiveStep , FunCall ) ))); // todo : or open with variable ref 
        

        
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
        public static DeserCapsule TermJSONCapsule ( NamedNode N ) { // again - don't want to carry JSonImplementation decision into this -- return type is currently LightJson.JSonValue
            try { 
                var termNode = (TermNode) N;
                var jSonTok  = (PTokJSON) termNode.tok;
                return jSonTok.deser_capsule;
            } catch ( Exception ) { throw new NNShapeException() ;}
        }

        #endregion 


    }

    


    public class Lexer {

        const string cSharp_basic_identifierS     = @"(?'pay'[\w_0-9]+)";    // todo
        const string SingleCharOpS = @"(?'pay'%|\*|\.|:|\[|\]|\{|\}|\(|\)|/|\$|#|,|\\)";

        const string SG_Operator_S                = @"(?'pay'>{1,2})";                        // andere idee : @"((?'pay'>{1,3})[^>]|$)"  , aktuelle variante matcht auch >>>> , bin noch nicht sicher, ob das ein Problem ist 
        const string SpecialPropOP_S              = @"(?'pay'%!)";
        const string WhitespaceS                  = @"(?'pay'\s+)"   ;
        const string JSonLiteral                  = @"(?'pay'@)" ; 
        //const string AssignmentOP_S               = @"(?'pay'<-|<=)";
        const string AssignmentOP_S               = @"(?'pay'<-)";
        const string DeclOP_S                     = @"(?'pay'->)";
        const string Equals_Operator_S            = @"(?'pay'==)";
        const string keywords_S                   = @"(?'pay'\^lup|\^ldown|\^1up)";    // todo ^ for disambiguation, there is currently no sane way to not let this conflict with `cname` otherwise 
        
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
                    else throw new Exception("decl tokenize");
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
                    else throw new Exception("equals op tokenize");
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
                    else if ( payl == "\\") op.E = PTokE.OP_backslash;
                    else if ( payl == "$" ) op.E = PTokE.OP_dollar;
                    else if ( payl == "#" ) op.E = PTokE.OP_sharp;
                    else if ( payl == "," ) op.E = PTokE.OP_comma;
                    
                    else throw new Exception("single char OP tokenize" );
                    R.Add( op);
                    continue;
                }
                if ( Eat ( JSonLiteral ) ) {  // consumation length determined by json parser 
                    LightJson.JsonValue JResult  ;
                    string new_rest = "";
                    if ( readJSON( rest , out JResult , out new_rest) ) { 
                        R.Add ( new PTokJSON (  payl + rest.Substring(0,rest.Length-new_rest.Length) /* <- todo: quick guess  */ 
                                             , new DeserCapsuleLightJson( JResult)) );
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
                if( Eat( keywords_S )) {
                    if( payl == "^lup" || payl =="^1up" ) { R.Add( new PTok { E = PTokE.OP_lift_up , pay = payl } )   ; continue ; }
                    if( payl == "^ldown" )               { R.Add( new PTok { E = PTokE.OP_lift_down , pay = payl } ) ; continue ; }
                    throw new NotImplementedException();
                }
                if ( relaxed ) if (Eat( @"(?'pay'.)" ) ) {
                        R.Add( new PTok { E = PTokE.ErrT , pay = payl } ); // consume arbitrary char and tag it as tokenization error
                        continue;
                }

                if ( rest == "" ) return R.ToArray(); // parse success
                throw new Exception ("untokenizable input :" + rest) ; // todo : non tokenizable string 
            }

        }

        
        public static bool readJSON ( string arg , out LightJson.JsonValue JResult , out string rest ) {
            JResult = LightJson.JsonValue.Null;
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
