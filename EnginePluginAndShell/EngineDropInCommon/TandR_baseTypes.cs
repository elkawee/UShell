using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using SObject = System.Object;
using D = System.Diagnostics.Debug;


namespace TranslateAndEval {

    // derive all Exceptions that are intended to leave this namespace from this : 
    public class TranslateAndEvalException : Exception { } 

    // (far goal is for any other Exceptions leaving to code outside this namespace  ==> BUG )


    /* basically encapsulation of arguments needed for TypedCH instantiation. ( and selecting which subtype to instantiate ) 
        point being: that most of the code that deals with instantiating these guys, 
        only passes this around and does not care what it looks like 
        */
    public struct TTuple    { 
        // public Type BoxType ;   
        public bool isMulti ;       // it might be reasonable to introduce more Column-Types in this case preCH.Instantiate() needs changing too 
        public Type PayT ;
    }
    public interface TypingSrc {  TTuple ttuple { get; }  }

    #region CH

    public interface TypedCH : TypingSrc {
        VBoxTU  pred_SrcVBXTU {get; }  /* maybe null 

                                          every implementation of VBoxTU is required to generate OPCodes such that 
                                          the pred fields of their VBoxes all point into the column: 
                                          `pred_SrcVBXTU.CH_in`

                                          this is the assumption, that the backedge counting algos currently use 

                                          superfluous BShifts interspersed in `Fan` and `Frame` are kind of a cope for this 
                                          (their typing src as well as the LHS of their _internal_ operation are different Columns as the
                                           one into which the backedge is supposed to point ) 
                                        */
        
    }    
    public interface TypedCH<T> : TypedCH {
        Column<T> SpawnColumn();
    }                 


    public interface MultiCH  : TypedCH { } 
    public interface SingleCH : TypedCH { } 

    // actually instaiable types : 
    public class TypedMultiCH<T>  : TypedCH<T> , MultiCH  {
        public TTuple ttuple { get { return new TTuple { PayT = typeof (T) , isMulti = true } ; } } 
        public VBoxTU    pred_SrcVBXTU {get; set; }  
        public Column<T> SpawnColumn() { return new ColumnMulti<T> { _CH = this  } ; } 
    }  
    public class TypedSingleCH<T> : TypedCH<T> , SingleCH {
        public TTuple ttuple { get { return new TTuple { PayT = typeof (T) , isMulti = false } ; } }
        public VBoxTU    pred_SrcVBXTU {get; set; }  
        public Column<T> SpawnColumn() { return new ColumnSingle<T> { _CH = this }; } 
    }

    #endregion

    #region preCH

    public abstract class preCH : TypingSrc {
        public abstract TypedCH CH { get; }   // usage relies on preCH -> CH (by object instance) to be a pure function. that is, assign underlying field at most once, if any 
        public  static TypedCH Instantiate( TTuple ttuple , VBoxTU dataSrc ) {
            Type closedType ; 
            if ( ttuple.isMulti ) {
                closedType = typeof ( TypedMultiCH<>  ).MakeGenericType( new [] { ttuple.PayT } );
            } else { 
                closedType = typeof ( TypedSingleCH<> ).MakeGenericType( new [] { ttuple.PayT } );
            }
            var instance =  (TypedCH) Activator.CreateInstance( closedType );
            instance.GetType().GetProperty("pred_SrcVBXTU").SetValue( instance , dataSrc , new SObject[0] ) ;  // guess 
            return instance ;
        }
        public abstract TTuple ttuple  { get; }
        public abstract VBoxTU dataSrc { get; }
        public Type PayT => ttuple.PayT;
    }     
    
    /*
        idea : 
        abuse interfaces to tag preCHs with their semantic subtleties, 
        then use those for variable declaration where it is necessary for desired invariants to hold 
        example: slots in TranslationUnits could gain clarity over whether a CH is introduced by that unit or simply referenced from an other 
                 ( this could parallel bijecting/non-bijecting  ... needs some thought ) 
                 ( out_CH of a VBoxTr is always introduced by that same TR ? ) 
    */


    // bijecting pre_CHs
    public class explicit_preCH : preCH {
        TypedCH   _CH;
        TTuple    _types;
        VBoxTU    _dataSrc;
        public override TypedCH   CH  =>  _CH;    
        public override TTuple ttuple =>  _types; 
        public override VBoxTU dataSrc => _dataSrc; 

