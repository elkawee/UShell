
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;

using UnityEngine;

using SObject = System.Object;

using D = System.Diagnostics.Debug;




namespace TranslateAndEval { 



    /*
        All OPCodes treat the columns they write to, as if they were empty 
        how this is satisfied is up to the MemMapper 
        ... primarily because this opens the door to all kinds of optimizations 
    */


    public class OP_Assign_Dollar<PayT> : OPCode {
        public TypedCH<PayT> CH_in  , CH_out  , CH_aux ;
        public Column<PayT>  Col_in , Col_out , Col_aux ; 
        Action<VBox,SObject> PrimAssign;                   // todo SObject -> PayT
        public OP_Assign_Dollar( TypedCH<PayT> LHS , TypedCH<PayT> RHS , TypedCH<PayT> Data , Action<VBox,SObject> PrimAssign ) {
            CH_in = LHS ; CH_out = RHS ; CH_aux = Data ;
            this.PrimAssign = PrimAssign ;
        }
        public override void fill ( MemMapper M ) {
            Col_in  = M.get ( CH_in  ) ;
            Col_out = M.get ( CH_out ) ;
            Col_aux = M.get ( CH_aux ) ;
        }
        public override void eval ( Context Cxt ) {
             
            var data_rator = Col_aux.boxesT.NonEmptyCyclic().GetEnumerator();
            foreach ( VBox lhs_box in Col_in.boxesT ) {
                data_rator.MoveNext();
                var Datum = data_rator.Current;
                PrimAssign(lhs_box,Datum.value() );
                Col_out.AddVal( Datum.valueT() , pred: lhs_box );   
            }

        }
    }
    // the primAssign variants should probably be defined here and selected via enum or some such 

    public static partial class OPGEN {
        public static OPCode MK_OP_Assign_Dollar( TypedCH CH_Left , TypedCH CH_Res , TypedCH CH_aux , Action<VBox,SObject> PrimAssign ) {
            // can one dynamically check the actual type of the backing function in a delegate ? 
            D.Assert (
                ( CH_Left.ttuple.PayT == 
                  CH_Res .ttuple.PayT        ) && 
                ( CH_Res .ttuple.PayT  == 
                  CH_aux .ttuple.PayT
                ));                                       // todo turn this into Exception - MakeGenericType checks this too, but the err msg is cryptic and generic 
            var completeType = typeof( OP_Assign_Dollar<> ).MakeGenericType( new [] { CH_Left.ttuple.PayT  } );
            return (OPCode)Activator.CreateInstance( completeType , new object [] { CH_Left , CH_Res , CH_aux , PrimAssign } );
        }
    }

    #region deprecated Mema_ref_ref    
    public class OP_MemA_RefRef<Tobj,Tfield> : OPCode{
        public TypedCH<Tobj>         CH_in ;
        public TypedCH<Tfield>       CH_out ;
        public Column<Tobj>          Col_in ;
        public Column<Tfield>        Col_out;         

        public FieldInfo fi ;
        public OP_MemA_RefRef( TypedCH<Tobj> CH_in , TypedCH<Tfield> CH_out , FieldInfo fi ) {
            this.CH_in = CH_in ; this.CH_out = CH_out ; this.fi = fi;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get(CH_in ) ;
            Col_out = MM.get(CH_out);
        }
        public override void eval (Context _ ) {
            foreach ( var box_in in Col_in.boxesT ) {
                Col_out.AddVal( (Tfield)fi.GetValue(box_in.value() ) , box_in );
            }
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_MemA_RefRef( TypedCH CH_L , TypedCH CH_R , FieldInfo FI ) {
            var completeType = typeof( OP_MemA_RefRef<,> ).MakeGenericType( new [] { CH_L.ttuple.PayT , CH_R.ttuple.PayT } );
            return (OPCode)Activator.CreateInstance( completeType , new object [] { CH_L , CH_R , FI } );
        }
    }
    #endregion
    
    public class OP_MemA_FieldSingle<Tobj,Tfield> : OPCode {
        public TypedCH<Tobj>         CH_in ;
        public Column<Tobj>          Col_in ;

        public TypedSingleCH<Tfield>       CH_out ;
        public ColumnSingle<Tfield>        Col_out;         

        public FieldInfo fi ;
        public OP_MemA_FieldSingle( TypedCH<Tobj> CH_in , TypedSingleCH<Tfield> CH_out , FieldInfo fi ) {
            this.CH_in = CH_in ; this.CH_out = CH_out ; this.fi = fi;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get(CH_in ) ;
            Col_out = MM.get(CH_out);
        }
        public override void eval (Context _ ) {
            foreach ( var box_in in Col_in.boxesT ) {
                Col_out.AddVal( (Tfield)fi.GetValue(box_in.value() ) , box_in );
            }
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_OP_MemA_FieldSingle( TypedCH CH_L , TypedCH CH_R , FieldInfo FI ) {
            var completeType = typeof( OP_MemA_FieldSingle<,> ).MakeGenericType( new [] { CH_L.ttuple.PayT , CH_R.ttuple.PayT } );
            return (OPCode)Activator.CreateInstance( completeType , new object [] { CH_L , CH_R , FI } );
        }
    }

    public class OP_MemA_PropSingle<Tobj,Tprop> : OPCode  {
        public TypedCH<Tobj>         CH_in ;
        public Column<Tobj>          Col_in ;

        public TypedSingleCH<Tprop>       CH_out ;
        public ColumnSingle<Tprop>        Col_out;         

