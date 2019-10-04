using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MainGrammar ;
using ParserComb;

#if haveServerTypes 

using UnityEngine; 
using TranslateAndEval;

#endif 

using Tok = MainGrammar.PTok ;
using MG = MainGrammar.MainGrammar;



namespace EditorlessTests {
    class ProgramEditorlessTests {
        static void Main(string[] args) {
            Test1();
            Test2();
            Test3_Fans_and_Literals();
            Test4_still_fanning_and_analyz0r(analyz0r:true);

            Console.WriteLine ("============ outsiders ============== " );
            TypeMapping.LightJSonAdapter.Test1();

            Console.ReadLine();
        }




        public static ParserComb.NamedNode LexxAndParse(string arg) { return LexxAndParse(arg,TestMG1.TestStart); }
        public static ParserComb.NamedNode LexxAndParse(string arg,ParserComb.Parser<Tok>.PI startProd) {
            var parse_matches = TestMG1.RUN_with_rest(startProd,TranslateEntry.LexxAndStripWS(arg));
            return parse_matches.First().N;
        }

        public static void Test1() {
            //LexxAndRun( ".*foo -> x <- y  -> z " );
            Console.WriteLine(LexxAndParse(".*foo",MG.MemA));
            Console.WriteLine(LexxAndParse(".*foo -> x ",MG.MemAVT));
            Console.WriteLine(LexxAndParse(".*foo -> x ",MG.MemAVT_RX));
            Console.WriteLine(LexxAndParse("..str",MG.MemAVT_RX));
            Console.WriteLine(LexxAndParse("..str -> foo",MG.MemAVT_RX));

            Console.WriteLine(LexxAndParse("..str -> foo",TestMG1.TestStartRX));
            Console.WriteLine("----------- whoooo ------- " );

            Console.WriteLine(LexxAndParse("  <- $XX ",MG.SingleAssign));
            Console.WriteLine(LexxAndParse("  <- $XX -> a -> b ",MG.AssignVT));

            Console.WriteLine(LexxAndParse(" .*foo -> decl1 <- $ARG -> decl2 ",TestMG1.TestStart));


            // -----------------------------------------------------------------

            var MM     = new MemMapper();
            var dollar_arg_CH = new TypedSingleCH<int>();

            // Method I: hack column entries into MM directly 
            ColumnSingle<int>     dollar_arg_Column = (ColumnSingle<int>)dollar_arg_CH.SpawnColumn();     // todo: maybe provide SpawnColumnT that "kinda-overloads" on the return type 
            MM.D[dollar_arg_CH] = dollar_arg_Column;


            dollar_arg_Column.AddVal(3,null);
            dollar_arg_Column.AddVal(4,null);

            // Method II: abuse MemMapper for column creation 
            ColumnSingle<TestcasesLHSType> LHS_column ;
            LHS_column = MM.get( DummyInstances.TestcasesLHSsingletonTypedCH );                           // TypedSingle<DummyType>

            LHS_column.AddVal( new TestcasesLHSType () , null ) ;
            LHS_column.AddVal( new TestcasesLHSType () , null ) ;
            LHS_column.AddVal( new TestcasesLHSType () , null ) ;

            CH_closedScope scope = new CH_closedScope();
            scope = scope.decl ( "ARG" , dollar_arg_CH ) ;

            CH_closedScope out_scope ;

            

            var TR_LHS = new TranslateLHS {
                preCH_LHS = new adapter_preCH ( DummyInstances.TestcasesLHSsingletonTypedCH ),
                scope = scope
            };

            Console.WriteLine( "in -> " + LHS_column );
            Column res = Evaluate.Eval(" ..intMem1 -> decl1 <- $ARG -> decl2 ",DummyInstances.GE_TestStart,TR_LHS,MM,out out_scope);
            foreach(var s in ColumnChainPrttS(
                res,
                CH => MM.D[CH]
                ).Reverse()) {    Console.WriteLine(s);   }


        }


        public static void Test2() {
            CH_closedScope sc = DummyInstances.DummyScope();
            string expr_l = "..str", expr_r = " -> foo ";
            string expr = expr_l + expr_r;
            Tok[] strippedToks = TranslateEntry.LexxAndStripWS(expr );
            var NN = TranslateEntry.Scope( strippedToks , DummyInstances.GE_TestStartRX , DummyInstances.DummyTransLHS ) ;

        }
        public static IEnumerable<string> ColumnChainPrttS ( Column Col_in , Func<TypedCH,Column> CH2Col   ) {
            Column col = Col_in ;
            while ( true ) {
                yield return col.ToString();
                TypedCH CH = col.CH;
                if ( CH.DataSrc == null ) yield break;
                col= CH2Col ( CH.DataSrc.CH_in );
            }

        }