        public explicit_preCH( TTuple types_in , VBoxTU _dataSrc  ) {
            _types = types_in ;
            this._dataSrc = _dataSrc;
            // since this is bijective, there is no point in deferring creation 
            _CH = Instantiate(types_in , dataSrc);
        }
        
        
    }
    public class deferred_preCH : preCH  {
        TypedCH     _CH;
        TTuple      _types;
        VBoxTU      _dataSrc = null ;
        Func<TTuple> TypeCalcEdges;  // funky name because outgoing dependencies are contained in the closure of this func 
        
        public deferred_preCH ( Func<TTuple> F_in , VBoxTU dataSrc ) { TypeCalcEdges = F_in ; this._dataSrc = dataSrc ;}
        void init() {
            _types = TypeCalcEdges();
            _CH    = Instantiate(_types , dataSrc);  // <- das isses 
            
        }
        public override TTuple ttuple   { get { if (_CH == null ) init() ; return _types ; } }
        public override TypedCH   CH    { get { if (_CH == null ) init() ; return _CH;     } }
        public override VBoxTU dataSrc => _dataSrc; 
    }

    /*
        the MemoryManager uses CH instances as keys
        this means, if a "future CH" is referenced in more than one place
        accessing the CH/triggering its instantiation from all of them must return the same instance 
        ( that's the point of the adapter variants ) 
    */

    //non bijecting pre_CHs
    public class adapter_preCH:preCH {
        public override TypedCH CH      =>  _CH ; 
        public override TTuple ttuple   =>  _CH.ttuple ;
        public override VBoxTU dataSrc   => _CH.pred_SrcVBXTU ;

        TypedCH _CH ;
        public adapter_preCH ( TypedCH CH_in ) { _CH = CH_in ; }
    }
    public class deferred_adapter_preCH : preCH {
        TypedCH _CH; 
        Func<TypedCH> getter ;
        public deferred_adapter_preCH ( Func<TypedCH> getter ) { this.getter = getter;}
        public override TypedCH CH      {  get { if (_CH == null ) _CH = getter() ; return _CH ; } }
        public override TTuple ttuple   {  get { if (_CH == null ) _CH = getter() ; return _CH.ttuple ;  } }
        public override VBoxTU dataSrc  {  get { if (_CH == null ) _CH = getter() ; return _CH.pred_SrcVBXTU ; } }
            
        
    }


    #endregion

    #region VBox 
    /*
    ecma 334 : 
    " The new modifier is only permitted on interfaces defined within a class. It specifies that the interface hides an inherited member by the same name, as described in §15.3.5"

        was bedeutet "whithin a class" nested type ??? is das post 3.5 ? 
    */


    public interface VBox                     {    IEnumerable<VBox>  preds();  object value  ();  } 
    public interface VBox<T>    : VBox        {                                      T valueT ();  }    
    public interface VBoxMulti  : VBox        {    void AddPred(VBox vb);                          }
    public interface VBoxSingle : VBox        {    VBox pred() ;                                   }

    
    public class VBoxMulti<T>: VBox<T>, VBoxMulti {
        public List<VBox> _preds;
        public T _value;
        public void AddPred(VBox vb) {
            _preds.Add(vb);
        }
        public IEnumerable<VBox> preds() {
            return _preds;
        }

        public object value() {
            return _value;
        }

        public T valueT() {
            return _value;
        }
    }
    public class VBoxSingle<T>:VBox<T>, VBoxSingle {
        public VBox _pred;
        public T    _value;
        public VBox pred() {
            return _pred;
        }

        public IEnumerable<VBox> preds() {
            return new [] { _pred };
        }

        public object value() {
            return _value;
        }

        public T valueT() {
            return _value;
        }
    }
    #endregion

    #region Column
    public interface Column {
        TypedCH  CH { get; }
        IEnumerable<object> values {get ;}
        IEnumerable<VBox>   boxes {get; }
    }

    public interface Column<T> : Column {
        IEnumerable<VBox<T>> boxesT  { get; }
        IEnumerable<T>       valuesT { get; }
        TypedCH<T>           CHT     { get; }

        void AddVal( T _val ) ;
        void AddVal( T _val , VBox pred) ;
        
    }

    public interface ColumnSingle : Column { 
        IEnumerable<VBoxSingle> boxesSingle { get; }
    }

    public abstract class ColumnImpl<T,VBoxT> : Column<T> where VBoxT : VBox<T> /* , new () */ {
        protected List<VBoxT> _boxes = new List<VBoxT>();