        public PropertyInfo pi ;
        public OP_MemA_PropSingle( TypedCH<Tobj> CH_in , TypedSingleCH<Tprop> CH_out , PropertyInfo pi ) {
            this.CH_in = CH_in ; this.CH_out = CH_out ; this.pi = pi;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get(CH_in ) ;
            Col_out = MM.get(CH_out);
        }
        public override void eval (Context _ ) {
            foreach ( var box_in in Col_in.boxesT ) {
                Col_out.AddVal( (Tprop)pi.GetValue(box_in.value() , new object [0] ) , box_in );
            }
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_OP_MemA_PropSingle( TypedCH CH_L , TypedCH CH_R , PropertyInfo FI ) {
            var completeType = typeof( OP_MemA_PropSingle<,> ).MakeGenericType( new [] { CH_L.ttuple.PayT , CH_R.ttuple.PayT } );
            return (OPCode)Activator.CreateInstance( completeType , new object [] { CH_L , CH_R , FI } );
        }
    }


    // --------------------------------------------------------------------------

    public class OP_MemA_RefProp<Tobj,Tprop> : OPCode{
        public TypedCH<Tobj>         CH_in ;
        public TypedCH<Tprop>       CH_out ;
        public Column<Tobj>          Col_in ;
        public Column<Tprop>        Col_out;         

        public PropertyInfo pi ;
        public OP_MemA_RefProp( TypedCH<Tobj> CH_in , TypedCH<Tprop> CH_out , PropertyInfo pi ) {
            this.CH_in = CH_in ; this.CH_out = CH_out ; this.pi = pi;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get(CH_in ) ;
            Col_out = MM.get(CH_out);
        }
        public override void eval (Context _ ) {
            foreach ( var box_in in Col_in.boxesT ) {
                
                Col_out.AddVal( (Tprop)pi.GetValue(box_in.value() , new object[0] ) , box_in );
            }
        }
    }
    public static partial class OPGEN {
        public static OPCode MK_MemA_RefProp( TypedCH CH_L , TypedCH CH_R , PropertyInfo FI ) {
            var completeType = typeof( OP_MemA_RefProp<,> ).MakeGenericType( new [] { CH_L.ttuple.PayT , CH_R.ttuple.PayT } );
            return (OPCode)Activator.CreateInstance( completeType , new object [] { CH_L , CH_R , FI } );
        }
    }

    #region const 

    // placeholder - simplest solution for now - wrap a value into an operator 
    public class OP_const<DeserializedPay> : OPCode {
        public DeserializedPay payload;
        public TypedSingleCH<DeserializedPay> CH_out ;
        public ColumnSingle<DeserializedPay>  Col_out;
        public OP_const ( TypedSingleCH<DeserializedPay> CH_out , DeserializedPay payload ) {
            this.CH_out = CH_out ;
            this.payload = payload ;
        }
        public override void fill ( MemMapper MM ) {
            Col_out = MM.get( CH_out ) ;
        }
        public override void eval ( Context _ ) {
            Col_out.AddVal(payload );
        }
    }
    public static partial class OPGEN {
        //                              \/ could be restricted to single 
        public static OPCode MK_const ( TypedCH CH_out , SObject payload ) {
            var compleType = typeof ( OP_const<> ).MakeGenericType( new [] { CH_out.ttuple.PayT }  ) ;
            return (OPCode)Activator.CreateInstance( compleType , new SObject [] { CH_out , payload } );
        }
    }

    public class OP_Const_Spread<DeserializedPay> : OPCode
    {
        public TypedCH CH_in; 
        public Column  Col_in;

        public TypedSingleCH<DeserializedPay> CH_out ;
        public ColumnSingle<DeserializedPay>  Col_out;

        DeserializedPay pay;
        
        public OP_Const_Spread( TypedCH CH_in , TypedSingleCH<DeserializedPay> CH_out , DeserializedPay pay ) {
            this.CH_in  = CH_in ; 
            this.CH_out = CH_out;
            this.pay = pay ;
        }

        public override void fill(MemMapper MM)
        {
            Col_in  = MM.getGen( CH_in  );
            Col_out = MM.get   ( CH_out );
        }

        public override void eval(Context c)
        {
            foreach( VBox vb in Col_in.boxes ) Col_out.AddVal( pay , vb ) ;
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_const_spread( TypedCH CH_in , SingleCH CH_out , SObject payload ) {
            Type out_CH_payType = CH_out.ttuple.PayT;
            if ( !out_CH_payType.IsAssignableFrom( payload.GetType() ) ) throw new Exception( "deserialized payload is not assignable" ) ;
            Type completeType = typeof(OP_Const_Spread<>).MakeGenericType( new [] { out_CH_payType } ) ; 
            return (OPCode)Activator.CreateInstance( completeType , new SObject[] { CH_in , CH_out , payload } );
        }
    }

    /* 
        pretty much the same as const spread, but instead of repeating a constant `Count( LHS_Column.boxes )` 
        it cycles through the boxes of the refrenced columns until `Count( LHS_Column.boxes )` are reached, repeating the sequence, if neccesary 

     */

    public class OP_Dollar_Spread<T_variable> : OPCode
    {
        public TypedCH                   LHS_CH ;
        public TypedCH<T_variable>       referenced_CH ;
        public TypedSingleCH<T_variable> CH_out ;

        public Column                    LHS_Column;
        public Column<T_variable>        referenced_Column;
        public ColumnSingle<T_variable>  out_Column;

        public OP_Dollar_Spread( TypedCH LHS_CH , TypedCH<T_variable> referenced_CH , TypedSingleCH<T_variable> CH_out ) {
            this.LHS_CH        = LHS_CH ; 
            this.referenced_CH = referenced_CH; 
            this.CH_out        = CH_out; 
        }
        
