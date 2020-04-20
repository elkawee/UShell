using System;


using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using SObject = System.Object;

using D = System.Diagnostics.Debug;
using MG = MainGrammar.MainGrammar;
using NamedNode = ParserComb.NamedNode;
using NLSPlain;

using UnityEngine;

using SuggestionTree;
using SGA = SuggestionTree.SuggTAdapter;


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

    public class Assign_VBXTU : VBoxTU_pIN_pOUT {
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

        public Assign_VBXTU ( preCH LHS , MG.AssignVTNode AssNode) {
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
            //                   primitive assign over a (RefT -> RefT)-edge 
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


    public class __depricated_MemA_VBXTU : VBoxTU_pIN_pOUT , VBoxTUMem {   // TOTAL! fucking! hackjob -- needs redoing ASAP 
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

            // todo : exceptions for GetField()/GetProperty() 

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
        

        public __depricated_MemA_VBXTU ( preCH LHS ,  MG.MemAVTNode MAVTNode  ) {
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
            var memb_kind_from_syntax  = MembK.Any() ;// new MembK_Filter( (children[0] as TermNode).tok.E );
            
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

    // // // // // // // // // // // // // // // // // // // // 
    // next up : do MemA_VBXTU properly 
    // // // // // // // // // // // // // // // // // // // // 

    public class MemA_VBXTU : VBoxTU_pIN_pOUT , VBoxTUMem { 
        /* not strictly needed, just for ease of debugging/reasoning 
         * if there is an execution order mishap of "collapsing preCHs before scoping" that _should_ _theoretically_ 
         * throw at the end of the preCH chain that starts with the preCH_in passed to the constructor of this type 
         * 
         * otoh : there are quite a lot of configurations that COULD derive types without scoping because they have no VarRefs in their preCH chain -- hmm hmm 
         */
        bool is_scoped = false;     

        public MG.MemANode memA_node = null ;
        public bool is_property  = false;
        MemberInfo MI = null ;

        MemberInfo VBoxTUMem.MI {  get {  if ( MI == null ) TypeDerive(); return MI; } } 

        public TTuple TypeDerive(){
            if( !is_scoped ) throw new Exception();

            BindingFlags BI = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding;

            Func<Type,string,MemberInfo> GetMemb = (teh_type,teh_name) => {
                var res = teh_type.GetMember(teh_name , BI );
                if ( res.Length != 1 ) throw new Exception();       
                // afaik: name ambiguity in c# is only possible via function signature overload -- and we don't do functions in this part of town 
                // overloading of getters with differing parameters? is this a thing ? 
                return res[0];                              
            };


            // and thus i can now collapse to CH and look at its type 
            Type lhsT = preCH_in.PayT;
            //                                                        todo : try-wrap for Reflection API specific stuff 
            Type out_payT = null;
            bool out_is_multi = false ;

            if ( memA_node.kind ==  MG.MemANode.kindE.any ) {
                try { 
                    MI = GetMemb( lhsT,memA_node.name ); 
                } catch ( Exception ) {                          // <- quickhack  GetMember(someName, .. ) doesn't find property of somename -- it matches against the mangled "get_$(someName)" special name 
                    MI = lhsT.GetProperty( memA_node.name ) ;
                }
                
                if      ( MI.MemberType == MemberTypes.Field )    { out_payT = ((FieldInfo)MI).FieldType       ; out_is_multi = ! out_payT.IsValueType ;  }
                else if ( MI.MemberType == MemberTypes.Property ) { out_payT = ((PropertyInfo)MI).PropertyType ; out_is_multi = ! out_payT.IsValueType ; is_property = true ; }
                else    throw new Exception();

            } else if ( memA_node.kind == MG.MemANode.kindE.ref_field ) {  // extra stanzas if the membtype-refinement was present, because that throws if within that subtype there was no member with that name 
                var FI = lhsT.GetField( memA_node.name , BI );
                if ( FI.FieldType.IsValueType ) throw new Exception();
                MI = FI;
                out_payT     = FI.FieldType;
                out_is_multi = false;
            } else if ( memA_node.kind == MG.MemANode.kindE.val_field ) {
                var FI = lhsT.GetField( memA_node.name , BI );
                if ( ! FI.FieldType.IsValueType ) throw new Exception();
                MI = FI;
                out_payT     = FI.FieldType;
                out_is_multi = false;
            } else if ( memA_node.kind == MG.MemANode.kindE.property ) { 
                var PI = lhsT.GetProperty( memA_node.name ); // todo property with parameters ( indexers and such ) - i wanted to map them to normalized c# interfaces, see SerializationTypeMapping.cs 
                MI = PI;
                out_payT     = PI.PropertyType;
                out_is_multi = ! PI.PropertyType.IsValueType;  // TODO: ... debateable 
                is_property  = true; 
            } else throw new Exception();

            return new TTuple { PayT = out_payT , isMulti = false }; /* <- HACK implicit uniqueness filter is TODO */ 

        }

        public MemA_VBXTU ( preCH preCH_in , MG.MemANode memA_node ) {
            this.memA_node         = memA_node ;
            this.backing_preCH_in  = preCH_in  ;
            this.backing_preCH_out = new deferred_preCH ( TypeDerive , this );
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) { 
            is_scoped = true ; 
            return c;
        }

        public override IEnumerable<OPCode> emit()
        {
            // ach figgn - ich wollte doch den implicit post-OPCode unique filter shizzle extra machen 
            /*
            if ( is_property ) {
                yield return OPGEN.MK_OP_MemA_PropSingle ( CH_in , CH_out , (PropertyInfo) MI ); 
            } else { 
                yield return OPGEN.MK_OP_MemA_FieldSingle( CH_in , CH_out , (FieldInfo)    MI );
            }
            */
            var dummy_CH = CH_out ; // trigger TypeDerive - which writes back to  "is_property" --- disgusting 
            if ( is_property ) {
                return new [] {  OPGEN.MK_OP_MemA_PropSingle ( CH_in , CH_out , (PropertyInfo) MI ) };
            } else {
                return new [] { OPGEN.MK_OP_MemA_FieldSingle( CH_in , CH_out , (FieldInfo)    MI ) };
            }
        }
    }



    public class RG_EdgeTU : TranslationUnit {
        public readonly MG.RG_EdgeNode rgEdgeNode ;

        public readonly __depricated_MemA_VBXTU    memATU ;
        public readonly Assign_VBXTU  asgTU;

        public          preCH     preCH_out => asgTU == null ? memATU.preCH_out : asgTU.preCH_out;

        public RG_EdgeTU ( preCH pLHS , MG.RG_EdgeNode nn ) {
            this.rgEdgeNode = nn ;
            memATU = new __depricated_MemA_VBXTU ( pLHS , nn.memAVT ) ;
            if ( nn.assignVT != null ) asgTU = new Assign_VBXTU ( memATU.preCH_out , nn.assignVT );
        }

        public override VBoxTU[]            VBoxTUs => memATU.VBoxTUs.Concat( asgTU == null ? new VBoxTU[0] : asgTU.VBoxTUs ).ToArray();

        public override IEnumerable<OPCode> emit()  => memATU.emit() .Concat( asgTU == null ? new OPCode[0] : asgTU.emit() );

        public override preCH_deltaScope scope(preCH_deltaScope c) {
            c = memATU.scope( c ) ;
            if ( asgTU != null ) c = asgTU.scope(c );
            return c ;
        }
    }

    #region SG

    public class SG_EdgeVBX_TU : VBoxTU_pIN_cOUT {  // dependend on in-Type , out-Type is always GameObject
        public bool is_immediate;
        public SG_EdgeVBX_TU ( preCH preCH_in , bool is_immediate ) {
            this.is_immediate = is_immediate;
            backing_preCH_in = preCH_in ;
            if ( is_immediate ) {
                backing_CH_out   = new TypedSingleCH<GameObject>();
            } else { 
                backing_CH_out   = new TypedMultiCH<GameObject>();
            }
        }

        public override IEnumerable<OPCode> emit()
        {
            if ( is_immediate ) yield return OPGEN.MK_SG_immediate ( CH_in , CH_out );
            else                yield return OPGEN.MK_OP_SG_all    ( CH_in , CH_out );
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) => c;
       
    }

    public class SG_EdgeTU : TranslationUnit
    {
        VBoxTU [] __VBoxTUs;
        public override VBoxTU[] VBoxTUs => __VBoxTUs;
        
        public TypedCH CH_out => __VBoxTUs.Last().CH_out;
        public preCH   preCH_in , preCH_out;
        public bool    is_immediate;

        public SG_EdgeTU ( preCH preCH_in , MG.SG_EdgeNode sg_edge_node ) {
            this.preCH_in = preCH_in;

            is_immediate = sg_edge_node.kind == MG.SG_EdgeNode.kindE.immediate;

            var SG_VBX_TU = new SG_EdgeVBX_TU(preCH_in , is_immediate );

            if ( sg_edge_node.typefilter == null ) {
                __VBoxTUs = new [] { SG_VBX_TU } ;
            } else { 
                var typfilter_VBX_TU = new TypeFilterVBX_TU ( SG_VBX_TU.preCH_out , sg_edge_node.typefilter ) ;
                __VBoxTUs = new VBoxTU[] { SG_VBX_TU , typfilter_VBX_TU };
            }


        }

        public override IEnumerable<OPCode> emit() => __VBoxTUs.SelectMany( vbxTU => vbxTU.emit() );


        public override preCH_deltaScope scope(preCH_deltaScope c) => c ; // 360

    }

    public class Root_SG_EdgeTU : TranslationUnit
    {

        public override VBoxTU[] VBoxTUs => opt_typefilter == null ? new VBoxTU[0] : new VBoxTU[] { opt_typefilter };
        public preCH    preCH_out        ;

        public OPCode   initial_OPCode;
        public TypedSingleCH<GameObject> SuiGen_CH_out = new TypedSingleCH<GameObject>(); // TODO: something to look out for with paramless constructor ?? 
        public TypeFilterVBX_TU opt_typefilter = null;

        public Root_SG_EdgeTU ( MG.SG_EdgeNode sg_edge_node ) {
            if ( sg_edge_node.kind == MG.SG_EdgeNode.kindE.immediate ) {

                initial_OPCode = OPGEN.MK_SuiGen( 
                    SuiGen_CH_out , 
                    (Func<Context,IEnumerable<GameObject>>) OPCode_AUX.NonInactiveRootsGO                   // can't cast method directly to System.Object ... why tho?
                    );
            } else { 
                initial_OPCode = OPGEN.MK_SuiGen( 
                    SuiGen_CH_out , 
                    (Func<Context,IEnumerable<GameObject>>) OPCode_AUX.NonInactiveObjectsOf<GameObject>    // same 
                    );
            }
            if ( sg_edge_node.typefilter != null ) {                                               // in this case a single OPCode - no edge - no nuthin john snow 
                opt_typefilter = new TypeFilterVBX_TU( new adapter_preCH( SuiGen_CH_out) ,sg_edge_node.typefilter );
                preCH_out = opt_typefilter.preCH_out;
            } else { 
                preCH_out = new adapter_preCH( SuiGen_CH_out );
            }
        }

        public override IEnumerable<OPCode> emit()
        {
            yield return initial_OPCode;
            if ( opt_typefilter != null ) foreach ( var opc in opt_typefilter.emit()) yield return opc;
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) => c;
       
    }

    #endregion 

    #region FanTU

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

    #endregion 


    #region Filter

    public class TypeFilterVBX_TU:VBoxTU_pIN_pOUT {
        public TypeFilterVBX_TU ( preCH preCH_in , string [] typefilter_names ) {
            this.backing_preCH_in = preCH_in;
            Func<TTuple> deferredTT = () => new TTuple {
                PayT = SGA.QTN_Exact ( typefilter_names ) ,   
                isMulti = false
            } ;
            this.backing_preCH_out = new deferred_preCH( deferredTT , dataSrc: this ) ;
        }


        public override IEnumerable<OPCode> emit() {
            yield return OPGEN.MK_ComponentFilter( CH_in , CH_out );  // opcode derives its stuff from typeargs alone 
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) => c ;
    }

    public static partial class AUX {
   
    }

    public class EqualsFilterTU : VBoxTU_pIN_pOUT
    {
        // public override VBoxTU[] VBoxTUs => throw new NotImplementedException();
        public preCH refTarget_pCH;         // in case of scopeRef - where the ref points to 
        public preCH ref_aux_pCH ;          // kinda like a tmp var - for easier translation 

        MG.EqualsFilterNode eq_filter_node ; 

        public EqualsFilterTU ( preCH preCH_in , MG.EqualsFilterNode eq_filter_node ) {
            this.eq_filter_node = eq_filter_node;

            // outType depends only on inType 
            backing_preCH_in  = preCH_in;
            backing_preCH_out = new deferred_preCH ( () => new TTuple { isMulti = false , PayT = preCH_in.PayT} , dataSrc: this );
        }


        public override preCH_deltaScope scope(preCH_deltaScope c)
        {
            if ( eq_filter_node.isRef ) return c.addRef ( eq_filter_node.RHS_ref.name , out refTarget_pCH ) ; // <- this sets auxCH , dataSrc for the auxCH is ???? 
            else return c ;
        }

        public override IEnumerable<OPCode> emit()
        {
            var L = new List<OPCode>();
            TypedCH  eq_RHS_CH ;
            if ( eq_filter_node.isSharpRef ) {
                var bshift = OPGEN.MK_BarrierShift( refTarget_pCH.CH , preCH_in.CH , ref_aux_pCH.CH );
                eq_RHS_CH = ref_aux_pCH.CH ;
                L.Add( bshift );
            } else if ( eq_filter_node.isDollarRef ) {
                eq_RHS_CH = refTarget_pCH.CH;
            } else { // JSON 
                Type ser_target_type = preCH_in.PayT;
                eq_RHS_CH = preCH.Instantiate( new TTuple{ PayT = ser_target_type , isMulti = false } , this );
                SObject ser_payload = TypeMapping.LightJSonAdapter.FromJson( eq_filter_node.json , ser_target_type ) ;
                var const_op = OPGEN.MK_const ( eq_RHS_CH , ser_payload ) ;

            }
            var eq_filter = OPGEN.MK_EqualsFilter_SingleC ( CH_in ,  eq_RHS_CH , (SingleCH)  CH_out , true ) ;
            L.Add(eq_filter);
            return L;
        }

    }

    public class FilterTU : TranslationUnit
    {
        List<VBoxTU> L = new List<VBoxTU>();
        public override VBoxTU[] VBoxTUs => L.ToArray();
        public preCH preCH_out;

        public FilterTU ( preCH preCH_in , MG.FilterNode filter_node ) {
            var ch_node = filter_node.children[0];
            if( ch_node is MG.TypeNameNode ) {

                var sub_tu = new TypeFilterVBX_TU (preCH_in , ((MG.TypeNameNode) ch_node).names);
                L.Add( sub_tu );
                preCH_out = sub_tu.preCH_out;

            } else if ( ch_node is MG.EqualsFilterNode ) {

                var sub_tu = new EqualsFilterTU ( preCH_in ,  (MG.EqualsFilterNode) ch_node );
                L.Add( sub_tu );
                preCH_out = sub_tu.preCH_out;

            } else {
                throw new NotImplementedException();
            }
           
        }

        public override IEnumerable<OPCode>  emit()                     => VBoxTUs.SelectMany( _ => _.emit() );
        public override preCH_deltaScope     scope(preCH_deltaScope c)  => c;
        
    }

    #endregion 

    public class PrimitveStepTU : TranslationUnit
    {
        public override VBoxTU[] VBoxTUs => primaryTU.VBoxTUs.Concat( assigns.SelectMany ( asg => asg.VBoxTUs)).ToArray();

        TranslationUnit      primaryTU         = null; 
        preCH                primary_preCH_out = null ;
        List<Assign_VBXTU>   assigns           = new List<Assign_VBXTU>();
        MG.PrimitiveStepNode primtv_step_node  = null;
        
        public preCH                preCH_out         = null;

        public PrimitveStepTU ( preCH preCH_in , MG.PrimitiveStepNode primtv_step_node ) {
            this.primtv_step_node = primtv_step_node;       //
            var primaryNN = primtv_step_node.children[0];
            if        ( primaryNN is MG.SG_EdgeNode ) {

                primaryTU           = new SG_EdgeTU( preCH_in , (MG.SG_EdgeNode) primaryNN ) ;
                primary_preCH_out   = ((SG_EdgeTU) primaryTU).preCH_out;

            } else if ( primaryNN is MG.MemANode ) {
                var MemTU = new MemA_VBXTU(preCH_in , (MG.MemANode) primaryNN) ;
                primaryTU = MemTU;
                primary_preCH_out = MemTU.preCH_out;
            
            } else if ( primaryNN is MG.FilterNode ) {
                var filterTU = new FilterTU( preCH_in , (MG.FilterNode) primaryNN );
                primaryTU = filterTU;
                primary_preCH_out = filterTU.preCH_out;
            } else if ( primaryNN is MG.FanNode ) {
                var fanTU = new FanTU( preCH_in , (MG.FanNode) primaryNN) ;
                primaryTU = fanTU;
                primary_preCH_out = fanTU.pRHS;
            } else throw new Exception();  // grammar changed or parser bug 

            preCH current_preCH_out = primary_preCH_out;
            foreach( var asgn_node in primtv_step_node.assigns) {
                var asgn_TU = new Assign_VBXTU( current_preCH_out , asgn_node );
                assigns.Add( asgn_TU);
                current_preCH_out = asgn_TU.preCH_out;
            }
            preCH_out = current_preCH_out;
        }

        public override preCH_deltaScope scope(preCH_deltaScope c)
        {
            c = primaryTU.scope(c);
            foreach ( var decl_name in primtv_step_node.primary_decl_node.decls ) c = c.decl(decl_name , primary_preCH_out );
            foreach ( var assTU in assigns) c = assTU.scope(c);
            return c;
        }

        public override IEnumerable<OPCode> emit() => VBoxTUs.SelectMany( _ => _.emit());
        

    }


    public class ProvStartTU : TranslationUnit
    {
        public override VBoxTU[]    VBoxTUs => root_SG_edge.VBoxTUs.Concat( prim_Steps.SelectMany( ps => ps.VBoxTUs ) ).ToArray();
        public Root_SG_EdgeTU       root_SG_edge;
        public PrimitveStepTU []    prim_Steps;

        public preCH                preCH_out;

        public ProvStartTU ( MG.ProvStartNode start_node ) {
            root_SG_edge = new Root_SG_EdgeTU( start_node.startSG );
            var L = new List<PrimitveStepTU>();
            preCH current_preCH = root_SG_edge.preCH_out;
            foreach ( var prim_step in start_node.primSteps ) {
                var TU = new PrimitveStepTU( current_preCH, prim_step  );
                L.Add( TU  );
                current_preCH = TU.preCH_out;
            }
            prim_Steps = L.ToArray();
        }

        public override IEnumerable<OPCode> emit() => root_SG_edge.emit().Concat( prim_Steps.SelectMany( ps => ps.emit() ) );


        public override preCH_deltaScope scope(preCH_deltaScope c)
        {
            foreach ( var ps in prim_Steps ) c = ps.scope(c);
            return c;
        }
    }


}