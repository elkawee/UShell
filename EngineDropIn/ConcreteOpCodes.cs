
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
             
            var data_rator = Col_aux.boxesT.Cyclic().GetEnumerator();
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
            while ( currentCH != origCH ) { currentCH = currentCH.DataSrc.CH_in; backsteps ++ ; } // if dataSrc is null, that's a bug in translate 
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
                var origBox = (VBox<PayOrig>) currBox;
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

    public class OP_filter<T>:OPCode {  // untested
        public TypedCH<T>       CH_in;
        public TypedSingleCH<T> CH_out;
        public Column<T>        Col_in;
        public ColumnSingle<T>  Col_out;
        public Func<T,bool>     filterfunc;
        public OP_filter ( TypedCH<T> CH_in , TypedSingleCH<T> CH_out , Func<T,bool> filterfunc ) {
            this.CH_in  = CH_in;
            this.CH_out = CH_out;
            this.filterfunc = filterfunc;
        }
        public override void fill(MemMapper MM) {
            Col_in  = MM.get( CH_in );
            Col_out = MM.get( CH_out );
        }
        public override void eval(Context c) {
            foreach ( var box_in in Col_in.boxesT ) {
                if ( filterfunc( box_in.valueT() ) ) Col_out.AddVal( box_in.valueT() , box_in ) ;
            }
        }
    }

    public static partial class OPGEN {
        public static OPCode MK_filter ( TypedCH CH_in , TypedCH CH_out , SObject filterfunc ) {
            var Tparam = CH_in.ttuple.PayT ;
            var closedType = typeof ( OP_filter<>).MakeGenericType( new [] { Tparam } );
            return (OPCode)Activator.CreateInstance( closedType , new SObject [] { CH_in , CH_out , filterfunc } );
        }
    }

    public class OP_SuiGen<T>:OPCode {

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




}