        public override void eval(Context c)
        {
            var values = referenced_Column.valuesT.NonEmptyCyclic();
            var zipped = AUX.Zip(LHS_Column.boxes , values , ( b , v ) => new { box = b , val = v } ) ;

            foreach ( var anon_tpl in zipped ) {
                out_Column.AddVal( anon_tpl.val , anon_tpl.box ) ;
            }
        }

        public override void fill(MemMapper MM)
        {
            LHS_Column        = MM.getGen( LHS_CH );
            referenced_Column = MM.get   ( referenced_CH );
            out_Column        = MM.get   ( CH_out );
        }
    }

    public static partial class OPGEN { 
        public static OPCode MK_dollar_spread( TypedCH lhs_CH , TypedCH referenced_CH , TypedCH outCH ) {
            Type T_variable   = referenced_CH.ttuple.PayT ;
            Type completeType = typeof( OP_Dollar_Spread<> ).MakeGenericType( new [] { T_variable } ) ;
            return (OPCode)Activator.CreateInstance( completeType , new [] { lhs_CH , referenced_CH , outCH } ) ;
        }
    }



    #endregion 

    public class OP_BarrierShift<PayOrig, PayLHS>:OPCode {
        public TypedCH<PayOrig>        origCH ;         // only for calc-ing backsteps 
        public TypedSingleCH<PayLHS>   lhsCH ;
        public TypedSingleCH<PayOrig>  CH_out ;
        public int                     backsteps ;

        
        public ColumnSingle<PayLHS>    lhsCol;
        public ColumnSingle<PayOrig>   out_Col;

        public OP_BarrierShift(
            TypedCH<PayOrig> origCH , 
            TypedSingleCH<PayLHS> lhsCH , 
            TypedSingleCH<PayOrig> CH_out ) 
        {
            this.origCH = origCH; this.lhsCH = lhsCH; this.CH_out = CH_out; 
            // starting from lhs count backwards the VBox-edges 
            backsteps = 0 ;
            TypedCH currentCH = lhsCH;

            // TODO , throw when not ColumnSingle 

            while ( currentCH != origCH ) { currentCH = currentCH.pred_SrcVBXTU.CH_in; backsteps ++ ; } // if dataSrc is null, that's a bug in translate 
        }
        public override void fill(MemMapper MM) {
            //origCol = MM.get(origCH);
            lhsCol  = MM.get(lhsCH);
            out_Col = MM.get(CH_out);
        }

        public override void eval(Context c) {
            foreach ( var box_lhs in lhsCol.boxesT ) {
                VBox currBox = box_lhs ;
                for ( int i = 0 ; i < backsteps ; i ++ ) {     // walk back from lhs_boxes to add only those from origColumn, that are reachable from lhs 
                    currBox = (currBox as VBoxSingle).pred(); 
                    // this operation is only valid if the entire path had no multi columns - if this cast fails the error is in translate 
                    // todo : check this in constructor 
                }
                var origBox = ( VBox<PayOrig> ) currBox;
                out_Col.AddVal( origBox.valueT() , box_lhs );
            }   
        }
    }
    public static partial class OPGEN {
        public static OPCode MK_BarrierShift ( TypedCH origCH , TypedCH lhsCH , TypedCH outCH ) {
            if ( origCH.ttuple.PayT != outCH.ttuple.PayT ) throw new Exception();  // kinda superfuous Activator should throw the same 
            var completeType = typeof ( OP_BarrierShift<,> ).MakeGenericType( new [] { origCH.ttuple.PayT , lhsCH.ttuple.PayT } );
            return (OPCode)Activator.CreateInstance( completeType , new SObject [] { origCH , lhsCH , outCH } ); 
        }
    }


    public class arg_tuple {
        public SObject [] args;
        public arg_tuple( SObject [] args ) { this.args = args; } 
    }

    public class OP_TupleExtract : OPCode
    {

        SingleCH []              data_CHs ;
        TypedCH                  lhs_CH;
        TypedSingleCH<arg_tuple> out_CH;

        int      [] steps2CH ; 
        /* 
            Since Frames and Fans are expected to "spread" nodes that would natrually have multi-VBoxes (ref types for example) 
            into VBoxSingles TypedSingleCH can be assumed for all data gathering Columns
        */
        public OP_TupleExtract ( SingleCH []                  data_CHs 
                               , TypedCH                      lhs_CH      // typing problem in case of 0 FrameElements [1]
                               , TypedSingleCH< arg_tuple >   out_CH       
            ) {

            this.data_CHs = data_CHs ;
            this.lhs_CH   = lhs_CH   ; 
            this.out_CH   = out_CH   ;
            steps2CH = new int[data_CHs.Length] ;

            TypedCH current_CH = lhs_CH;

            for( int i = data_CHs.Length -1 ; i >= 0 ; i -- ) {
                int step_sz = 0 ;
                while( current_CH != data_CHs[i] ) {
                    current_CH = current_CH.pred_SrcVBXTU.CH_in;
                    step_sz ++ ;
                }
                steps2CH[i] = step_sz;
            }
        }

        Column                    lhs_Column;
        ColumnSingle< arg_tuple > out_Column;

        public override void fill(MemMapper MM)
        {
            lhs_Column = MM.getGen( lhs_CH) ;
            out_Column = MM.get   ( out_CH ) ;
        }

