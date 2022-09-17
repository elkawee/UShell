using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ParserComb;

using D = System.Diagnostics.Debug;


namespace MainGrammar { 

    public class MainGrammarRX : MainGrammar { 


        public class MemANodeRX : MemANode, ACableMemb {
            public Func<Type> ACMembTypingCallback {get;set;}
            public PTok refineOPTok = null ;
            public PTok nameTok     = null ; 
            public PTok initialOpTok = null ;
            public override void build () {
                initialOpTok = TermTok ( children[0] );
                kind = kindE.any; 
                if ( children.Length == 1 ) {
                    name = ""; 
                    kind = kindE.any ;
                    return ;
                }
                PTok Tok1 = TermTok( children[1] );
                if ( children.Length == 2 ) {
                    if (Tok1.E == PTokE.CS_name) {
                        name = Tok1.pay;
                        kind = kindE.any ;
                        nameTok = Tok1;
                    } else {
                        name = "";
                        kind = kindE_from_PTokE( TermEnum ( children[1] ));
                        refineOPTok = Tok1;
                    }
                    return ;
                }
                
                if ( children.Length == 3 ) {
                    name    = TermPay( children[2] );
                    nameTok = TermTok( children[2] );
                    
                    kind        = kindE_from_PTokE ( TermEnum( children[1] ));
                    refineOPTok = TermTok( children[1] );

                }
            }
            
        }
        public static PI MemARX = Prod<MemANodeRX> (  SEQ ( TermP ( PTokE.OP_dot ) ,
                                                        OR(  OR ( TermP ( PTokE.OP_dot ) ,
                                                                  TermP ( PTokE.OP_star ) ),
                                                                  TermP ( PTokE.OP_percent ),
                                                                  TermP ( PTokE.OP_special_prop ),
                                                                  EPSILON()),
                                                        OR( TermP ( PTokE.CS_name ),
                                                            EPSILON())
                                                     ));

        public static PI MemAVT_RX = Prod<MemAVTNode> ( SEQ ( MemARX , DeclStar));

        public class TypeNameNodeRX : TypeNameNode /* includes : NamedNode , ACableTypeName */ { 
            //public string [] names = new string[0]; // contract : never null , absence of CS_name toks -> Array.Len == 0 
            public PTok   [] nameToks = null ;
            public override void build() {
                nameToks = children.Where( nn => (nn is TermNode ) && TermEnum( nn ) ==  PTokE.CS_name ).Select(TermTok).ToArray(); 
                names = nameToks.Select ( tok => tok.pay ).ToArray();
            }
        }

        public static PI TypenameRX = Prod<TypeNameNodeRX>( 
            OR ( SEQ ( TermP ( PTokE.OP_colon ) , TermP( PTokE.CS_name) , STAR  ( SEQ ( TermP( PTokE.OP_slash ) , TermP(PTokE.CS_name ) )))
               , TermP( PTokE.OP_colon )  ));
                                                          
        public class SG_EdgeNodeRX : SG_EdgeNode {
            //public string [] typefilter = new string[0];  // contract : never null , absence of CS_name toks -> Array.Len == 0 
            public TypeNameNodeRX typeNameNode = null ;
            public override void build() {
               var TNNs = children.Where ( nn => nn is TypeNameNodeRX ).Select( x => ( TypeNameNodeRX ) x ).ToArray();
               if ( TNNs.Any()) { 
                    typeNameNode = TNNs.Single();
                    typefilter = typeNameNode.names;
                }
               
               // ... don't really care for the rest 
            }
        }


        public static PI SG_EdgeRX = Prod<SG_EdgeNodeRX> ( 
            SEQ ( OR ( TermP(PTokE.OP_doubleGT) , TermP ( PTokE.OP_GT )   ) , 
                    // next up : TypenameRX
                  OR ( TypenameRX , EPSILON() )                                 // <- plugging this in directly here makes the grammar abigous, but eh ... 
                ) ) ;