        // this would be an upcast ( VBox<T> =/= VBoxT ) if covariance was available 
        // because 3.5, upcast every element. Todo: faster version with VBoxSing/VBoxMulti return types in the concrete classes 
        public IEnumerable<VBox<T>> boxesT  => _boxes.Select( box => (VBox<T>) box);   
        public IEnumerable<VBox>    boxes   => _boxes.Select( box => (VBox) box );

        public IEnumerable<T>       valuesT { get {return _boxes.Select( box => box.valueT() );   } }

        protected abstract VBoxT CreateBox ( VBox pred,  T pay ) ;
        public    abstract TypedCH<T>           CHT { get; }
        public             TypedCH              CH => CHT;

       
        public void AddVal( T _val ) {
            AddVal( _val , null );
        }
        public void AddVal( T _val , VBox pred) {
            _boxes.Add( CreateBox( pred , _val ));
        }
        public IEnumerable<object> values => valuesT.Select ( _ => (object ) _ );
        public override string ToString() {
            string hdr = GetType().Name+"["+GetType().GetGenericArguments()[0].Name + "] " ;
            return hdr + string.Join(  " : " , valuesT.Select( v => v.ToString() ).ToArray() ) ;
        }
    }

    public class ColumnSingle<T> : ColumnImpl<T,VBoxSingle<T>> , ColumnSingle {
        public TypedSingleCH<T> _CH;
        public override TypedCH<T>           CHT { get { return _CH; } }

        public IEnumerable<VBoxSingle> boxesSingle => _boxes.Select( _ => (VBoxSingle) _ );

        protected override VBoxSingle<T> CreateBox(VBox pred,T pay) {
            return new VBoxSingle<T> { _value = pay , _pred = pred };
        }
    }
    public class ColumnMulti<T>:ColumnImpl<T,VBoxMulti<T>> {
        public TypedMultiCH<T> _CH;
        public override TypedCH<T>           CHT { get { return _CH; } }
        protected override VBoxMulti<T> CreateBox(VBox pred,T pay) {
            return new VBoxMulti<T> { _value = pay , _preds = new List<VBox> ( new [] { pred } ) };
        }
        public VBoxMulti<T> CreateBox( IEnumerable<VBox> preds , T pay ) {
            return new VBoxMulti<T> { _value = pay , _preds = new List<VBox> ( preds ) };
        }
        public void AddVal( T _val , IEnumerable<VBox> pred_boxes ) {
            _boxes.Add( CreateBox( pred_boxes , _val ));
        }
    }

    #endregion

    public class MemMapper {
        public Dictionary<TypedCH,Column> D = new Dictionary<TypedCH, Column>();
        public Column<T>       get<T> ( TypedCH<T> CH       ) { if (!D.ContainsKey(CH)) D[CH] = CH.SpawnColumn();    return (Column<T>)       D[CH]; }
        public ColumnSingle<T> get<T> ( TypedSingleCH<T> CH ) { if (!D.ContainsKey(CH)) D[CH] = CH.SpawnColumn();    return (ColumnSingle<T>) D[CH]; }
        public ColumnMulti<T>  get<T> ( TypedMultiCH <T> CH ) { if (!D.ContainsKey(CH)) D[CH] = CH.SpawnColumn();    return (ColumnMulti<T>)  D[CH]; }

        // untyped version outside of these overloads ... to avoid mind pretzels 
        // invariant of proper type association between CH and Column now rests within the SpawnColumn method 
        public Column          getGen ( TypedCH CH )          { if (!D.ContainsKey(CH)) D[CH] = (Column)CH.GetType().GetMethod("SpawnColumn").Invoke(CH,new SObject[0]) ;    return   D[CH]; }
    }



    /* 
        dummy class that is simply passed through the entire translation process - kind of an escape hatch for when i design myself into a corner 
        - one of it's original inteded use cases was to have prefixes per expression (at syntax level to switch between different behaviours ) 
    */
    public class Context { } 

    public abstract class OPCode {
        public abstract  void fill( MemMapper MM ) ;
        public abstract  void eval ( Context c ) ;
    }


#region generic AUX stuff dunno where else to put it 

    public static partial class AUX {
        public static BindingFlags BiF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ;

        // SEQ -> SEQ of infinite len 
        public static IEnumerable<T> NonEmptyCyclic<T>( this IEnumerable<T>  inSeq ) {
            if ( ! inSeq.Any() ) throw new Exception();
            var rator = inSeq.GetEnumerator();
            while ( true ) {
                if (rator.MoveNext() ) {
                    yield return rator.Current;
                } else {
                    // rator.Reset();   //some linq stuff throws NotImplementedException on this 
                    rator = inSeq.GetEnumerator();
                }
            }
        }
    }
#endregion

}