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
using MGRX = MainGrammar.MainGrammarRX;



namespace EditorlessTests {
    class ProgramEditorlessTests {
        static void Main(string[] args) {

            Test1();
            Test2();
            Test3_Fans_and_Literals();
            Test4_still_fanning_and_analyz0r(analyz0r:true);

            Console.WriteLine ("============ outsiders ============== " );
            TypeMapping.LightJSonAdapter.Test1();
            TokLineTest.TestAll();
            ParserComb.Tests.TestMutualRecursion();
            Console.WriteLine("====== end outsiders ================= " ) ;

            GreenCameraTest();

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
            Console.WriteLine(LexxAndParse(".*foo -> x ",MGRX.MemAVT_RX));
            Console.WriteLine(LexxAndParse("..str",MGRX.MemAVT_RX));
            Console.WriteLine(LexxAndParse("..str -> foo",MGRX.MemAVT_RX));

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

            if ( analyz0r ) Analyz0r.A.JsonifyCompilat( compilat , "_Test4_still_fanning" );

            var MM = new MemMapper();
            var LHS_column = MM.get( lhsCH) ;
            LHS_column.AddVal ( new TestcasesLHSType() ) ;
            LHS_column.AddVal ( new TestcasesLHSType() ) ;
            LHS_column.AddVal ( new TestcasesLHSType() ) ;
            Evaluate.Eval ( compilat , MM ) ;

            if ( analyz0r ) Analyz0r.A.JsonifyEval ( compilat , MM  ,  "_Test4_still_fanning") ;

        }

        public static void GreenCameraTest() { 

        #if NetworklessShell
            Console.WriteLine( " --- GreenCameraTest() ---" ) ;
            var go_foo_cam = new GameObject();
            go_foo_cam.AddComponent<Camera>();
            go_foo_cam.GetComponent<Camera>().name = "foo" ;

            var go_other_cam = new GameObject();
            go_other_cam.AddComponent<Camera>();

            Resources.roots = new [] { go_foo_cam , go_other_cam} ;
            
            
            var query = ">> :Camera .%projectionMatrix <- @[[1,1,1,1],[2,2,2,2],[3,3,3,3],[4,4,4,4]]" ; 

            var GramE = new GrammarEntry { 
                StartProd      = MG.ProvStart , 
                TR_constructor = ( prov_start_node ) => new ProvStartTU ( (MG.ProvStartNode)  prov_start_node ) 
            };
            var transl_LHS = new TranslateLHS { 
                preCH_LHS = null ,
                scope = new CH_closedScope() 
            };

            var compilat1 = TranslateEntry.TranslateFully ( query , GramE , transl_LHS ) ;

            var MM = new MemMapper(); 
            var res = Evaluate.Eval ( compilat1 , MM ) ; 

            Console.WriteLine ( res ) ; 
            var query2 = ">> :Camera {.name == @\"foo\"} .projectionMatrix <- @[[1,1,1,1],[2,2,2,2],[3,3,3,3],[4,4,4,4]]" ; 
            var compilat2 = TranslateEntry.TranslateFully ( query2 , GramE , transl_LHS ) ;

            Analyz0r.A.JsonifyCompilat( compilat2 , "_GreenCam_with_fan" );

            var res2 = Evaluate.Eval ( compilat2 , MM ) ; 
            Console.WriteLine ( res2 ) ; 

            Analyz0r.A.JsonifyEval ( compilat2 , MM  ,  "_GreenCam_with_fan") ;

            Console.WriteLine( " --- GreenCameraTest() done ---" ) ;

        
        #endif 


        }
    }


    public class TestMG1 : MG {
        public class MemA_andOptAssign : NamedNode {
            public MemANode      memavt_node   { get { return (MemANode) children[0]; } }
            public DeclStarNode  declStarNode  => children[1] as DeclStarNode;                  // <- bin mir nicht mehr so sicher, ob das funktioniert 
            public AssignVTNode  assignvt_node { get { return children.Length > 2 ?(AssignVTNode) children[2] : null ; } }
        }

        /*
            Verhalten Komposition 
                - SEQ( ... , OR( ... , EPSILON ) ) 
                - SEQ( ... , somethingSTAR() ) 
            
            bezgl der laenge des children Arrays fuer verschiedene Matches ? - ich weiss es einfach nicht mehr , verdommt 

            ... ich 
        */

        public static PI TestStart   = Prod<MemA_andOptAssign> ( SEQ ( MG.MemA        , MG.DeclStar ,  MG.AssignVT ));
        public static PI TestStartRX = Prod<MemA_andOptAssign> ( SEQ ( MGRX.MemA      , MG.DeclStar ,  OR ( MG.AssignVT , EPSILON() )));
    }

     public class TestTR : TranslationUnit {
        public TestMG1.MemA_andOptAssign test_start_node {get;set; }

        public MemA_VBXTU                memaTR;
        public Assign_VBXTU              assignTR;
        
        public override VBoxTU[] VBoxTUs { get { return new VBoxTU[] { memaTR , assignTR } ; } }

        preCH pre_LHS ;
        
        public TestTR ( TestMG1.MemA_andOptAssign startNode, preCH pre_LHS ) {
            test_start_node = startNode;
            this.pre_LHS = pre_LHS;
            memaTR = new MemA_VBXTU ( pre_LHS , startNode.memavt_node );

            assignTR = startNode.assignvt_node == null ? null : new Assign_VBXTU ( memaTR.preCH_out , startNode.assignvt_node ) ;

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
        public string strMem  ; 
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