        public static PI RG_EdgeRX = Prod<RG_EdgeNode> ( SEQ ( MemAVT_RX , OR ( AssignVT , EPSILON() ) ));

        //                                                                        V'''' decls ?     V''' todo FanRX
        public static PI FanElemRX = Prod<FanElemNode> ( PLUS ( OR ( RG_EdgeRX , SG_EdgeRX ,         Fan ) ));


        public static PI FilterRX = Prod<FilterNode> ( 
            OR ( TypenameRX , 
                 EqualsFilter ) );

        public static PI PrimitiveStepRX = Prod<PrimitiveStepNode> ( SEQ ( 
                OR (    SG_EdgeRX , 
                        MemARX    , 
                        FilterRX  , 
                        Fan                 // TODO : FanRX 
                        // Todo : VarRef 
                        ) , 
                DeclStar ,
                STAR ( AssignVT )     // AssignVT includes DeclStar
                ) );


        public static PI FrameElemRX = Prod<FrameElemNodeRX>( OR(PLUS ( PrimitiveStepRX ) , TermP( PTokE.JSON) ) );

        public class FrameElemNodeRX : FrameElemNode {
        }

        public static PI_defer  FrameRX  = MKProdDefer<FrameNode>();    // atm frame is not recursive - but most likely will be at some point 

        public static PI        _FrameRX = SETProdDefer( FrameRX , 
                                                SEQ ( TermP( PTokE.squareBRL ) ,
                                                      OR(SEQ( FrameElemRX , STAR( SEQ ( TermP( PTokE.OP_comma ) , FrameElemRX ))),
                                                         EPSILON()                                                          ),     // <- zero `FrameElem`s need to be possible in order for the frame to be useful as function argument
                                                      TermP( PTokE.squareBRR ))
                                                );

        public class FuncNameNodeRX : NamedNode, ACableFuncName
        {
            public Func<Type> AC_FuncHostingType_Callback { get ; set ; }
            public string name = "" ;
            public override void build(){
                if ( children.Length == 2 ) name = TermPay( children[1] ) ;
                else D.Assert( children.Length == 1 ) ; 
            }
        }
        public static PI FuncNameRX = Prod<FuncNameNodeRX>( OR ( SEQ ( TermP( PTokE.OP_backslash ), TermP( PTokE.CS_name )   ),
                                                                       TermP( PTokE.OP_backslash )
                                                                 
                                                                ));

        public class FunCallNodeRX : FunCallNode {
            public override bool isStatic => typeNameNode != null ;
            public bool haveMethodName    => methodName   != null ;

            public FuncNameNodeRX funcNameNodeRX ;

            public override void build() {
                this.frameNode = (FrameNode) children[0] ;

                if        ( children.Length == 2  && children[1] is TypeNameNodeRX ) {

                    typeNameNode = (TypeNameNode) children[1];
                    methodName   = null ;

                } else if ( children.Length == 2  && children[1] is FuncNameNodeRX ) {
                    
                    typeNameNode = null ; 
                    methodName   = (children[1] as FuncNameNodeRX).name;
                    funcNameNodeRX = (FuncNameNodeRX)children[1]; 
                
                } else if ( children.Length == 3 ) { 
                    
                    typeNameNode = (TypeNameNode) children[1];
                    methodName   = (children[2] as FuncNameNodeRX).name;
                    funcNameNodeRX = (FuncNameNodeRX)children[2];
                }
            }
        }

        public static PI FunCallRX = Prod<FunCallNodeRX>( SEQ( FrameRX , OR(SEQ ( TypenameRX , FuncNameRX) ,
                                                                            TypenameRX,   
                                                                            FuncNameRX
                                                                            )));



        public class ProvStartNodeRX : ProvStartNode { 
        }

        
        // TODO proper RX variant 
        public static PI ProvStartRX = Prod<ProvStartNodeRX> ( SEQ ( OR( FunCallRX , SG_EdgeRX ) , STAR ( OR( FunCallRX , PrimitiveStepRX ))));



    }

}