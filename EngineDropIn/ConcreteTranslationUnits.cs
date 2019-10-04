using System;


using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using SObject = System.Object;

using D = System.Diagnostics.Debug;
using MG = MainGrammar.MainGrammar;
using NLSPlain;

//#if !mock_NamedNode_Types
using SuggestionTree;
using SGA = SuggestionTree.SuggTAdapter;
//#endif

namespace TranslateAndEval {

    public struct MemA_Anchor {   
        /*
            wraps calculation of mem-access entry point , all fields are return values 
            ( neccesary for assignment ) 
            */
        public TypedCH CH_target , CH_Anchor ;
        public List<MemberInfo> MemEdges ;
        public int VBoxEdgeCount ;

        public MemA_Anchor ( TypedCH CH_target) {
            this.CH_target = CH_target;
            TypedCH currentCH = CH_target;
            MemEdges = new List<MemberInfo>();
            VBoxEdgeCount = 0;

            while ( true ) {
                VBoxTU TR = currentCH.DataSrc;
                if ( TR == null ) throw new Exception();    // Anchor not found but chain at end 
                VBoxEdgeCount += 1 ;
                if ( TR is VBoxTUMem ) MemEdges.Add( (TR as VBoxTUMem).MI );
                if ( TR.CH_in.ttuple.PayT.IsClass ) {
                    CH_Anchor = TR.CH_in;
                    break ;
                }
                currentCH = TR.CH_in ;
            }
        }
    }

    public class AssignTR : VBoxTU_pIN_pOUT {
        MG.AssignVTNode AsgNode { get ; set ;}

        

        
        preCH preCH_ref ; // from scope 
        public bool fromConstant => AsgNode.SAN.type == MG.SingleAssignNode.typeE.json ;

        /*
            assignments are supposed to be completely transparent - except for their values of course 
            e.g. there is no detectable difference between : 
            ..some_int_field <- @3.1415 <restExpr>        and       ..some_int_field <restExpr>
            for <restExpr>, particularly for its in-typing

            ergo: derive out-type soley by copying the in-type 
        */

        public AssignTR ( preCH LHS , MG.AssignVTNode AssNode) {
            this.AsgNode = AssNode;
            backing_preCH_in  = LHS;
            backing_preCH_out  = new deferred_preCH ( () => new TTuple { PayT = preCH_in.PayT , isMulti = false } , dataSrc: this  );
        }

        public override preCH_deltaScope scope(preCH_deltaScope SC) {
            if ( AsgNode.SAN.type == MG.SingleAssignNode.typeE.dollar ||
                 AsgNode.SAN.type == MG.SingleAssignNode.typeE.sharp  ) 
            {
                /*
                    in this case payload types of in_Column and ref_column might differ, but must : ( in_Type isAssignableFrom ref_Type ) 
                */
                    SC = SC.addRef( AsgNode.SAN.name , out preCH_ref) ;                   // might throw ScopeFindException : ScopeException  -- decision on handling needs to be elswhere though
                    
            } else {                     // json constant 
                /* 
                    in this case the json adapter is tasked with producing the exact type of the assinment target 
                    interesting edge case : 
                    one would expect

                    @some_val_1 -> x ; < Expr_1 >  ..some_field_1 <- $x                 to behave identically to :
                                       < Expr_1 >  ..some_field_1 <- @some_val_1
                    
                    with regards to success of type conversion - this needs special attention in the json_adapter parts
                    ( this also means, that there might be cases in which x can't be typed finally. when it is free_standing :  in an interactive session : 
                      like : 
                      @[ 1,2,3 ] -> X <ENTER>
                      ) ... allow JSonVal as type for variables ? ... treat them specially ? ... special column type ? special form of IsAssignableFrom for the normalized subset of c#  types 
                   
                */ 
                // case of assigning the a constant dircectly to a field , the exact type needed is known directly 
                preCH_ref = new deferred_preCH ( () => new TTuple { PayT = preCH_in.PayT , isMulti = false } , dataSrc: null );

            }
            foreach ( string decl in AsgNode.decls ) SC = SC.decl ( decl , preCH_out) ;
            return SC;
        }
        
       
        public override IEnumerable<OPCode> emit ( ) {
            var CH_tmp  = preCH_ref.CH;
            var opcodes = new List<OPCode>();
            if ( fromConstant ) {
                SObject Value = TypeMapping.LightJSonAdapter.FromJson( 
                    (LightJson.JsonValue)AsgNode.SAN.JSonVal,
                    CH_tmp.ttuple.PayT ) ; 
                opcodes.Add ( OPGEN.MK_const( CH_tmp , Value ) );
            }

            var Anchor = new MemA_Anchor( CH_in ) ;                                // CH_in or CH_out doesn't matter for the anchor finding algorithm 
                                                                                   // it does matter for the OPCode - it assumes edges are counted from CH_in's boxes ( CH_out's are created by itself ) 
            Action<VBox,SObject> PrimA_RefRef = (vbox,val_obj) => {
                for ( int i = 0 ; i<Anchor.VBoxEdgeCount ; i ++ ) {
                    vbox = (vbox as VBoxSingle).pred();
                }
                D.Assert( Anchor.MemEdges.Count() == 1 ) ;
                (Anchor.MemEdges[0] as FieldInfo).SetValue( vbox.value() , val_obj );   
            } ;

            opcodes.Add ( OPGEN.MK_OP_Assign_Dollar(CH_in,CH_out,preCH_ref.CH,PrimA_RefRef) );

            // todo ... all the other cases 
            return opcodes ;
        }
    }


