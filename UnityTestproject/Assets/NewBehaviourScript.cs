using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SGA = SuggestionTree.SuggTAdapter;
using MainGrammar;
using MG = MainGrammar.MainGrammar;
using ParserComb;
using TranslateAndEval;
using System;

namespace InUnityTest1 { 

    class TestGrammar : MG {
        public class StartNode : NamedNode {
            public MG.SG_EdgeNode sg_edge_node ;
            public MG.RG_EdgeNode rg_edge_node ;
            public override void build() {
                sg_edge_node = ( MG.SG_EdgeNode ) children[0];
                rg_edge_node = ( MG.RG_EdgeNode ) children[1];
            }

        }
        public static PI StartProd = Prod<StartNode> ( SEQ ( SG_Edge , RG_Edge ) ) ;
    }

    class ROOT { } // dummy type has no fields , never instanced 

    class StartTU:TranslationUnit {
        public class RootVBoxTR:VBoxTU_cIN_cOUT {                                      // making a VBoxTR for this is actually quite retarded/superfluous -- no it's not! stop bugging me ! 
                                                                                       // but OPSuiGen produces no incoming edges - you would expect there to be edges between the in column an out column of a VBox ! 
            TypedSingleCH<ROOT>       rootCH  { get { return (TypedSingleCH<ROOT>)       backing_CH_in;  } set { backing_CH_in = value ; } }
            TypedSingleCH<GameObject> GO_cOut { get { return (TypedSingleCH<GameObject>) backing_CH_out; } set { backing_CH_out = value ; } } 
            
            
            
            public bool descend_all ;
            public RootVBoxTR ( bool descend_all ) {
                this.descend_all = descend_all;
                rootCH  = new TypedSingleCH<ROOT>();
                GO_cOut = new TypedSingleCH<GameObject>();
            }

            public static IEnumerable<GameObject> desc_all(Context ctx ) {
                return (IEnumerable<GameObject>)Resources.FindObjectsOfTypeAll<GameObject>( );
            }
            public static IEnumerable<GameObject> layer_one(Context ctx) {
                return desc_all(ctx).Where( GO => GO.transform.parent == null ) ;
            }

            public override IEnumerable<OPCode> emit() {
                System.Func < Context, IEnumerable<GameObject>> fun ;
                if ( descend_all ) fun = desc_all ;
                else               fun = layer_one;
                return new [] {  new OP_SuiGen<GameObject>( GO_cOut , fun) } ;
            }

            public override preCH_deltaScope scope(preCH_deltaScope c) {
                return c;
            }
        }

        RG_EdgeTU           rg_edgeTU ;
        RootVBoxTR          rootVBX;
        TypeFilterVBX_TU    typefilterVBX;
        bool  isDescAll ;

        public StartTU ( TestGrammar.StartNode startNode ) {
            isDescAll = startNode.sg_edge_node.kind == MG.SG_EdgeNode.kindE.all ;

            rootVBX   = new RootVBoxTR ( isDescAll );

            if(startNode.sg_edge_node.typefilter != null) {
                typefilterVBX = new TypeFilterVBX_TU(rootVBX.preCH_out,new[] { startNode.sg_edge_node.typefilter } );
            }

            rg_edgeTU = new RG_EdgeTU ( 
                typefilterVBX == null ? rootVBX.preCH_out : typefilterVBX.preCH_out, 
                startNode.rg_edge_node );
        }


        public override VBoxTU[] VBoxTUs { 
            get { return 
                    new VBoxTU [] { rootVBX }.Concat( rg_edgeTU.VBoxTUs ).ToArray() ;
            }
        }

        public override IEnumerable<OPCode> emit() {
            foreach ( var opc in rootVBX.emit()   ) yield return opc ;
            if ( typefilterVBX != null ) foreach ( var opc in typefilterVBX.emit() ) yield return opc ;
            foreach ( var opc in rg_edgeTU.emit() ) yield return opc ;
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) {
            return rg_edgeTU.scope( c ) ;
        }
    }


    public class NewBehaviourScript : MonoBehaviour {

	    void Start () {
            foreach ( var name in SGA.MembAC( typeof ( Transform ) , "p" , SuggestionTree.MembK_Filter.Any ).Select( mi => mi.Name )  ) { 
		        Debug.Log ( name ) ;
            }

            // ----------------------------------------

            var GE = new GrammarEntry() {
                StartProd = TestGrammar.StartProd ,
                TR_constructor = (nn ) => new StartTU ( ( TestGrammar.StartNode ) nn  ) 
            };
            var TLHS = new TranslateLHS() {
                preCH_LHS = null ,
                scope = new CH_closedScope() 
            };
            var compilat = TranslateEntry.TranslateFully( ">> :Transform ..position " , GE , TLHS) ;
            var MM = new MemMapper() ;
            var res = Evaluate.Eval(compilat , MM ) ;
            Analyz0r.A.JsonifyEval(compilat , MM ) ;
            Analyz0r.A.JsonifyCompilat(compilat ) ;
            Debug.Log ( "DIIIIIIIIIIIIIIIInGs" +  string.Join( "--" , res.values.Select ( _=>_.ToString() ). ToArray()  ) ) ;

	    }

	    void Update () {
		
	    }



    }


}