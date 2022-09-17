using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using SObject = System.Object;
using D = System.Diagnostics.Debug;

using MG = MainGrammar.MainGrammar;
using SGA = SuggestionTree.SuggTAdapter;



namespace TranslateAndEval {


    public abstract class TranslationUnit
    {
        public abstract VBoxTU[] VBoxTUs{ get; }

        // not quite sure yet
        // this is for TURX .... probably most sensible to swap the inheritance arrow between TU > TURX 
        // Concrete TUs already use something like this, but by convention only rather then interface - this might actually be the better solution 
        //public virtual preCH preCH_out => VBoxTUs.Last().preCH_out;  

        // translation phases
        public          preCH_deltaScope scope ( CH_closedScope   c ) { return scope ( new preCH_deltaScope (c ) ); }
        public abstract preCH_deltaScope scope ( preCH_deltaScope c ) ;
        
        public abstract IEnumerable<OPCode> emit();
        
    }

    public interface __VBoxTU_in {
        preCH   preCH_in   {get; }
        TypedCH CH_in    {get; }
    }

    public interface __VBoxTU_out {
        preCH   preCH_out  {get; }
        TypedCH CH_out   {get; } 
    }

    public interface VBoxTU : __VBoxTU_in , __VBoxTU_out { 
        IEnumerable<OPCode> emit();
    }

    public interface VBoxTUMem : VBoxTU {  // for VBoxTUs that stride conicide with an edge in c# object ref graph 
        MemberInfo MI { get; }
    }
    
    public abstract class __VBoxTU_pIn : TranslationUnit, __VBoxTU_in {
        protected /* readonly */ preCH  backing_preCH_in;         // we just can't have nice things ... readonly must be written to in the constructor of the class that declares it, or not at all 

        public preCH   preCH_in  { get {
            if ( backing_preCH_in == null ) throw new Exception(); 
            return backing_preCH_in;
        } }
        public TypedCH CH_in => backing_preCH_in.CH ;
    }

    public abstract class __VBoxTU_CIN : TranslationUnit, __VBoxTU_in {

        protected /* readonly */ TypedCH backing_CH_in;
        private preCH              __backing_pCH_in;
        

        public preCH   preCH_in { get {
                if ( __backing_pCH_in == null ) __backing_pCH_in = new adapter_preCH ( CH_in );  // chain over public accesssor to null check 
                return __backing_pCH_in;
            } }
                
        public TypedCH CH_in   { get {
                if ( backing_CH_in == null ) throw new Exception();
                return backing_CH_in;
            } }
    }

    // for lack of multi inheritance write out the whole card-prod of possibilities : 4  
    // an implementing type cooses one of these, to decide by which means it wants to satisfy VBoxTU 

    public abstract class VBoxTU_pIN_pOUT : __VBoxTU_pIn , VBoxTU {
        protected /* readonly */ preCH backing_preCH_out;

        public preCH preCH_out { get { 
                if (backing_preCH_out == null ) throw new Exception();
                return backing_preCH_out;
            } }

        public TypedCH CH_out => backing_preCH_out.CH;
        override public VBoxTU[] VBoxTUs => new [] { this};
    }

    public abstract class VBoxTU_pIN_cOUT : __VBoxTU_pIn ,VBoxTU {
        protected /* readonly */ TypedCH backing_CH_out;
        private                  preCH __backing_preCH_out ;

        public preCH preCH_out { get { if ( __backing_preCH_out == null ) __backing_preCH_out = new adapter_preCH ( backing_CH_out );
            return __backing_preCH_out;
            } }    
        public TypedCH CH_out { get { if ( backing_CH_out == null ) throw new Exception();
            return backing_CH_out;
            } }

        override public VBoxTU[] VBoxTUs => new [] { this};
    }
    public abstract class VBoxTU_cIN_cOUT : __VBoxTU_CIN ,VBoxTU {
        protected /* readonly */ TypedCH backing_CH_out;
        private                  preCH __backing_preCH_out ;

        public preCH preCH_out { get { if ( __backing_preCH_out == null ) __backing_preCH_out = new adapter_preCH ( backing_CH_out );
            return __backing_preCH_out;
            } }    
        public TypedCH CH_out { get { if ( backing_CH_out == null ) throw new Exception();
            return backing_CH_out;
            } }

        override public VBoxTU[] VBoxTUs => new [] { this};
    }

    public abstract class VBoxTU_cIN_pOUT : __VBoxTU_CIN , VBoxTU {
        protected /* readonly */ preCH backing_preCH_out;

        public preCH preCH_out { get { 
                if (backing_preCH_out == null ) throw new Exception();
                return backing_preCH_out;
            } }

        public TypedCH CH_out => backing_preCH_out.CH;
        override public VBoxTU[] VBoxTUs => new [] { this};

    }
    // ... the boilerplate god is fascinating in the concistency of his cruelty 
    // no matter where you are, salvation is always 100 lines of boilerplate away 

}