        public override void eval(Context c)
        {
            if ( data_CHs.Length == 0 ) {
                foreach(var lhs_box in  lhs_Column.boxes ) out_Column.AddVal( new arg_tuple( new SObject[0] ) , lhs_box );
                return;
            }
            /*
                with non-zero FrameElems lhs_CH is a Column generated as part of the Frame and can be assumed single 
                likewise the leftmost DataCH ( immediate predecessor of the leftmost BShift ) 
                is also neccesarily generated by Frame 
                [ Exp , ... ] ( until there is a way to translate the first Exp without generating a Column, which there currently isn't and likely never will be ) 

                ... from a type safety point of view, it would make more sense to simply have two OPCodes for the zero and N-arg cases though ... 
            */
            var lhs_ColumnSingle = (ColumnSingle) lhs_Column;
            var args = new SObject[data_CHs.Length];
            foreach( VBoxSingle lhs_box in  lhs_ColumnSingle.boxesSingle ) { 
                
                VBoxSingle currentBox = lhs_box ;
                for( int i_per_arg = data_CHs.Length -1 ; i_per_arg >= 0 ; i_per_arg -- ) {
                    
                    for( int i_step = 0 ; i_step < steps2CH[i_per_arg] ; i_step ++ ){
                        // .... i'm not going to introduce N-types expressing "this is `single` and N of its predecessors must be too" :) 
                        currentBox = (VBoxSingle)currentBox.pred() ;
                    }
                    args[i_per_arg] = currentBox.value();
                }
                out_Column.AddVal(new arg_tuple( args ),lhs_box );
            }
        }

        /*
            [1] since i do not want to inject more design dependencies on the Column-layout for FrameTU than neccessary,
            the zero element Frame can not be assumed to have a ColumnSingle as LHS (allows zero OPCode implementation for `[]` ) 
        */

    }

    

    public class OP_Funcall<OutT> : OPCode
    {
        TypedSingleCH<arg_tuple> args_CH ;
        TypedSingleCH<OutT>      out_CH  ;
        MethodInfo               MI;

        public OP_Funcall( TypedSingleCH<arg_tuple>  args_CH , TypedSingleCH<OutT> out_CH , MethodInfo MI ) {
            this.args_CH = args_CH ; 
            this.out_CH  = out_CH  ;
            this.MI      = MI; 
        }

        ColumnSingle<arg_tuple> args_Column; 
        ColumnSingle<OutT>      out_Column ; 

        public override void fill(MemMapper MM)
        {
            args_Column = MM.get( args_CH ) ; 
            out_Column  = MM.get( out_CH  ) ; 
        }

        public override void eval(Context c)
        {
            if( MI.IsStatic ) {                                  // todo : lotsa exceptions n sheet 
                foreach( var lhs_box in args_Column.boxesT ) {
                    var res = MI.Invoke( null , lhs_box.valueT().args ) ; 
                    out_Column.AddVal( (OutT)res , lhs_box ) ;
                }
            } else { 
                foreach( var lhs_box in args_Column.boxesT ) {
                    SObject[] args = lhs_box.valueT().args; 
                    var res = MI.Invoke( args[0] , args.Skip(1).ToArray()  ) ; 
                    out_Column.AddVal( (OutT)res , lhs_box ) ;
                }
            }
           
        }

        
    }

    public static partial class OPGEN {
        public static OPCode MK_Funcall( TypedSingleCH<arg_tuple> CH_tuple , SingleCH CH_out , MethodInfo MI ) {
            var completeType = typeof ( OP_Funcall<> ).MakeGenericType( new [] { MI.ReturnType } );
            return (OPCode)Activator.CreateInstance( completeType , new SObject [] { CH_tuple , CH_out , MI } ); 
        }
    }


    public class OP_lift_up<ElementT, CollectionT> : OPCode where CollectionT : IEnumerable<ElementT>
    {
        public Column<CollectionT>      Col_in ;
        public ColumnSingle<ElementT>   Col_out ;

        public TypedCH<CollectionT>     CH_in  ;
        public TypedSingleCH<ElementT>  CH_out ;

        public OP_lift_up( TypedCH<CollectionT> CH_in , TypedSingleCH<ElementT> CH_out ) {
            this.CH_in  = CH_in ;
            this.CH_out = CH_out;
        }
        

        public override void fill(MemMapper MM)
        {
            Col_in  = MM.get( CH_in );
            Col_out = MM.get( CH_out ) ; 
        }

        public override void eval(Context c)
        {
            foreach( VBox<CollectionT> coll_Box in Col_in.boxesT ) { 
                foreach( ElementT elem in coll_Box.valueT() ) {
                    Col_out.AddVal( elem , coll_Box ) ;
                }
            }
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_lift_up( TypedCH CH_in , TypedCH CH_out  ) {
            Type collection_type = CH_in.ttuple.PayT ;
            Type elem_type  = ALGO.element_type_from_IEnumerable( collection_type ) ;

            var completeType = typeof ( OP_lift_up<,> ).MakeGenericType( new [] { elem_type , collection_type } );
            return (OPCode)Activator.CreateInstance( completeType , new SObject [] { CH_in , CH_out } ); 
        }
    }


    public static partial class OPCode_AUX {
        /*

        // to be reactivated later, when there is syntax available to switch between these variants 

        public static IEnumerable<T>          NonInactiveObjectsOf<T>(Context _) where T:UnityEngine.Object 
            => Resources.FindObjectsOfTypeAll<T>              ();

        public static IEnumerable<GameObject> NonInactiveRootsGO     (Context _) 
            => Resources.FindObjectsOfTypeAll<GameObject>().Where( go => go.transform.parent == null );
        */

        public static IEnumerable<T>          NonInactiveObjectsOf<T>(Context _) where T:UnityEngine.Object 
            => UnityEngine.Object.FindObjectsOfType<T>              ();

        public static IEnumerable<GameObject> NonInactiveRootsGO     (Context _) 
            => UnityEngine.Object.FindObjectsOfType<GameObject>().Where( go => go.transform.parent == null );
    }