    public class MemATU : VBoxTU_pIN_pOUT , VBoxTUMem {   // TOTAL! fucking! hackjob -- needs redoing ASAP 
        MG.MemAVTNode MAVTNode { get ; set;}

        public string fieldName ;

        bool isField ;
        FieldInfo _FI;
        PropertyInfo _PI;
        public FieldInfo FI    { get { fetchFieldOrProp() ; return _FI;   } }  // either of which might be null 
        public PropertyInfo PI { get { fetchFieldOrProp() ; return _PI;   } }

        bool fetched = false ;
        void fetchFieldOrProp () {
            if ( fetched ) return ;           // execute max once 
            fetched = true; 

            Type inType = CH_in.ttuple.PayT;  // trigger inference and nail down input type 

            new { type = inType , requested_name = fieldName }.NLSend("fetch Field or Prop" );

            // try as Fieldname 
            _FI = inType.GetField(fieldName);
            if ( _FI != null ) { isField = true;  return ; }
            // try as PropName 
            _PI = inType.GetProperty( fieldName ) ;
            if ( _PI != null ) { isField = false ; return ; }
            // none found
            throw new Exception();
        }

        public MemberInfo MI { get { fetchFieldOrProp() ; return  isField ? (MemberInfo)_FI : (MemberInfo)_PI ; } }
        

        public MemATU ( preCH LHS ,  MG.MemAVTNode MAVTNode  ) {
            this.MAVTNode = MAVTNode ;
            fieldName = MAVTNode.name;
            backing_preCH_in = LHS ;

            Func<TTuple> deferredTT = () => {
                fetchFieldOrProp();
                return new TTuple {
                    PayT =  isField ? FI.FieldType : PI.PropertyType ,
                    isMulti = false };
            };

            backing_preCH_out = new deferred_preCH ( deferredTT , dataSrc: this );
        }
#if !mock_NamedNode_Types
        public MemberInfo [] MembAC () {
            // TODO copy pasta from OldGrammar 
            var memb_kind_from_syntax  = MembK_Filter.Any ;// new MembK_Filter( (children[0] as TermNode).tok.E );
            
            try {
                return SGA.MembAC(CH_in.ttuple.PayT,MAVTNode.name, memb_kind_from_syntax);
            } catch ( Exception ) {  throw new MG.NoACPossible(); }
        }
#endif

        public override preCH_deltaScope scope(preCH_deltaScope c) {
            foreach ( string decl_name in MAVTNode.decls ) c = c.decl( decl_name , preCH_out ) ;  
            return c;
        }
      
        public override  IEnumerable<OPCode> emit () {
            fetchFieldOrProp();
            if ( isField )  return new OPCode[] { OPGEN.MK_MemA_RefRef ( CH_in , CH_out , FI) };
            else            return new OPCode[] { OPGEN.MK_MemA_RefProp( CH_in , CH_out, PI ) } ;
        }
    }

    public class TypeFilterVBX_TU:VBoxTU_pIN_pOUT {
        public TypeFilterVBX_TU ( preCH preCH_in , string [] typefilter_names ) {
            this.backing_preCH_in = preCH_in;
            Func<TTuple> deferredTT = () => new TTuple {
                PayT = SGA.QTN_Exact ( typefilter_names ) ,   // Todo TypeNameNode needs to support namespacing 
                isMulti = false
            } ;
            this.backing_preCH_out = new deferred_preCH( deferredTT , dataSrc: this ) ;
        }


        public override IEnumerable<OPCode> emit() {
            yield return OPGEN.MK_ComponentFilter( CH_in , CH_out );  // opcode derives its stuff from typeargs alone 
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) => c ;
    }


    public class RG_EdgeTU : TranslationUnit {
        public readonly MG.RG_EdgeNode rgEdgeNode ;

        public readonly MemATU    memATU ;
        public readonly AssignTR  asgTU;

        public          preCH     preCH_out => asgTU == null ? memATU.preCH_out : asgTU.preCH_out;

        public RG_EdgeTU ( preCH pLHS , MG.RG_EdgeNode nn ) {
            this.rgEdgeNode = nn ;
            memATU = new MemATU ( pLHS , nn.memAVT ) ;
            if ( nn.assignVT != null ) asgTU = new AssignTR ( memATU.preCH_out , nn.assignVT );
        }

        public override VBoxTU[]            VBoxTUs => memATU.VBoxTUs.Concat( asgTU == null ? new VBoxTU[0] : asgTU.VBoxTUs ).ToArray();

        public override IEnumerable<OPCode> emit()  => memATU.emit() .Concat( asgTU == null ? new OPCode[0] : asgTU.emit() );

