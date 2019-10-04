
using System;


using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using SObject = System.Object;

using TranslateAndEval;
using MainGrammar;
using ParserComb;
using MG = MainGrammar.MainGrammar;
using Tok = MainGrammar.PTok;
using NLSPlain;


public class Compilat {
    //public IEnumerable<Scope.Ref> externals { get { return deltaScope.externals; } }
    //public IEnumerable<Scope.Ref> ownDecls { get { return deltaScope.ownDecls; } }
    public OPCode[] OPs;
    public VBoxTU[] VBoxTrs;
    public string src;
    public CH_deltaScope deltaScope;
    public void run(MemMapper MM) {    // <- will later need a variant with explicit Context passed in 
        var Ctx = new Context();
        foreach(var op in OPs) op.fill(MM);
        foreach(var op in OPs) op.eval(Ctx);
    }
}


#region translate_entry_point_arguments 

// both of these types are translate specific - prob best to merge them into one and use a better name 
// parsing can be done with the startProd alone 
// translation ( to get typing ) needs all 4 fields 


public class GrammarEntry {
    public MG.PI StartProd;
    public Func<ParserComb.NamedNode,TranslationUnit> TR_constructor;
}

public class TranslateLHS { // Aux information for translate - opaque type to pass around in other modules 
    public preCH preCH_LHS;        // <- pretty sure that's not needed anymore 
    public CH_closedScope scope;         // dunno if type choice is optimal here 
}


#endregion


public static class TranslateEntry {

    public static Tok[] LexxAndStripWS(string arg , out PTokBase[] unstrippedToks) {
        unstrippedToks = MainGrammar.Lexer.Tokenize(arg).ToArray();
        var StrippedL = new List<Tok>();
        foreach(MainGrammar.PTokBase tb in unstrippedToks) {
            if(tb is Tok) {
                StrippedL.Add((Tok)tb);
            }
        }
        return StrippedL.ToArray();
    }
    public static Tok[] LexxAndStripWS(string arg) {
        PTokBase [] unstripped;
        return LexxAndStripWS(arg , out unstripped );
    }

    //public static ParserComb.NamedNode LexxAndParse ( string arg ) { return  LexxAndParse( arg , TestMG1.TestStart ) ; }
    public static ParserComb.NamedNode LexxAndParse(string arg,ParserComb.Parser<Tok>.PI startProd) {
        var parse_matches = MG.RUN_with_rest(startProd,LexxAndStripWS(arg));
        return parse_matches.First().N;
    }

    
    public static Compilat TranslateFully(string src,GrammarEntry GE,TranslateLHS eval_LHS) {
        ParserComb.NamedNode NN = LexxAndParse(src,GE.StartProd);


        var TR = GE.TR_constructor(NN);
        var deltaScope    = new preCH_deltaScope ( eval_LHS.scope );
        preCH_deltaScope combinedScope = TR.scope(deltaScope);

        return new Compilat {
            deltaScope = (CH_deltaScope)combinedScope.instantiate(),
            OPs = TR.emit().ToArray(),
            src = src,
            VBoxTrs = TR.VBoxTUs
        };
    }

    public static ParserComb.NamedNode Scope(IEnumerable<PTok> toksIN,CH_closedScope scopeIN,MG.PI StartProd,Func<NamedNode,TranslationUnit> TRInstantiate, out TranslationUnit TRU ) {
        var matches = MG.RUN_with_rest(StartProd,toksIN).ToArray();
        if(matches.Length.NLSend("matchlen") == 0 || matches[0].rest.Any()) throw new Exception(); // no match , or the most greedy match could not consume whole input 
        NamedNode NN = matches[0].N;
        TranslationUnit TR = TRInstantiate(NN);
        var deltaScope = new preCH_deltaScope ( scopeIN );
        var combinedScope = TR.scope(deltaScope);
        TRU = TR;
        return NN;
    }

    public static ParserComb.NamedNode Scope(IEnumerable<PTok> toksIN,GrammarEntry GE,TranslateLHS exLHS) {
        TranslationUnit TRU;
        return Scope(toksIN,exLHS.scope,GE.StartProd,GE.TR_constructor, out TRU);
    }
    
    public static ParserComb.NamedNode ScopeAndType(IEnumerable<PTok> toksIN,CH_closedScope scopeIN,MG.PI StartProd,Func<NamedNode,TranslationUnit> TRInstantiate) {
        TranslationUnit TR;
        var NN = Scope( toksIN, scopeIN, StartProd , TRInstantiate , out TR );
        
        return NN;

    }
    public static ParserComb.NamedNode ScopeAndType(IEnumerable<PTok> toksIN,GrammarEntry GE,TranslateLHS exLHS) {
        return ScopeAndType(toksIN,exLHS.scope,GE.StartProd,GE.TR_constructor);
    }


    public static ParserComb.NamedNode ScopeAndType(string src,CH_closedScope scopeIN,MG.PI StartProd,Func<ParserComb.NamedNode,TranslationUnit> TRInstantiate) {
        ParserComb.NamedNode NN = LexxAndParse(src,StartProd);

        // TR constructors do not provide a uniform interface because there is no need for it 
        TranslationUnit TR = TRInstantiate(NN);
        var deltaScope = new preCH_deltaScope ( scopeIN );
        var combinedScope = TR.scope(deltaScope);
        return NN;
    }

}

public static class Evaluate {

    public static Column Eval(string strExpr , GrammarEntry GE , TranslateLHS trans_LHS , MemMapper MM , out CH_closedScope scope_out) {

        var compilat = TranslateEntry.TranslateFully(strExpr, GE , trans_LHS);
        scope_out = compilat.deltaScope.close();

#if todo_fixme   // external refs in the scopes still need implementation ( done . todo plug this shit in ) 
        foreach(var sc_ref in compilat.ownDecls) scope_out = (ClosedScope)scope_out.decl(sc_ref);
#endif
        compilat.run(MM);  /*atm the MM keeps references on tmp columns around forever - some pruning mechanism is needed */
        VBoxTU last_VBT = compilat.VBoxTrs.Last();
        return MM.getGen(last_VBT.CH_out);
    }

    public static Column Eval(Compilat compilat , MemMapper MM ) {
        compilat.run(MM);
        VBoxTU last_VBT = compilat.VBoxTrs.Last();
        return MM.getGen(last_VBT.CH_out);
    }

}

public class ShellEvaluator {
    public static TypedCH default_LHS_Header; // TODO plug in FakeRoot here 
    public static Column default_LHS_Column; // ditto 

    public CH_closedScope scope = new CH_closedScope();
    public MemMapper MM = new MemMapper();

    public ShellEvaluator() {
        // patch  MM to hold the default LHS - TODO expose a method in MM for this 
        MM.D[default_LHS_Header] = default_LHS_Column;
    }

    public Column Eval(string strExp) {

        throw new NotImplementedException();
    }
}