    #region suigen 

    public class OP_SuiGen<T>:OPCode {
        /* 
            水源 
            - because "source" and "data source"  is already used in too many other contexts 
            intended to implement the PhantomRoot simulation 
            e.g.  generator func does AllGameObjects().Where( go => go.parent == null ) 

        */ 

        public TypedSingleCH<T>         CH_out;
        public ColumnSingle<T>          Col_out;
        public Func<Context,IEnumerable<T>>     generator_func;

        public OP_SuiGen (  TypedSingleCH<T> CH_out , Func<Context ,IEnumerable<T>> generator ) {
            this.CH_out = CH_out;
            this.generator_func = generator;
        }

        public override void fill(MemMapper MM) {
            Col_out = MM.get( CH_out );
        }
        public override void eval(Context ctx ) {
            foreach ( var obj in generator_func(ctx) ) Col_out.AddVal( obj , null );
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_SuiGen ( TypedCH CH_out , SObject generator ) {
            var closedType = typeof ( OP_SuiGen<>).MakeGenericType( new [] { CH_out.ttuple.PayT } ) ;
            return (OPCode)Activator.CreateInstance( closedType , new SObject[] { CH_out , generator } ) ;
        }
    }
    #endregion 

    #region SG immediate boilerplatism 
    public class OP_SG_immediate_GO : OPCode{
        public TypedCH< GameObject >  CH_in;
        public Column < GameObject >  Col_in;

        public TypedSingleCH < GameObject >  CH_out;
        public ColumnSingle  < GameObject >  Col_out;

        public OP_SG_immediate_GO ( TypedCH< GameObject > CH_in , TypedSingleCH<GameObject> CH_out ) {
            this.CH_in = CH_in ; this.CH_out = CH_out; 
        }

        public override void eval(Context c)
        {
            foreach ( var box_in in Col_in.boxesT ) {
                foreach ( Transform ch_go_transform in box_in.valueT().transform ) {
                    Col_out.AddVal( ch_go_transform.gameObject );
                }
            }
        }

        public override void fill(MemMapper MM) { Col_in = MM.get(CH_in ); Col_out = MM.get(CH_out );}
    }

    public class OP_SG_immediate_Comp<in_CompType> : OPCode where in_CompType:Component{
        public TypedCH< in_CompType >  CH_in;
        public Column < in_CompType >  Col_in;

        public TypedSingleCH < GameObject >  CH_out;
        public ColumnSingle  < GameObject >  Col_out;

        public OP_SG_immediate_Comp ( TypedCH< in_CompType > CH_in , TypedSingleCH<GameObject> CH_out ) {
            this.CH_in = CH_in ; this.CH_out = CH_out; 
        }

        public override void eval(Context c)
        {
            foreach ( var box_in in Col_in.boxesT ) {
                foreach ( Transform ch_go_transform in box_in.valueT().transform ) {
                    Col_out.AddVal( ch_go_transform.gameObject );
                }
            }
        }

        public override void fill(MemMapper MM) { Col_in = MM.get(CH_in ); Col_out = MM.get(CH_out );}
    }

    public static partial class OPGEN {
        public static OPCode MK_SG_immediate ( TypedCH CH_in , TypedCH CH_out ) {
            var in_payload_type = CH_in.GetType().GetGenericArguments()[0];
            Type closedType ; 
            if ( in_payload_type == typeof(GameObject ) ) {
                closedType = typeof ( OP_SG_immediate_GO ) ;
            } else { 
                closedType = typeof ( OP_SG_immediate_Comp<>).MakeGenericType( new [] { in_payload_type  } ) ;
            }
            return (OPCode)Activator.CreateInstance( closedType , new SObject[] { CH_in , CH_out } ) ;
        }
    }
    #endregion

    #region SG desc all boilerplatism 
    /*
        from ILSpy : 

        UnityEngine.GameObject.Equals()  // does not override UE.Object.Equals() 
        UnityEngine.Object.Equals()      //  calls this :
        
        
        private static bool CompareBaseObjects(Object lhs, Object rhs)
        {
	        bool flag = (object)lhs == null;
	        bool flag2 = (object)rhs == null;
	        if (flag2 && flag)
	        {
		        return true;
	        }
	        if (flag2)
	        {
		        return !IsNativeObjectAlive(lhs);
	        }
	        if (flag)
	        {
		        return !IsNativeObjectAlive(rhs);
	        }
	        return object.ReferenceEquals(lhs, rhs);
        }

        will leads me to assume, for now, that there is a bijection between GameObject instances and the c-side objects they represent
        
        and that one can safely use these guys as dictionary keys 
        ( GetHashCode() too points to the standart library System.Object.GetHashCode()  ) 

        ... for now :) 
    */

    public static partial class OPCode_AUX { 
        public static IEnumerable<GameObject> DescAll ( GameObject ObParent , bool inactive=false ) 
            // abuse the fact that every GameObject has to have a Transform Component -- filter self 
            => ObParent.GetComponentsInChildren<Transform>(inactive).Where(tr=>tr.gameObject != ObParent ).Select( tr=>tr.gameObject);
        
