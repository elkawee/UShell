
The original idea was to have derivation of type info in lazy eval.
Auto completion needs typing, the idea was to have it available even if only the relevant subset of the current expression is typeable.
( evaluation propagates through the expression only as far as needed to determine the type at the requested position and then terminates ) 

In hindsight: this turned translation into a goldberg machine 3x as confusing to read and reason about as it has any need to be =/ 

Thus, a lot of the following is mostly needless and will be replaced by and explicit dependency graph for type derivation at the next possible opportunity.

Translation and Eval goes like this : 

`preCH` -> `CH` -> `Column`

The data structure that finally holds data is the `Column` (mostly a list with some extra bits). These things are fully typed, taking advantage of the fact that the entirety of the c# type checking machinery is available at runtime via reflection.
Since OpCodes bind to Columns and are themselves fully typed, successfully instaniating a set of opcodes, plumbed with their columns, yields code for ( yet another ) virtual machine that is about as type save as c# itself.

### `CH`

Column Header.
An expression is translated once, cached and then might be executed multiple times, each time with freshly allocated memory.
The expressions are deliberately simple enough that the entire memory layout is constant for a given expression and knowable at translate time.
The column header is that knowledge.

example : 
```csharp
public class TypedSingleCH<T> : TypedCH<T> , SingleCH {
        public TTuple ttuple { get { return new TTuple { PayT = typeof (T) , isMulti = false } ; } }
        public VBoxTU    DataSrc {get; set; }  
        public Column<T> SpawnColumn() { return new ColumnSingle<T> { _CH = this }; } 
    }
```

`T` is the payload type. ( in  `>> :Transform .%position` the last ColumnHeader would be `Vector3` )
( the multiCH/singleCH thingie comes later ) 


### `preCH`

Since neither `Column`s nor `CH`s can be instantiated without knowing their payload-types, they are represented by this structure at first.

They come in 2x2 flavors.
- "bijecting" (`explicit_preCH`, `deferred_preCH` ) - meaning exactly one instance of `CH` will be present in the translated structure for every one of those 
- "non bijecting" ( `adapter_preCH` , `deferred_adapter_preCH` ) - meaning they point to a `CH` "belonging" to some other `preCH` 

`explicit_preCH` and `adapter_preCH` are akin to "grounds" in the type derivation graph and used wherever a type is immediately available from a single syntactic primitve.

example: the typefilter `:Transform` from above.
Its out-Column is ... well it could be static, but it is currently not (?) 

```csharp
public class TypeFilterVBX_TU:VBoxTU_pIN_pOUT {
       public TypeFilterVBX_TU ( preCH preCH_in , string [] typefilter_names ) {
           this.backing_preCH_in = preCH_in;
           Func<TTuple> deferredTT = () => new TTuple {
               PayT = SGA.QTN_Exact ( typefilter_names ) ,   
               isMulti = false
           } ;
           this.backing_preCH_out = new deferred_preCH( deferredTT , dataSrc: this ) ;
       }
       /*  ....    */ 

   }
```

As can be seen, the `deferredTT` lambda makes no use of the in-column. 
It's kinda useful this way: as an invalid type name would not abort translation in the constructor and allows to progress to the scoping phase. But ... i'm not sure that's why i wrote it like that 

... anywho 


### `VBoxTU_{p,c}in_{p,c}out`

A VBoxTU is a special subset of `TranslationUnit`.
It represents a single edge of computation between two subsequently evaluated columns.
They too come in 2x2 flavors, but over slightly different domains.

`p` and `c` stand for preCH and CH. ( column header is instantiated deferred, of immediately, respectively ) 

The point is to give an uniform interface to all these basic computation edges the full set of possibilities with regards to access ( preCH,CH for in and out column ) 

```csharp

abstract class VBoxTU_base_type {  // ... that doesn't actually exist in this form because of type system limitations 

       preCH   preCH_in   {get; }
       TypedCH CH_in      {get; }
       preCH   preCH_out  {get; }
       TypedCH CH_out     {get; }
} 

```

Only the abstract classes : 

- `VBoxTU_cIN_cOUT`
- `VBoxTU_cIN_pOUT`
- `VBoxTU_pIN_cOUT`
- `VBoxTU_pIN_pOUT`

Are intended to derive actual implementations of `VBoxTU` from.
Each provides one of { `protected TypedCH backing_CH_out` , `protected preCH backing_preCH_out` }
and one of           { `protected  TypedCH backing_CH_in` , `protected preCH backing_preCH_in` } 
depending on which variant was chosen.
An implementation is expected to fill both in its constructor. 
(sadly they must be protected members and can't be constructor arguments, since the amount of computation allowed before calling the base constructor is too limited ) 

The ROI for all this ugliness is to have the boilerplate for all 4 properties done once (and hopefully correct) in those abstract classes. 
(debateable if worth it) 





