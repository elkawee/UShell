
using MainGrammar ;
using MG = MainGrammar.MainGrammar;

using SObject = System.Object ;
using System;
using System.Linq;


namespace Operations {
    public static partial class Operations { 
        public static ShellCommon.TYPEINFO_Resp TYPEINFO ( ShellCommon.TYPEINFO_Req req ) {
            // only do this for requests that are valid to be exectuted -> no relaxed grammar 

            // thus, most of this is copypasta from Operations_EVAL.cs 

            GrammarEntry GE = new GrammarEntry { 
                StartProd       = MG.ProvStart , 
                TR_constructor  = NN => new TranslateAndEval.ProvStartTU ( (MG.ProvStartNode )  NN  ) 
            };

            TranslateLHS TLHS = new TranslateLHS { 
                preCH_LHS = null ,
                scope     = new TranslateAndEval.CH_closedScope()
            };
            Compilat compilat;
            try { 
                compilat  = TranslateEntry.TranslateFully_incomplete_tolerant( req.expression , GE , TLHS ) ;
            } catch ( Exception e ) {
                return new ShellCommon.TYPEINFO_Resp { 
                    success = false ,
                    msg     = e.Message , 
                    // defaults to make serialization easier -- little use in specifying a contract here, as this is likely to change anyway 
                    members = new System.Type[0]
                };
            }

            var result_type = compilat.VBoxTrs.Last().CH_out.ttuple.PayT ; // out-Type of last column

            var memIs = result_type 
                        .GetMembers()
                        .Where( x => x.MemberType == System.Reflection.MemberTypes.Field || x.MemberType == System.Reflection.MemberTypes.Property )
                        .ToArray() ;

            


            return new ShellCommon.TYPEINFO_Resp { 
                success = true , 
                msg = "" , 
                expr_unity_type = result_type , 
                unique = true ,                     // FIXME : todo 
                members = memIs

            };
        }
    }
}