        public static IEnumerable<GameObject> DescAll ( Component ObParent , bool inactive=false ) 
            // abuse the fact that every GameObject has to have a Transform Component -- filter self 
            => ObParent.GetComponentsInChildren<Transform>(inactive).Where(tr=>tr.gameObject != ObParent ).Select( tr=>tr.gameObject);
            /*
                this is the variant with implied uniquness filter built in - dunno yet if something like >>+  ( spread ) is ever going to be needed 
                -> all 

                usage of dictionaries and sets preclude all assumptions about elemnt orders in columns or edges 
            */
        public static void DescAllColumnUnique ( Column<GameObject> col_in , ColumnMulti<GameObject> col_out ) {
            var backedgeDict = new Dictionary<
                GameObject,
                HashSet<VBox<GameObject>>          // i'm 99% sure that for a concrete object DescAll can not yield a child seq that's not unique in Reference equal - a list should do - but set doesn't hurt and makes this a no brainer 
                > ();
            foreach ( var vbox_in in col_in.boxesT ) {
                foreach ( var desc_obj in DescAll( vbox_in.valueT() ) ) {
                    if ( backedgeDict.ContainsKey( desc_obj ) ) {
                        backedgeDict[desc_obj].Add( vbox_in );                // this assumes it impossible that for a concrete vbox_in ( iterator fixed ) the same dict key can't be hit twice ( this is true if all payloads are different by ReferenceEquals ) 
                    } else { 
                        backedgeDict[desc_obj] = new HashSet<VBox<GameObject>>();
                        backedgeDict[desc_obj].Add( vbox_in);                 // vboxes are classes -> RefEq uniqueness - which is what we want here 
                    }
                }
            }
            foreach ( var KV in backedgeDict ) {
                GameObject childObject = KV.Key;
                HashSet<VBox<GameObject>> sources = KV.Value;
                col_out.AddVal(childObject , sources.Select( box => (VBox) box ) );   // the usual explicit upcast %#$! VBox<GameObject> --> VBox 
            }

        }
        // pasta job to satisfy the type system - there is no real way to wrap this without huge amounts of boiler plate + runtime impact
        public static void DescAllColumnUnique ( Column<Component> col_in , ColumnMulti<GameObject> col_out ) {
            var backedgeDict = new Dictionary<
                GameObject,
                HashSet<VBox<Component>>          // i'm 99% sure that for a concrete object DescAll can not yield a child seq that's not unique in Reference equal - a list should do - but set doesn't hurt and makes this a no brainer 
                > ();
            foreach ( var vbox_in in col_in.boxesT ) {
                foreach ( var desc_obj in DescAll( vbox_in.valueT() ) ) {
                    if ( backedgeDict.ContainsKey( desc_obj ) ) {
                        backedgeDict[desc_obj].Add( vbox_in );                // this assumes it impossible that for a concrete vbox_in ( iterator fixed ) the same dict key can't be hit twice ( this is true if all payloads are different by ReferenceEquals ) 
                    } else { 
                        backedgeDict[desc_obj] = new HashSet<VBox<Component>>();
                        backedgeDict[desc_obj].Add( vbox_in);                 // vboxes are classes -> RefEq uniqueness - which is what we want here 
                    }
                }
            }
            foreach ( var KV in backedgeDict ) {
                GameObject childObject = KV.Key;
                HashSet<VBox<Component>> sources = KV.Value;
                col_out.AddVal(childObject , sources.Select( box => (VBox) box ) );   // the usual explicit upcast %#$! VBox<GameObject> --> VBox 
            }

        }
            
    }

    public class OP_SG_all_GO : OPCode{
        public TypedCH< GameObject >  CH_in;
        public Column < GameObject >  Col_in;

        public TypedMultiCH < GameObject >  CH_out;
        public ColumnMulti  < GameObject >  Col_out;

        public OP_SG_all_GO ( TypedCH< GameObject > CH_in , TypedMultiCH<GameObject> CH_out ) {
            this.CH_in = CH_in ; this.CH_out = CH_out; 
        }

        public override void eval(Context c) => OPCode_AUX.DescAllColumnUnique( Col_in , Col_out );    // todo pass through bool inactive 
        public override void fill(MemMapper MM) { Col_in = MM.get(CH_in ); Col_out = MM.get(CH_out );}
    }

    public class OP_SG_all_Comp : OPCode{
        public TypedCH< Component >  CH_in;
        public Column < Component >  Col_in;

        public TypedMultiCH < GameObject >  CH_out;
        public ColumnMulti  < GameObject >  Col_out;

        public OP_SG_all_Comp ( TypedCH< Component > CH_in , TypedMultiCH<GameObject> CH_out ) {
            this.CH_in = CH_in ; this.CH_out = CH_out; 
        }

        public override void eval(Context c) => OPCode_AUX.DescAllColumnUnique( Col_in , Col_out );    // todo pass through bool inactive 
        public override void fill(MemMapper MM) { Col_in = MM.get(CH_in ); Col_out = MM.get(CH_out );}
    }
    public static partial class OPGEN { 
        public static OPCode MK_OP_SG_all ( TypedCH CH_in , TypedCH CH_out ) {
            if ( CH_in.GetType().GetGenericArguments()[0] == typeof( GameObject ) ) {
                return new OP_SG_all_GO ( (TypedCH<GameObject>) CH_in , (TypedMultiCH<GameObject>) CH_out); 
            } else { 
                return new OP_SG_all_Comp( (TypedCH<Component>) CH_in , (TypedMultiCH<GameObject>) CH_out); 
            }
        }
    }


    #endregion 

    // these two are identical implementation wise , but i can't restrict:  T_in :: GameObject|Component 
    public class OP_ComponentFilterGO< T_component > : OPCode  where T_component : UnityEngine.Component {
        public TypedCH< GameObject >  CH_in;
        public Column < GameObject >  Col_in;

        public TypedSingleCH < T_component >  CH_out;
        public ColumnSingle  < T_component >  Col_out;
        

