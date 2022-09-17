


The parser comb implementation is pretty straight forward, except for the way it does recursive productions.

Since individual productions are represented as instances in static variables,
the order of their declaration matters.
( actually nothing forces use of static vars, the point is: they are inheritely runtime constructs - no errors can be found at compile time ) 

```c#
    static PI Prod_A = Prod<A> ( STAR( Prod_B )) 
    static PI Prod_B = Prod<B> ( .... ) 
```

Static vars are initialized at DLL load time in the order in which they occur in the source file.
In the case above `Prod_B` would still be `null` at `Prod<A>` calltime. 
( afaik dotnet does something akin to dependency toposorting, but only at the granularity of assemblies )  


This, of cause, neccesitates special treatment for recursive definitions.

```c#
static PI_defer  Fan = MKProdDefer<FanNode>();

static PI PrimitiveStep = Prod<PrimitiveStepNode> ( SEQ ( 
                OR (... , 
                        Fan 
                        ) , 
                    ... 
                ) );
static PI FanElem = Prod<FanElemNode> ( PLUS ( PrimitiveStep ));

static             PI  _Fan = SETProdDefer( Fan , 
                                            SEQ ( ... , SEQ ( FanElem , ... )  , ...  );

```

`MKProdDefer(...)` creates a dummy Production of type `Prod` that can be plugged in where normal productions would go.
A reference to a fully fledged `Prod` needs to be supplied to it later.

`_Fan` is never used. Its declaration is a way of triggering the execution SETProdDefer ( and to make the code look more uniform =). 

( Todo: could use some user friendly Exceptions to be thrown here. 
    Atm a user readable exception flies as soon as an actual parse triggers running of that production. ) 




