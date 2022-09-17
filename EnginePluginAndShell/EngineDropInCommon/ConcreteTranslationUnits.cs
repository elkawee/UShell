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
            /*
                Search backwards until a reference type is found ( relies on ColumnHeaders paralleling Instantiated Columns in layout ) 
                - the check for .IsClass : could it being independent from isMemEdge cause problems? ( this only searches for a reference to do in place mutati, ColumnLayouts _should_ be completely independent of that  ) 
            */
            while ( true ) {  
                VBoxTU TR = currentCH.pred_SrcVBXTU;
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




    public class Assign_VBXTU : VBoxTU_pIN_pOUT {
        public MG.AssignVTNode AsgNode { get ; set ;}

        

        
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
                /*SObject Value = TypeMapping.LightJSonAdapter.FromJson( 
                    (LightJson.JsonValue)AsgNode.SAN.JSonVal,
                    CH_tmp.ttuple.PayT ) ; */
                SObject Value = AsgNode.SAN.capsule.DESER(CH_tmp.ttuple.PayT );
                opcodes.Add ( OPGEN.MK_const( CH_tmp , Value ) );
            }

            // dunno much about these lambdas, might be better to push this functionality into individual OPCodes 

            var Anchor = new MemA_Anchor( CH_in ) ;                                // CH_in or CH_out doesn't matter for the anchor finding algorithm 
                                                                                   // it does matter for the OPCode - it assumes edges are counted from CH_in's boxes ( CH_out's are created by itself ) 

            D.Assert( Anchor.MemEdges.Any() );  // allways ... obviously 

            Action<VBox,SObject> PrimAssg = null ;

            if ( Anchor.MemEdges[0] is FieldInfo ) {                                //    primitive assign over a (RefT -> RefT)-edge 

                if ( Anchor.MemEdges.Count() > 1 ) throw new NotImplementedException(); // todo 
                PrimAssg = (vbox,val_obj) => {
                    for ( int i = 0 ; i<Anchor.VBoxEdgeCount ; i ++ ) {    // walk back to instance of closest reference type ( superfluous atm ) 
                        vbox = (vbox as VBoxSingle).pred();
                    }
                    (Anchor.MemEdges[0] as FieldInfo).SetValue( vbox.value() , val_obj );   
                } ;
            } else if ( Anchor.MemEdges[0] is PropertyInfo ) {
                if ( Anchor.MemEdges.Count() > 1 ) throw new NotImplementedException(); // todo 
                PrimAssg = ( vbox , val_obj ) => {
                    VBox pred = (vbox as VBoxSingle).pred();                // again ... todo 
                    var set_meth = (Anchor.MemEdges[0] as PropertyInfo ).GetSetMethod();
                    if ( set_meth == null ) throw new Exception("trying to invoke setter on a read only property");  // Todo - this should be cought in translation 

                    set_meth.Invoke( pred.value() , new object[] { val_obj } );  
                };
            } else { 
                throw new NotImplementedException(); // todo 
            }

            // calling properties on value types ??? <- needs rt-emission of IL assembly - see experiments/PropertiesOnValueTypes 

            opcodes.Add ( OPGEN.MK_OP_Assign_Dollar(CH_in,CH_out,preCH_ref.CH,PrimAssg) );

            // todo ... all the other cases 
            return opcodes ;
        }
    }



    public class 
    MemA_VBXTU : VBoxTU_pIN_pOUT , VBoxTUMem { 
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

            BindingFlags BI = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding | BindingFlags.Instance | BindingFlags.Static;

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

        public readonly PrimitveStepTU [] primStep_TUs;
        public readonly preCH          pLHS ;
        public preCH                   pRHS => primStep_TUs.Length == 0 ?  pLHS : primStep_TUs.Last().preCH_out ;  // the grammar does not allow the len=0 case, .. but eh ... 

        public FanElemTU ( preCH pLHS , MG.FanElemNode fanElemNode ) {
            this.fanElemNode = fanElemNode;
            this.pLHS = pLHS;

            var L = new List<PrimitveStepTU>();
            preCH current_LHS = pLHS ;
            foreach ( var primStepNode in fanElemNode.primStepNodes  ){
                var primStepTU = new PrimitveStepTU( current_LHS , primStepNode ) ;
                current_LHS = primStepTU.preCH_out;
                L.Add(primStepTU);
            }
            primStep_TUs = L.ToArray();

        }

        public override VBoxTU[] VBoxTUs => primStep_TUs.SelectMany( _ => _.VBoxTUs ).ToArray();

        public override preCH_deltaScope scope(preCH_deltaScope c) 
            { foreach ( var primStepTU in primStep_TUs ) c = primStepTU.scope( c ) ; return c ; }

        public override IEnumerable<OPCode> emit() 
            {  foreach ( var primStepTU in primStep_TUs ) foreach ( var opC in primStepTU.emit() ) yield return opC; }
    }
    /*
        todo : 
        there should be legal decls immediately following a fan " { ... } -> z ", not yet sure where to put them  
        ( assigment? - the LHS is not neccesarily a c# reference type 
          ... and if it was it would invalidate everything that happend in the fan - still not enough reason to forbid it ... ) 
    */

    public class BShiftVBoxTU:VBoxTU_pIN_pOUT {

        public preCH pCH_shiftOrig ;

        public BShiftVBoxTU ( preCH pCH_lhs , preCH pCH_shiftOrig) {
            this.backing_preCH_in = pCH_lhs ;
            this.pCH_shiftOrig = pCH_shiftOrig;
            this.backing_preCH_out = new deferred_preCH ( () => new TTuple { PayT = pCH_shiftOrig.PayT , isMulti =false }, dataSrc: this );
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) => c ; // this thing can't decl and doesnt ref 

        public override IEnumerable<OPCode> emit() {
            return new OPCode[] { OPGEN.MK_BarrierShift( pCH_shiftOrig.CH , CH_in , preCH_out.CH ) } ;
        }

    }


    public class FanTU:TranslationUnit {           


        /*
            In general " { FanElem1 , FanElem2 , ... } " is semantically identical to : " {FanElem1} {FanElem2} ... " 
            thus every comma separated FanElem gets its own BShift
            types of these BShifts are identical 
            contents of these Columns are identical too, save for filtering 
            ( a BShift column is an ordered subset of all BShift columns to its left belonging to the same Fan ) 
        */


        public readonly MG.FanNode     fanNode;
        public readonly preCH          pLHS ;
        public readonly preCH          pRHS ;

        public readonly FanElemTU       []   fanElemTUs;
        public readonly BShiftVBoxTU    []   bShiftTUs;
        public readonly TranslationUnit []   TUs;        // all generated top-level TUS in execution order [ fanElem Bshift fanElem BShift ... ] 
        

       

        public FanTU (preCH pLHS , MG.FanNode fanNode ) {
            this.pLHS            = pLHS;
            this.fanNode         = fanNode;

            

            // defensive programming : assume FanElems to be able to be empty of VBoxTUs even if they might end up not being in the final grammar 
            // this does pose a problem with the dataSrc field - cuz it could be the in-data src of the whole fan 

            var L_FanElems  = new List<FanElemTU>();
            var L_BShifts   = new List<BShiftVBoxTU>();
            var L_allTUs    = new List<TranslationUnit>();

            preCH           fanElem_LHS_pCH       = pLHS;  // in-column for the [ fan-elem , barrier-shift] group 
            
            foreach ( var fanElemNode in fanNode.elems ) {
                var currFanElemTU = new FanElemTU( fanElem_LHS_pCH , fanElemNode );
                L_FanElems.Add ( currFanElemTU );
                L_allTUs.Add( currFanElemTU );

                var BShift = new BShiftVBoxTU( currFanElemTU.pRHS ,  fanElem_LHS_pCH ) ;  // only need to go back to the last BShift ( lastFanElem.lhsCH == rhsCH of the BShift that preceded it , or the in-CH of the entire FAN if first FanElem  ) 
                L_BShifts.Add ( BShift );
                L_allTUs.Add(BShift);
                fanElem_LHS_pCH = BShift.preCH_out;
            }
            fanElemTUs = L_FanElems.ToArray();
            bShiftTUs  = L_BShifts.ToArray();
            TUs        = L_allTUs.ToArray();

            pRHS = fanElem_LHS_pCH; // === rhsCH of last BShift or FAN-lhsCH if 0-elem fan 
        }

        public override preCH_deltaScope scope(preCH_deltaScope pdScope) {
            foreach ( var fETU in fanElemTUs )  pdScope = fETU.scope ( pdScope );  // only fan elems - cuz BShifts are phantom tingies with no relation to syntactic elements that don't do, or use any scoping thingamabobs 
            return pdScope;
        }


        public override IEnumerable<OPCode> emit() {
            return TUs.SelectMany ( tu => tu.emit());
        }
        public override VBoxTU[] VBoxTUs => TUs.SelectMany( _ => _.VBoxTUs ).ToArray();

    }

    #endregion 

    #region FrameTU 

    /*
        for the frame it is actually sensible to have a distinct TU per FrameElementNode
        as the columns that make up the "data frame columns" are the CH_out of each FrameElement 
    */
    public class FrameElementTU : TranslationUnit {
        public preCH            preCH_out  ;
        
        public PrimitveStepTU[] primStepTUs;
        public MG.FrameElemNode frameElementNode;


        

        public bool isConst => frameElementNode.isConstant ;
        public bool isRef   => frameElementNode.isRef;

        public JsonLitSpreadVBXTU ConstantTU ;
        public VarRefVBXTU        VarRefTU ;

        public Type     JsonLit_deserialization_target_type { get { return ConstantTU.JsonLit_deserialization_target_type; } set { ConstantTU.JsonLit_deserialization_target_type = value;  }  }
        public SObject  JsonLit_deserialized_value          { get { return ConstantTU.JsonLit_deserialized_value;          } set { ConstantTU.JsonLit_deserialized_value          = value;  }  }

        public FrameElementTU( preCH preCH_in , MG.FrameElemNode frameElementNode ) { 
            
            /*
                preCH_out ist entweder deferred bis nach der half-typing prozedur ,
                oder deferred in abhaengigkeit von den linksseitigen Columns 
            */
            
            this.frameElementNode = frameElementNode;
            
            

            if ( isConst ) { 
                ConstantTU = new JsonLitSpreadVBXTU( preCH_in ); 
                preCH_out  = ConstantTU.preCH_out ;
            } else if ( isRef ) {  
                VarRefTU   = new VarRefVBXTU( preCH_in , frameElementNode.refNode );
                preCH_out  = VarRefTU.preCH_out ;
            } else { 
                /*
                    for AC it is vital that (!is.Constant <==> "typeable without half-type fully-type" steps ) 
                    atm, this is true, but in a very fragile way 
                    preCH_ins of non-constant primStepTU's depend on : 
                        - the Frame-LHS ( first column ) 
                        - the BShift, which depends only on the Frame-LHS ( subsequent columns ) 

                    ... yet an other case for an explicit type resolution dependency graph 
                */
                preCH current_preCH = preCH_in; 
                var L_primStepTUs   = new List<PrimitveStepTU>();

                foreach ( var primStepNode in frameElementNode.primSteps ) { 
                    var primStepTU = new PrimitveStepTU(current_preCH, primStepNode ) ; 
                    L_primStepTUs.Add( primStepTU ) ; 
                    current_preCH = primStepTU.preCH_out;
                }
                primStepTUs = L_primStepTUs.ToArray();
                preCH_out   = primStepTUs.Last().preCH_out; 
            } 
            
            
        }

        public class JsonLitSpreadVBXTU : VBoxTU_pIN_pOUT
        {
            public Type     JsonLit_deserialization_target_type ; // in case of constant this is to be plugged in from the outside as soon as it is available
            public SObject  JsonLit_deserialized_value ;          // dunno if it is sensible to  have both of these, or to simply get the type from the instance ( ... hmm the value could be null? and still be valid ) 

            public JsonLitSpreadVBXTU( preCH pCH_in ) {
                backing_preCH_in = pCH_in ;
                backing_preCH_out = new deferred_preCH( () => {
                    if ( JsonLit_deserialization_target_type  == null ) throw new Exception( "deserialization type not set before type resolution" ) ;
                    return new TTuple{ isMulti = false , PayT = JsonLit_deserialization_target_type } ;
                } , this );
            }
            public override IEnumerable<OPCode> emit()
            {
                return new OPCode[] { OPGEN.MK_const_spread( CH_in , (SingleCH)CH_out , JsonLit_deserialized_value ) } ;
            }

            public override preCH_deltaScope scope(preCH_deltaScope c) => c ;
            
        }

        public override VBoxTU[]            VBoxTUs  { get {
            if (isConst) return  new [] { ConstantTU } ;
            if (isRef  ) return  new [] { VarRefTU } ;
            return primStepTUs.SelectMany( _ => _.VBoxTUs ).ToArray(); 
            }
        }

        Action T_assert ;

        public virtual void type_fully( Type full_type , SObject maybe_deser ) {
            if( isConst ) {
                ConstantTU.JsonLit_deserialization_target_type = full_type;
                ConstantTU.JsonLit_deserialized_value          = maybe_deser;
            } else { 
                
                D.Assert( maybe_deser == null ) ; 
                // might be cool to D.Assert here - otoh i can't do that without triggering  type collapsing which makes execution order even more convoluted than it already is 
                T_assert = () => { D.Assert( preCH_out.PayT == full_type ) ;};
            }
        }

        public override IEnumerable<OPCode> emit()  {
            if( isConst ) {
                return ConstantTU.emit();
            } else if ( isRef ) {
                return VarRefTU.emit();
            } else {
                T_assert();
                return primStepTUs.SelectMany( primSTU => primSTU.emit() ) ;
            }
        }
        

        public override preCH_deltaScope scope(preCH_deltaScope c)
        {
            if ( isConst ) return c ; 
            if ( isRef   ) return VarRefTU.scope( c ) ;
            foreach( var primTU in primStepTUs ) c = primTU.scope(c ) ; 
            return c;
        }
    }





    public class FrameTU : TranslationUnit {
        
        public readonly preCH preCH_in ; 
        public readonly MG.FrameNode       frameNode;
        public readonly FrameElementTU  [] frameElemTUs ;
        public readonly TranslationUnit [] allTUs   ;

        bool isScoped = false;

        public SingleCH [] data_CHs => frameElemTUs.Select( frmE_TU => (SingleCH)frmE_TU.preCH_out.CH ).ToArray();  // sanity check halfType/fullType is implicit in the preCH_out of the jsonConst VBXTU 

        public FrameTU( preCH pLHS , MG.FrameNode frameNode , bool use_tuple_conversion = true ) {
            this.frameNode = frameNode;
            this.preCH_in = pLHS ;
            var L_frameElemTUs = new List<FrameElementTU> ();
            var L_allTus       = new List<TranslationUnit>();
            preCH curr_preCH = pLHS ;

            

            foreach ( var frameElemNode in frameNode.frameElemNodes ) {
                var frameElemTU  = new FrameElementTU( curr_preCH           , frameElemNode );
                L_frameElemTUs.Add(frameElemTU);
                L_allTus      .Add(frameElemTU);
                var bshiftTU     = new BShiftVBoxTU  ( frameElemTU.preCH_out, pLHS );
                L_allTus.Add(bshiftTU);                                         // semi sinnvoll - der Fan setzt auch bei dem letzten Element noch einen BShift, weil seine Ausgabe-Column eine Kopie seiner LHS-Column ist 
                
                curr_preCH = bshiftTU.preCH_out;
            }
            
            allTUs       = L_allTus      .ToArray();
            frameElemTUs = L_frameElemTUs.ToArray();
        }

        //public IEnumerable<preCH> data_preCHs => frameElemTUs.Select( fTU => fTU.preCH_out ) ; // <- der faellt weg weil keine konstanten beruecksichtigt

        public preCH preCH_out => frameElemTUs.Length > 0 
                                  ? ((VBoxTU)allTUs.Last()).preCH_out     // assumes trailing BShift 
                                  : preCH_in ;

        public override preCH_deltaScope scope(preCH_deltaScope c) {
            isScoped = true; 
            foreach( var frameElemTU in frameElemTUs ) c = frameElemTU.scope(c);
            return c ;
        }

        public ALGO.HalfTyped_withLiterals[] halfType() {
            D.Assert( isScoped );
            var Lresult = new List<ALGO.HalfTyped_withLiterals>();
            foreach ( FrameElementTU frElemTU in frameElemTUs ) {
                ALGO.HalfTyped_withLiterals curr ;
                if ( frElemTU.isConst ) {
                    curr = new ALGO.HalfTyped_withLiterals( MG.TermJSONCapsule( frElemTU.frameElementNode.jsonLit )  ); 
                } else { 
                    curr = new ALGO.HalfTyped_withLiterals( frElemTU.preCH_out.PayT );
                }
                Lresult.Add( curr ) ; 
            }
            return Lresult.ToArray() ;
        }

        bool fully_typed = false ;

        public virtual void type_fully( Type [] signature , SObject [] maybe_deser ) { 
            D.Assert( signature.Length == maybe_deser.Length && signature.Length == frameElemTUs.Length )  ;
            for ( int i = 0 ;i < frameElemTUs.Length ; i ++ ) {
                frameElemTUs[i].type_fully( signature[i] , maybe_deser[i] ) ;
            }
            fully_typed = true ;
        }

        VBoxTU [] __vboxtus;
        public override VBoxTU[] VBoxTUs { get { 
                if ( __vboxtus != null ) return __vboxtus ;
                __vboxtus = allTUs.SelectMany( tu => tu.VBoxTUs).ToArray();
                return __vboxtus;
            }
        }

        public override IEnumerable<OPCode> emit( ) { if( !fully_typed) throw new Exception() ; return allTUs.SelectMany( tu => tu.emit() ) ; }

    }

   
    #endregion 

    public static partial class ALGO {   // actually do something :) 

        public class HalfTyped_withLiterals { 
            // basically : Union( Type , JsonValue ) 

            public bool isLiteral => csharp_type == null ; 
            public DeserCapsule capsule ; 
            public Type                csharp_type;
            public HalfTyped_withLiterals( DeserCapsule capsule             ) { this.capsule     = capsule ; } 
            public HalfTyped_withLiterals( Type                csharp_type  ) { this.csharp_type = csharp_type ; } 

        }
        

        public class SignatureMatchException : Exception { public SignatureMatchException(string msg ) : base( msg ) {}  } 
         
        public static MethodInfo fetchMI( bool isStatic , Type class_type , string name , HalfTyped_withLiterals[] signature_pattern , out SObject[] deserializations ) { 
            BindingFlags BI = BindingFlags.Public | BindingFlags.NonPublic ;

            BI = BI | ( isStatic ? BindingFlags.Static : BindingFlags.Instance ) ; 

            MethodInfo [] mis = class_type.GetMethods( BI ).Where( mi => mi.Name == name ).ToArray() ;
            if( mis.Length == 0 ) throw new SignatureMatchException( " no such name " ) ; 
            return filterMIs( mis , signature_pattern , out deserializations ) ;
        }


        public static MethodInfo filterMIs( MethodInfo[] MIs , HalfTyped_withLiterals[] signaturePattern , out SObject[] deserializations ) {
            
            var len_filtered = MIs.Where( mi => mi.GetParameters().Length == signaturePattern.Length );
            if ( ! len_filtered.Any() ) throw new SignatureMatchException("no match (arg count) ");

            var MI_candidates    = new List<MethodInfo>() ; 
            var deser_candidates = new List<SObject[]> () ;

            foreach( var mi in len_filtered ) {
                ParameterInfo [] PI = mi.GetParameters();
                bool match = true ;

                var current_deserializations = new SObject[signaturePattern.Length] ;
                for( int i = 0 ; i < signaturePattern.Length ; i  ++ ) {
                    
                    var param   = PI[i];
                    var pattern = signaturePattern[i] ;
                    
                    if( ! pattern.isLiteral ) {
                        if ( ! param.ParameterType.IsAssignableFrom( pattern.csharp_type ) ) { match = false ; break ; } 
                        
                    } else {
                        SObject deser_val ;
                        try { 
                            deser_val = pattern.capsule.DESER(  param.ParameterType ) ; 
                        } catch( Exception  ) { // todo Exception discipline in SerializationTypeMapping 
                            match = false ; break ; 
                        }
                        current_deserializations[i] = deser_val; 
                    }
                }  // for ( i .. 
                if( match ) {  MI_candidates.Add( mi ) ; deser_candidates.Add( current_deserializations ) ; }
            }
            if ( MI_candidates.Count() == 0 ) throw new SignatureMatchException( "no match" ) ;
            if ( MI_candidates.Count() >  1 ) throw new SignatureMatchException( "ambigous" ) ;    
            /* 
                todo , probably not correct. iirc : 
                    ` class C { void memF( T1 ) {... } void  memF( T2) { ... } } `   and ` T2 : T1 ` 
                    is resolved when called with 
                    ` C_instance.memF( some_T2 ) `
                    to the T2 overload, whereas this would complain about ambigous 
             */
             deserializations = deser_candidates.First();
             return MI_candidates.First();

         }


    }



    public class FunCallTU : TranslationUnit
    {
        /*
            The BShift Operator currently in use to implement Frame, drops `null`s             
        */

        public FrameTU        frameTU;
        public MG.FunCallNode funCallNode;
        public TupleExtractVBXTU tupleExtract;
        public CallVBXTU         call ; 
        public virtual preCH preCH_out   => call.preCH_out;

        protected FunCallTU(){} // only for the derived RX variant 

        public FunCallTU( preCH pLHS , MG.FunCallNode funCallNode ) {
            this.funCallNode = funCallNode;
            frameTU = new FrameTU( pLHS , funCallNode.frameNode ) ;

            
            tupleExtract    = new TupleExtractVBXTU( frameTU.preCH_out  );
            
            call            = new CallVBXTU( (TypedSingleCH<arg_tuple>) tupleExtract.CH_out , funCallNode  ) ;

        }
        
        public override VBoxTU[] VBoxTUs => frameTU.VBoxTUs.Concat( new VBoxTU[] { tupleExtract , call  } ).ToArray() ;

        // -------------

        public class TupleExtractVBXTU : VBoxTU_pIN_cOUT
        {
            
            SingleCH[] data_CHs ; /* delayed because half typing shenanigans */
            
            public TupleExtractVBXTU( preCH pCH_in   ) {
               
                backing_preCH_in = pCH_in ;

                var out_CH = new TypedSingleCH<arg_tuple>();
                out_CH.pred_SrcVBXTU = this ;
                backing_CH_out = out_CH ;

            }

            public void type_fully( SingleCH [] data_CHs ) {
                this.data_CHs = data_CHs;
            }
            public override IEnumerable<OPCode> emit()
            {
                if ( data_CHs == null ) throw new Exception("emit() before type_fully()" ); 
                var op = new OP_TupleExtract(
                                data_CHs,
                                CH_in,
                                (TypedSingleCH<arg_tuple>)CH_out
                                );
                return new [] { op };

            }

            public override preCH_deltaScope scope(preCH_deltaScope c) => c;
            
        }

        public class CallVBXTU : VBoxTU_cIN_pOUT
        {
            
            public MG.FunCallNode funCallNode;


            private MethodInfo __MI;
            public  MethodInfo MI { get { if( ! fully_typed ) throw new Exception( "trying to access MemberInfo before fully typed" ) ; return __MI ; }}
            public Type[] signature_types => MI.GetParameters().Select( p => p.ParameterType).ToArray();

            public CallVBXTU( TypedSingleCH<arg_tuple> tuple_CH , MG.FunCallNode funCallNode  ) {
                
                this.funCallNode = funCallNode;
                backing_CH_in     = tuple_CH; 
                backing_preCH_out = new deferred_preCH( () => new TTuple{ isMulti = false , PayT = MI.ReturnType } , dataSrc: this ) ;

                
            }
            public override IEnumerable<OPCode> emit()
            {
                var r_type = MI.ReturnType;
                return new [] { OPGEN.MK_Funcall( (TypedSingleCH<arg_tuple>) CH_in , (SingleCH) CH_out , MI ) } ;
            }

            public override preCH_deltaScope scope(preCH_deltaScope c) => c ;

            bool fully_typed = false ;

            public SObject[]  fetchMIfromHalfTypes( ALGO.HalfTyped_withLiterals [] half_types , out Type [] signature_types ) { 
                SObject [] out_obj_dummy ;
                if ( funCallNode.isStatic ) {
                    var classType = SGA.QTN_Exact( funCallNode.typeNameNode.names ) ;
                
                    __MI  = ALGO.fetchMI( true , classType, funCallNode.methodName , half_types , out out_obj_dummy ) ;
                    
                } else {
                    if ( half_types.Length == 0 ) throw new Exception("member function needs at least one argument" ) ;
                    var classType_HT = half_types[0];
                    if ( classType_HT.isLiteral ) throw new Exception("can't use literal as first arg for non static functions" ) ;   // well maybe, if SerializationTypeMapping defines a canonical default type in such cases 
                    
                    __MI = ALGO.fetchMI( false, classType_HT.csharp_type , funCallNode.methodName , half_types , out out_obj_dummy );
                }
                fully_typed = true ;
                signature_types = __MI.GetParameters().Select( x => x.ParameterType ).ToArray();
                return out_obj_dummy;
            }
            
        }
        // -------------

        public virtual void type_fully() {
            ALGO.HalfTyped_withLiterals [] htypes              =  frameTU.halfType();
            Type                        [] signature ;
            SObject                     [] maybe_deserialized  =  call.fetchMIfromHalfTypes( htypes , out signature ) ;

            frameTU.type_fully( signature , maybe_deserialized ) ;
            tupleExtract.type_fully( frameTU.data_CHs ) ; 

        }

        public override IEnumerable<OPCode> emit() {
            type_fully();

            var frameTU_OPS = frameTU.emit();
            return frameTU_OPS
                    .Concat( tupleExtract.emit() ) 
                    .Concat( call.emit() )
                    .ToArray();
        }
        

        public override preCH_deltaScope scope(preCH_deltaScope c) => frameTU.scope(c) ;

    }

    public static partial class ALGO {   // actually do something :) 
        public static Type element_type_from_IEnumerable ( Type maybe_IEnumerable ) {

            // https://stackoverflow.com/a/13589104
            // collection types implement both  `IEnumerable` and `IEnumerable<>` , which are distinct types - filter only for the generic one 
            Type [] candidate_interfaces =  maybe_IEnumerable.GetInterfaces()
                .Where( IF => IF.IsGenericType && IF.GetGenericTypeDefinition() == typeof(IEnumerable<>) )
                .ToArray();
            
            if ( candidate_interfaces.Length != 1 ) throw new Exception( " no generic IEnumrable<> interface found " ) ; 

            return candidate_interfaces[0].GetGenericArguments()[0] ;
            
        }
    }

    public class lift_up_VBXTU : VBoxTU_pIN_pOUT
    {
       

        public lift_up_VBXTU( preCH preCH_in ) {
            backing_preCH_in = preCH_in ;
            backing_preCH_out = new deferred_preCH( () => 
                                                        new TTuple { isMulti = false , PayT = ALGO.element_type_from_IEnumerable( CH_in.ttuple.PayT ) } 
                                                    , dataSrc: this) ;
        }

        public override IEnumerable<OPCode> emit()
        {
            return new [] { OPGEN.MK_lift_up( CH_in , CH_out ) } ; 
        }

        public override preCH_deltaScope scope(preCH_deltaScope c) => c ;
        
    }




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
                // this circumvents the entire comparison between types topic by casting the RHS literal to the exact same type 
                SObject ser_payload = eq_filter_node.capsule.DESER( ser_target_type ) ;
                var const_op = OPGEN.MK_const ( eq_RHS_CH , ser_payload ) ;
                L.Add(const_op );

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

    /*
         variable ref in primitve step 
         fuer #x kann ich einfach den BShift recyclen 
         fuer $x braucht es wahrscheinlich nen extra opcode ( im assignment operator ist das automatische wiederholen der src sequenz hart im code ) 

    */


    public class VarRefVBXTU : VBoxTU_pIN_pOUT
    {
        public preCH      referenced_pCH ;  // Column pointed to by varname     
        public MG.RefNode refNode ;
        public bool scoped = false ; 

        public VarRefVBXTU( preCH preCH_in , MG.RefNode refNode ) {
            this.refNode = refNode ;
            this.backing_preCH_in  = preCH_in ; 

            this.backing_preCH_out = new deferred_preCH( 
                () => {
                    D.Assert(scoped);
                    return new TTuple { PayT = referenced_pCH.PayT , isMulti = false } ;
                } , 
                dataSrc: this 
                );
            
        }

        public override preCH_deltaScope scope(preCH_deltaScope c )
        {
            
            c = c.addRef( refNode.name , out referenced_pCH ) ;
            scoped = true ; 
            return c; 
        }

        public override IEnumerable<OPCode> emit()
        {
            if        ( refNode is MG.SharpRefNode  ) {
                return new [] { OPGEN.MK_BarrierShift( referenced_pCH.CH , CH_in , CH_out ) } ; 
                
            } else if ( refNode is MG.DollarRefNode ) { 
                return new [] { OPGEN.MK_dollar_spread( CH_in , referenced_pCH.CH , CH_out ) };

            }
            throw new NotImplementedException();
        }
    }



    public class PrimitveStepTU : TranslationUnit
    {
        public override VBoxTU[] VBoxTUs => primaryTU.VBoxTUs.Concat( assigns.SelectMany ( asg => asg.VBoxTUs)).ToArray();

        protected TranslationUnit      primaryTU         = null; 
        protected preCH                primary_preCH_out = null ;
        protected List<Assign_VBXTU>   assigns           = new List<Assign_VBXTU>();
        protected MG.PrimitiveStepNode primtv_step_node  = null;
        
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
            } else if ( primaryNN is MG.TermNode && MG.TermEnum( primaryNN ) == MainGrammar.PTokE.OP_lift_up ) {
                var lift_up_TU = new lift_up_VBXTU( preCH_in ) ;
                primaryTU = lift_up_TU ;
                primary_preCH_out = lift_up_TU.preCH_out ; 
            
            }
            
            else throw new Exception();  // grammar changed or parser bug 

            preCH current_preCH_out = primary_preCH_out;
            foreach( var asgn_node in primtv_step_node.assigns) {
                var asgn_TU = new Assign_VBXTU( current_preCH_out , asgn_node );
                assigns.Add( asgn_TU);
                current_preCH_out = asgn_TU.preCH_out;
            }
            preCH_out = current_preCH_out;
        }
        protected PrimitveStepTU () {}

        /*
            finding the lastmost `Decl` is not that obvious 
            PrimitiveStepNode has an immediate `DeclStarChild`
            and optionally any number of assigns, each of which might contain an `DeclStar` 
            on top of that a `DeclStarNode` can consume epsilon 

            variable declarations produce neither OpCodes nor Columns, they are only used to record a Column to that name during scoping 
            -> currently the decls are not extracted from their parse tree nodes into other fields 

            for constructs like this : 
            [ ... , ..someField -> X <- @3     , ... ] 
            to make the dictionary interpretation consistent with the tuple interpretation `preCH_out_decl()` returns null because it always refers
            to the last column constructed for the primitve step 
        */

        public string preCH_out_decl() { 
             /*
                PrimitiveStep = Prod<PrimitiveStepNode> ( SEQ ( 
                OR (    SG_Edge , 
                        MemA    , 
                        Filter  , 
                        Fan 
                        // Todo : VarRef 
                        ) , 
                DeclStar ,
                STAR ( AssignVT )     // AssignVT includes DeclStar
                ) );
            */
            
            string res = null ; 
            // simply iterate over all steps that construct a columnn - if the last column doesn't have a decl associated with it it will simply be overwritten again with null 

            var immediate_decls = primtv_step_node.primary_decl_node.decls ;
            res = immediate_decls.Length > 0 ? immediate_decls.Last() : null ; 
            foreach( Assign_VBXTU assgn in assigns ) {
                var assg_decl = assgn.AsgNode.decls ;
                res = assg_decl.Length > 0 ? assg_decl.Last() : null ;
            }
            return res ; 
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
        public override VBoxTU[]    VBoxTUs =>  rootIsSG 
            ? root_SG_edge.VBoxTUs.Concat( subsequent_Steps.SelectMany( ps => ps.VBoxTUs ) ).ToArray()
            : root_FunCall.VBoxTUs.Concat( subsequent_Steps.SelectMany( ps => ps.VBoxTUs ) ).ToArray();

        public bool rootIsSG ;

        public Root_SG_EdgeTU       root_SG_edge;

        public FunCallTU            root_FunCall; 
        public OP_SuiGen<int>       dummy_Suigen;  // dummy operator because the funcall needs a left hand side, even for static ones it influences how often it is called 

        public TranslationUnit []    subsequent_Steps;

        public ProvStartTU ( MG.ProvStartNode start_node ) {

            preCH primStepsLHS = null ; 
            if ( start_node.rootIsSG ) {
                root_SG_edge = new Root_SG_EdgeTU( start_node.startSG );
                rootIsSG = true ;
                primStepsLHS = root_SG_edge.preCH_out;
            } else { 
                var dummy_root_CH = new TypedSingleCH<int>();
                dummy_Suigen = new OP_SuiGen<int>( dummy_root_CH , ( _ ) => new [] { 1} ) ; 
                root_FunCall = new FunCallTU     ( new adapter_preCH( dummy_root_CH ) , start_node.startFuncall ) ;
                rootIsSG = false ; 
                primStepsLHS = root_FunCall.preCH_out;
            }
            var L = new List<TranslationUnit>();
            preCH current_preCH = primStepsLHS ; 

            foreach ( var subseq_step in start_node.subsequentSteps ) {
                TranslationUnit TU ; 
                if        ( subseq_step is MG.PrimitiveStepNode ) {

                    TU = new PrimitveStepTU( current_preCH, (MG.PrimitiveStepNode)subseq_step  );

                } else if ( subseq_step is MG.FunCallNode ) { 

                    TU = new FunCallTU     ( current_preCH , (MG.FunCallNode ) subseq_step ) ; 

                } else throw new NotImplementedException();
                L.Add( TU  );
                current_preCH = (TU is PrimitveStepTU) ? (TU as PrimitveStepTU) .preCH_out : (TU as FunCallTU).preCH_out ;
            }
            subsequent_Steps = L.ToArray();
        }

        protected ProvStartTU(){}

        public override IEnumerable<OPCode> emit() {
            if ( rootIsSG ) { 
                return root_SG_edge.emit().Concat( subsequent_Steps.SelectMany( ps => ps.emit() ) );
            } else { 
                return new OPCode [] { dummy_Suigen } 
                    .Concat( root_FunCall.emit() )
                    .Concat( subsequent_Steps.SelectMany( ps => ps.emit() ) );
            }
        }


        public override preCH_deltaScope scope(preCH_deltaScope c)
        {
            if ( ! rootIsSG ) c = root_FunCall.scope(c ) ;
            foreach ( var ps in subsequent_Steps ) c = ps.scope(c);
            return c;
        }
    }


}