        public OP_ComponentFilterGO ( TypedCH< GameObject > CH_in , TypedSingleCH<T_component> CH_out  ) {
            this.CH_in  = CH_in;
            this.CH_out = CH_out;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get( CH_in );
            Col_out = MM.get( CH_out );
        }
        public override void eval(Context c) {
            foreach ( var box_in in Col_in.boxesT ) {
                T_component comp = box_in.valueT().GetComponent<T_component>();
                if ( comp!=null ) Col_out.AddVal( comp , box_in );
            }
        }
    }
    public class OP_ComponentFilterComp< T_comp_in , T_comp_out > : OPCode  
        where T_comp_out : UnityEngine.Component
        where T_comp_in  : UnityEngine.Component
        {
        public TypedCH< T_comp_in  >  CH_in;
        public Column < T_comp_in  >  Col_in;

        public TypedSingleCH < T_comp_out >  CH_out;
        public ColumnSingle  < T_comp_out >  Col_out;
        

        public OP_ComponentFilterComp ( TypedCH< T_comp_in > CH_in , TypedSingleCH<T_comp_out> CH_out  ) {
            this.CH_in  = CH_in;
            this.CH_out = CH_out;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get( CH_in );
            Col_out = MM.get( CH_out );
        }
        public override void eval(Context c) {
            foreach ( var box_in in Col_in.boxesT ) {
                T_comp_out comp = box_in.valueT().GetComponent<T_comp_out>();
                if ( comp!=null ) Col_out.AddVal( comp , box_in );
            }
        }
    }
 
    public static partial class OPGEN {
        public static OPCode MK_ComponentFilter( TypedCH CH_in , TypedCH CH_out ) {
            var l_type = CH_in. ttuple.PayT;
            var r_type = CH_out.ttuple.PayT;
             //                                                    look mom ! template specialisation in c#    ^_______^
            if ( l_type == typeof ( GameObject )  ) {   
                var closedType = typeof ( OP_ComponentFilterGO<> ) .MakeGenericType( new [] { r_type } ) ;
                return (OPCode) Activator.CreateInstance( closedType , new SObject [] { CH_in , CH_out } ) ;
            } else {
                var closedType = typeof ( OP_ComponentFilterComp<,> ) .MakeGenericType( new [] { l_type , r_type } ) ;
                return (OPCode) Activator.CreateInstance( closedType , new SObject [] { CH_in , CH_out } ) ;
            }
        }
    }

    #region FILTER

    public class OP_UnaryFilter_SingleC<BoxT> : OPCode
    {
        public TypedCH < BoxT > CH_in  ;
        public Column  < BoxT > Col_in ; 

        public TypedSingleCH < BoxT > CH_out  ; 
        public ColumnSingle  < BoxT > Col_out ;

        public Func<BoxT,bool> FilterF ;

        public OP_UnaryFilter_SingleC( TypedCH<BoxT> CH_in , TypedSingleCH<BoxT> CH_out , Func<BoxT,bool> FilterF ) 
        { this.CH_in = CH_in ; this.CH_out = CH_out; this.FilterF = FilterF;}

        public override void eval(Context c)
        {
            foreach ( var box_in in Col_in.boxesT ) {
                if ( FilterF ( box_in.valueT() )  ) Col_out.AddVal( box_in.valueT() , box_in ) ;
            }
        }

        public override void fill(MemMapper MM)
        {
            Col_in  = MM.get( CH_in  ) ;
            Col_out = MM.get( CH_out ) ;
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_OP_UnaryFilter_SingleC ( TypedCH CH_in , SingleCH CH_out ) {
            var payT       = CH_in.ttuple.PayT ;
            var closedType = typeof ( OP_UnaryFilter_SingleC<> ).MakeGenericType( new [] { payT} ) ; 
            return (OPCode) Activator.CreateInstance( closedType , new SObject [] { CH_in , CH_out } ); 
        }
    }

    /*
        Column Layout : 

        C<A>        |   C<B>                   |      C<A> 
        LHS colm    |  Filter argument colm    |      result colm 

        or rather 
        C<B>   \
        C<A>  --F --- C<A>  

        if FilterFun ( LHS_elem , argument_elem ) => copy LHS_elem to result column

        this kicks the can of type-conversion down the road :
        the compile side has to provide a fitting  "(A,B) -> bool" to deal with stuff like  ( 3 > 3.5f ) 

        eventually, somewhere, the whole apparatus of implicit conversion operators, implicit upcast, and the priorities between them that the compiler employs will have to be mirrorred

        additionally : 
            there is a needed invariant for this to work properly 
            i   )      (SomeExpr) == $var
            ii  )      (SomeExpr) == #var
            iii )      (SomeExpr) == @json_literal 

            in case i) and iii) the len's of SomeExpr-Column and $var-Column doesn't matter since they are defined with repeater semantics  
            
            in case ii) i *THINK* a    (anyLHS) #var  the len of the BarrierShift-column belonging to #var is neccesarily the same as of the LHS-Column
            this is one of those things that just "should come together" -- dunno yet how to enforce it, other than being super pedantic while implementing the #-fetching stuff,  ... , and everything it depends on ... 

    */

    public class OP_BinaryFilter_SingleC<A, B> : OPCode
    {

        TypedCH<A>   CH_in  ;
        Column <A>   Col_in ;

        TypedCH<B>   CH_arg ;
        Column <B>   Col_arg;

        TypedSingleCH<A> CH_res;
        ColumnSingle<A>  Col_res; 

        Func<A,B,bool> FilterF ;

        bool use_repeater;
        
        public OP_BinaryFilter_SingleC( TypedCH<A> CH_in , TypedCH<B> CH_arg , TypedSingleCH<A> CH_res , Func<A,B,bool> FilterF , bool use_repeater ) { 
            this.CH_in = CH_in; this.CH_arg = CH_arg ; this.CH_res = CH_res; 

            this.FilterF      = FilterF;
            this.use_repeater = use_repeater;
        }

