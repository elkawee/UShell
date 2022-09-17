

using MainGrammar ;
using MG = MainGrammar.MainGrammar;

using SObject = System.Object ;
using System;
using System.Linq;

namespace Operations { // todo maybe own namespace 
    public static partial class Operations {

        

        public static ShellCommon.EVAL_Resp EVAL_stateless ( ShellCommon.EVAL_Req eval_req , bool analyz0r = false ) { 

            // fetch evaluation entry from the current junkjard that is EvaluatorTests_and_random_fux0ring.cs

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
                compilat  = TranslateEntry.TranslateFully_incomplete_tolerant( eval_req.expr , GE , TLHS ) ;
            } catch ( Exception e ) {
                return new ShellCommon.EVAL_Resp { 
                    success = false ,
                    msg = "translate ERROR : " +  e.Message + "\n" + e.StackTrace ,
                    result = new SObject[0]
                };
            }

            if ( analyz0r ) {
                //Analyz0r.A.JsonifyCompilat( compilat ) ;
            }

            var MM = new TranslateAndEval.MemMapper();
            TranslateAndEval.Column resultColumn ;
            try { 
                resultColumn = Evaluate.Eval( compilat , MM );
            } catch ( Exception e) {
                return new ShellCommon.EVAL_Resp { 
                    success = false ,
                    msg = "eval ERROR : " +  e.Message + "\n" + e.StackTrace ,
                    result = new SObject[0]
                };
            }

            try { 
                if ( analyz0r ) {
                    //Analyz0r.A.JsonifyEval( compilat , MM ) ; 
                }
            } catch ( Exception ) {} // TODO - write a message to the file about how this crashed 

            var resp = new ShellCommon.EVAL_Resp {
                success = true, 
                msg     = "OK",
                result  = resultColumn.values.ToArray()     // <- current serialization (ShellNetworkGlue.cs) simply does a ToString() for all Objects 
            };

            return resp; 
        }

    } 
}