        public override preCH_deltaScope scope(preCH_deltaScope c) {
            c = memATU.scope( c ) ;
            if ( asgTU != null ) c = asgTU.scope(c );
            return c ;
        }
    }



    public class FanElemTU:TranslationUnit {
        public readonly MG.FanElemNode fanElemNode ;
        public readonly RG_EdgeTU []      rgEdge_TUs ;
        public readonly preCH          pLHS ;
        public preCH                   pRHS => rgEdge_TUs.Length == 0 ?  pLHS : rgEdge_TUs.Last().preCH_out ;

        public FanElemTU ( preCH pLHS , MG.FanElemNode fanElemNode ) {
            this.fanElemNode = fanElemNode;
            this.pLHS = pLHS;
            var  L = new List<RG_EdgeTU>();

            preCH currentLHS = pLHS;
            foreach ( var rg_EdgeNode in fanElemNode.rgEdges ) {
                var rgEdgeTU = new RG_EdgeTU(currentLHS , rg_EdgeNode );
                currentLHS = rgEdgeTU.preCH_out;
                L.Add( rgEdgeTU );
            }
            rgEdge_TUs = L.ToArray();
        }

        public override VBoxTU[] VBoxTUs => rgEdge_TUs.SelectMany( _ => _.VBoxTUs ).ToArray();

        public override preCH_deltaScope scope(preCH_deltaScope c) 
            { foreach ( var rgEdgeTU in rgEdge_TUs ) c = rgEdgeTU.scope( c ) ; return c ; }

        public override IEnumerable<OPCode> emit() 
            {  foreach ( var rgEdgeTU in rgEdge_TUs ) foreach ( var opC in rgEdgeTU.emit() ) yield return opC; }
    }
    /*
        todo : 
        there should be legal decls immediately following a fan " { ... } -> z ", not yet sure where to put them  
        ( assigment? - the LHS is not neccesarily a c# reference type 
          ... and if it was it would invalidate everything that happend in the fan - still not enough reason to forbid it ... ) 
    */

    public class FanTU:TranslationUnit {           
        public readonly preCH          pLHS ;
        public          preCH          pRHS  => fanElemTUs.Length == 0 ?  pLHS : fanElemTUs.Last().pRHS ;
        public readonly FanElemTU []   fanElemTUs;
        public readonly MG.FanNode     fanNode;

        public class BShiftVBTR:VBoxTU_pIN_pOUT {
            
            

            public preCH pCH_shiftOrig ;

            public BShiftVBTR ( preCH pCH_lhs , preCH pCH_shiftOrig) {
                this.backing_preCH_in = pCH_lhs ;
                this.pCH_shiftOrig = pCH_shiftOrig;
                this.backing_preCH_out = new deferred_preCH ( () => new TTuple { PayT = pCH_shiftOrig.PayT , isMulti =false }, dataSrc: this );
            }

            public override preCH_deltaScope scope(preCH_deltaScope c) => c ; // this thing can't decl and doesnt ref 

            public override IEnumerable<OPCode> emit() {
                return new OPCode[] { OPGEN.MK_BarrierShift( pCH_shiftOrig.CH , CH_in , preCH_out.CH ) } ;
            }

        }

        
        List<BShiftVBTR>  BShifts = new List<BShiftVBTR>();

        public FanTU (preCH pLHS , MG.FanNode fanNode ) {
            this.pLHS            = pLHS;
            this.fanNode         = fanNode;

            preCH           fanElem_LHS_pCH       = pLHS;  // in-column for the [ fan-elem , barrier-shift] group 

            // defensive programming : assume FanElems to be able to be empty of VBoxTUs even if they might end up not being in the final grammar 
            // this does pose a problem with the dataSrc field - cuz it could be the in-data src of the whole fan 

            var LfanElemTUs = new List<FanElemTU>();

            VBoxTU current_dataSrc = pLHS.dataSrc;

            foreach ( var fanElemNode in fanNode.elems ) {
                var currFanElemTU = new FanElemTU( fanElem_LHS_pCH , fanElemNode );

                LfanElemTUs.Add ( currFanElemTU );
                var BShift = new BShiftVBTR( currFanElemTU.pRHS ,  fanElem_LHS_pCH ) ;  // only need to go back to the last BShift 
                BShifts.Add ( BShift );
                fanElem_LHS_pCH = BShift.preCH_out;
            }
            fanElemTUs = LfanElemTUs.ToArray();
        }

        public override preCH_deltaScope scope(preCH_deltaScope pdScope) {
            foreach ( var fETU in fanElemTUs )  pdScope = fETU.scope ( pdScope );
            return pdScope;
        }


        public override IEnumerable<OPCode> emit() {
            return fanElemTUs
                .Zip( BShifts , 
                                (fElem , bShift )=> fElem.emit().Concat( bShift.emit() ) )
                .SelectMany(_=>_);
        }
        public override VBoxTU[] VBoxTUs => fanElemTUs.Zip( BShifts , ( fE ,bShift ) => fE.VBoxTUs.Concat( bShift.VBoxTUs ) ).SelectMany( _=>_).ToArray();

    }



}