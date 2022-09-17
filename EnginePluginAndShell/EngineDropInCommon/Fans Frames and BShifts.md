


The only non trivial part about FanTU is the BShift operator.

Since the the result Z in 

` <EXP 1> ... <EXP N> { <FanElemExpr>+ } -> Z `

Is of `type( <EXP N> )` and a subset of `<EXP N>` the fan construct itself needs no representation as c# data type. Its value is simply unaccessable.

The Frame : 

` [ <FrameExpr>+ ] ` is convertable to either Tuple, List or Dictionary.



#### `BShift` semantics 

The Layout of Columns and VBoxes of a general FanExpression currently looks like this.
![pls ignore my lack of understanding how to kill rotation meta info](FanColumnLayout.JPG)

I do not actually know why i decided to set the `pred` field of of the VBoxes in the BShift columns
to the original Fan-LHS VBoxes.

If there is a variable decl, for example in `<Expr1>` it would be hard/impossible to reach this way.
This might have come from the `VBoxTU.dataSRC` field intended to mirror the subsequent layout of VBoxes. 

... needs investigation 

### Quick notes 

    the link from `Column -> CH` already exists and is also already populated by `SpawnColumn`
    i.e. everything that is needed to do the BShift backtracking in a less fragile way already exists 