        public override void eval(Context c)
        {
            IEnumerator<B> arg_enum ;
            // what happens to cyclic if the in-Enumerable has zero Elements ? -- even in the absence of bugs, that would be a valid use case for $vars in the arg 
            // Cyclic throws if the in_seq is empty 

            // default behaviour in this case? 
            // i'm leaning towards silently nope-ing the entire in-Column - but leave this Exception uncought for now as a reminder that this question needs further thought 

            if ( use_repeater ) arg_enum = Col_arg.valuesT.NonEmptyCyclic().GetEnumerator();   
            else                arg_enum = Col_arg.valuesT.GetEnumerator();
            foreach ( var box_in in Col_in.boxesT ) {
                if ( !arg_enum.MoveNext() ) throw new Exception("binary filter (repeater: " + use_repeater + ") : arg column exhausted before in_column -- this is likely bug in #fetch ") ;
                B arg_val = arg_enum.Current;
                if ( FilterF( box_in.valueT() , arg_val ) ) Col_res.AddVal( box_in.valueT() , box_in );
            }
            if ( (!use_repeater) && arg_enum.MoveNext() ) // no repeater means #fetch in this case the enumerator is expected to be exhausted at this point 
                throw new Exception("binary filter : non exhausted arg enumerator ");
        }

        public override void fill(MemMapper MM)
        {
            Col_in  = MM.get( CH_in  ) ;
            Col_arg = MM.get( CH_arg ) ;
            Col_res = MM.get( CH_res ) ;
        }
    }

    public static partial class OPGEN {
        
        /*
            In the general case T1.Equals(T2) does not always exist explicitly even if it "effectually" exists 
            c# typing rules for assignment being as they are, for Fuc<A,B,..> to be assignable the types must match exactly 

            TODO - the goal for the == Operator is to behave exactly as  " a == b " would in plain old c# source with all the implicit conversions, overloads and so on 
                 - emulating this from within reflection will prob. be quite some work 
        */

        public static Func<TObj,TArgTarget,bool> EqualsTypingWrapper <TObj,TArgTarget> ( ) {
            return (obj,arg) => obj == null ? arg == null : obj.Equals(arg );

            /*
                "obj.Equals(arg )" translates to : 
                
		        IL_0003: box !TArgTarget
		        IL_0008: constrained. !TObj
		        IL_000e: callvirt instance bool [mscorlib]System.Object::Equals(object)
		        
		        

                Diese Variante verliert sowieso den compile time statischen Dispatch via overloads 

                Was mich wundert ist, dass es kein boxing fuer TArg gibt ? 

                https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes.constrained?redirectedfrom=MSDN&view=netframework-4.8

                callvirt OPCode wurde gepimpt, extra um diese Art von konstrukt in einem Generic-Context uebersetzen zu koennen 
                (callvirt selbst testet auf ValueType und uebernimmt das boxing automagically ) 

            */

        }

        public static OPCode MK_OP_BinaryFilter_SingleC ( TypedCH CH_in , SingleCH CH_out , SObject FilterF , bool useRepeater ) {
            /*
            var payT1       = CH_in.ttuple.PayT ;
            var payT2       = CH_out.ttuple.PayT ;
            var closedType = typeof ( OP_BinaryFilter_SingleC<,> ).MakeGenericType( new [] { payT1 , payT2} ) ; 
            return (OPCode) Activator.CreateInstance( closedType , new SObject [] { CH_in , CH_out , FilterF , useRepeater } ); 
            */
            throw new NotImplementedException(); 
        }

        // special treatment for Equals 
        public static OPCode MK_EqualsFilter_SingleC ( TypedCH CH_in , TypedCH CH_arg ,  SingleCH CH_out ,  bool useRepeater ) {
            var PayT_in  = CH_in.ttuple.PayT  ;
            var PayT_arg = CH_out.ttuple.PayT ;  // in and out have the same type 
            if ( PayT_in != CH_out.ttuple.PayT ) throw new Exception(); // <- CreateInstance would throw anyhow - but very cryptically 


            MethodInfo WrapperF_T = typeof (OPGEN).GetMethod("EqualsTypingWrapper");
            MethodInfo closed_F   = WrapperF_T.MakeGenericMethod( new [] { PayT_in , PayT_arg } ) ;

            SObject InstantiatedDelegate = closed_F.Invoke( null , new SObject[0] ) ;

            Type       closed_OPC_T = typeof ( OP_BinaryFilter_SingleC<,>).MakeGenericType( new [] { PayT_in , PayT_arg } ) ;

            // public OP_BinaryFilter_SingleC( TypedCH<A> CH_in , TypedCH<B> CH_arg , TypedSingleCH<A> CH_res , Func<A,B,bool> FilterF , bool use_repeater ) 


            // Some typeMapping problems here 
            // come to think of it: this "automagically choose the correct overload" has to be doing quite a bit of magic to work as advertized
            // a complete and precise description of its inner workings would probably span pages - if it exists somewhere 

            // ok ... i'm an idiot (passed MethodInfo for a delegate) , ... but still 

            // OPCode     inst = (OPCode) Activator.CreateInstance( closed_OPC_T , new SObject[] { CH_in , CH_arg , CH_out , closed_F , useRepeater } );

            ConstructorInfo CInfo = closed_OPC_T.GetConstructors()[0]; // don't intend to ever have more than one constructors on any of these 

            return (OPCode) CInfo.Invoke( new SObject[] { CH_in , CH_arg , CH_out , InstantiatedDelegate , useRepeater } ) ;

        }
    }


    #endregion 





}