        public static void Test3_Fans_and_Literals () {
            var lhsCH = new TypedSingleCH<TestcasesLHSType>();

            var FanGE = new GrammarEntry {
                StartProd       = MG.Fan , 
                TR_constructor  = ( nnode ) => new FanTU ( new adapter_preCH( lhsCH )  , (MG.FanNode) nnode ) 
            };
            var lhsColumn = (ColumnSingle<TestcasesLHSType>)lhsCH.SpawnColumn();
            lhsColumn.AddVal ( new TestcasesLHSType () , null  );

            CH_closedScope outScope ;

            var TR_LHS = new TranslateLHS {
                preCH_LHS = new adapter_preCH ( lhsCH ) ,             // different instance then GrammarEntry - intentional 
                scope     = new CH_closedScope()
            };
            var MM = new MemMapper () ;
            MM.D[lhsCH] = lhsColumn ;
            Column res = Evaluate.Eval ( " { ..intMem1 -> x <- @4 ->x2  , ..intMem2  <- $x2 }  " , FanGE , TR_LHS , MM , out outScope );

            Console.WriteLine ( " -------------- " ) ;
            foreach ( var v in (res as Column<TestcasesLHSType>).valuesT ) Console.WriteLine ( v ) ; 

            Console.WriteLine ( " ---.---.--.--.---- " ) ;
            foreach ( var KV in MM.D ) {
                Console.WriteLine( KV.Key + " :: " + KV.Value) ;
            }

        }
        public static void Test4_still_fanning_and_analyz0r (bool analyz0r = false ) {
            var lhsCH = new TypedSingleCH<TestcasesLHSType>();

            var FanGE = new GrammarEntry {
                StartProd       = MG.Fan , 
                TR_constructor  = ( nnode ) => new FanTU ( new adapter_preCH( lhsCH )  , (MG.FanNode) nnode ) 
            };
            var trslLHS = new TranslateLHS {
                preCH_LHS = new adapter_preCH ( lhsCH ),
                scope     = new CH_closedScope()
            };

            var compilat = TranslateEntry.TranslateFully( " { ..intMem1 -> x <- @4 ->x2  , ..intMem2  <- $x2 }  " , FanGE , trslLHS ) ;

            if ( analyz0r ) Analyz0r.A.JsonifyCompilat( compilat );

            var MM = new MemMapper();
            var LHS_column = MM.get( lhsCH) ;
            LHS_column.AddVal ( new TestcasesLHSType() ) ;
            LHS_column.AddVal ( new TestcasesLHSType() ) ;
            LHS_column.AddVal ( new TestcasesLHSType() ) ;
            Evaluate.Eval ( compilat , MM ) ;

            if ( analyz0r ) Analyz0r.A.JsonifyEval ( compilat , MM ) ;

        }


    }


    public class TestMG1 : MG {
        public class MemA_andOptAssign : NamedNode {
            public MemAVTNode   memavt_node   { get { return (MemAVTNode) children[0]; } }
            public AssignVTNode assignvt_node { get { return children.Length > 1 ?(AssignVTNode) children[1] : null ; } }
        }
        public static PI TestStart = Prod<MemA_andOptAssign> ( SEQ ( MG.MemAVT , MG.AssignVT ));
        public static PI TestStartRX = Prod<MemA_andOptAssign> ( SEQ ( MG.MemAVT_RX , OR ( MG.AssignVT , EPSILON() )));
    }

     public class TestTR : TranslationUnit {
        public TestMG1.MemA_andOptAssign test_start_node {get;set; }

        public MemATU                memaTR;
        public AssignTR              assignTR;
        
        public override VBoxTU[] VBoxTUs { get { return new VBoxTU[] { memaTR , assignTR } ; } }

        preCH pre_LHS ;
        
        public TestTR ( TestMG1.MemA_andOptAssign startNode, preCH pre_LHS ) {
            test_start_node = startNode;
            this.pre_LHS = pre_LHS;
            memaTR = new MemATU ( pre_LHS , startNode.memavt_node );

            assignTR = startNode.assignvt_node == null ? null : new AssignTR ( memaTR.preCH_out , startNode.assignvt_node ) ;

        }
        public override preCH_deltaScope scope ( preCH_deltaScope SC ) {
            preCH_deltaScope midSC = memaTR.scope( SC);
            if ( assignTR == null ) return midSC ;
            return assignTR.scope(midSC);
        }
        public override IEnumerable<OPCode> emit() {
            foreach ( var op in memaTR.emit()   ) yield return op;
            if (assignTR != null ) foreach ( var op in assignTR.emit() ) yield return op;
        }
    }

    public struct TestcasesStruct {
    }

    public class TestcasesLHSType {
        public string strMem ; 
        public string strMem2 ; 
        public string strMem3 ; 
        public int    intMem1 ; 
        public int    intMem2 ; 
        public TestcasesLHSType selfTref ;
        public TestcasesLHSType selfTref2 ;

        public TestcasesStruct structMem ;
#if haveServerTypes
        Vector3 vecMem;
        Transform transform;
        MeshRenderer meshRenderer;
#endif 
    }
    public static class DummyInstances {

        // becuase instance identification of Translate View MemLoc 
        public static TypedSingleCH<TestcasesLHSType> TestcasesLHSsingletonTypedCH = new TypedSingleCH<TestcasesLHSType>();
        public static TypedSingleCH<int>              DummyIntHeaderSing = new TypedSingleCH<int>();


        public static TranslateLHS DummyTransLHS = new TranslateLHS {
            preCH_LHS = new adapter_preCH( TestcasesLHSsingletonTypedCH ),
            scope  = DummyScope(),
        };

        public static GrammarEntry GE_TestStart = new GrammarEntry { 
            StartProd = TestMG1.TestStart,
            TR_constructor = ( NN ) => new TestTR ( (TestMG1.MemA_andOptAssign)  NN , DummyTransLHS.preCH_LHS ) 
        };
        public static GrammarEntry GE_TestStartRX = new GrammarEntry { 
            StartProd = TestMG1.TestStartRX,
            TR_constructor = ( NN ) => new TestTR ( (TestMG1.MemA_andOptAssign)  NN , DummyTransLHS.preCH_LHS ) 
        };


        public static CH_closedScope DummyScope () {
            CH_closedScope sc = new CH_closedScope() ;
            sc = sc.
                decl( "VDummyType" , TestcasesLHSsingletonTypedCH ).
                decl( "Vint"       , DummyIntHeaderSing);
            return sc;

        }
    }



}
