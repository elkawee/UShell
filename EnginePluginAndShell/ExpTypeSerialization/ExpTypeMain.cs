using System;
using System.Collections.Generic;


using LightJson;
using System.Linq;
using System.Reflection;

public abstract class ExpType { 

    
    /*
        no matter how this is written, it will yield a default representation in ExpType for every c# type - either by accident or by will 

        how sensible is it to force c# -> ExpType to be a proper function ? 

        e.g. Vector3 , might be sensible to use [ _ , _ , _ ] , but it being a struct naturally also yields { x : _ , y : _ , z : _ } 

        a general precedence of ( has Indexer ) > ( is user defined struct ) will sooner or later collide with a counter example 

        ... the other question is also force uniqueness of representation of for incoming to be serialized data 
        (   inconvenient for the user as he will always have to look up what it is instead of just deriving from declaration of the type itself,
            also limits the usefulness of the ExpType for assigning stuff between two c# types that happen to be "structurally identical enough" ) 
    */

    // public static Dictionary< Type , ExpSerializer > ExplicitMapping ??? // <- and this one is always the first to be checked ? 


    static ExpType ()
    {
        
    }

    /* 
        currently GetSerializer is in here, because that's how you get the mapping ( csharp_type -> ExpType ) 
        - and this is so because they are read from the corresponding serializer 
        - and this is so becuase i want special case overloading for serializers doable by extending a single class 

        but a theoretical 
        GetExpType ( Type csharp_type ) {} 
                  
        could also just do : 
            if ( explicit_serializers.Contains( csharp_type ) ) -> return that types ExpType field 
            else -> use a hardcoded function for the mapping 
    */ 


    public class NoSerializerForTypeException : Exception
    {
        public Type T ; 
        public NoSerializerForTypeException( Type T) {  this.T = T ;}  // i cry everytiem
        public override string ToString()
        {
            return base.ToString() + T.ToString();
        }
    }
    

    public static Dictionary<Type,ExpSerializer> typeCache = new Dictionary<Type, ExpSerializer>();

    public static HashSet<Type>                  greyTypes = new HashSet<Type>();

    public static ExpSerializer GetSerializer(Type rttype , bool only_natural  = false)
    {

        if( typeCache.ContainsKey( rttype ) ) return typeCache[rttype ];

        if( greyTypes.Contains(rttype) )  throw new Exception( "circular serializer dependency in " + rttype.Name );

        // as it currently stands these predicates do not partition the set of types
        // order is crucial here 

        ExpSerializer SER = null ;

        if      ( (!only_natural) && IsCustomSerializedType(rttype )  )   {    SER =  GetCustomSerializer( rttype );                            }
        else if ( SequenceExpType  .canonicallyIs(rttype)  )          {    SER =  SequenceExpType.GetSerializerR(rttype, only_natural) ;    } 
        else if ( StructExpType    .naturallyIs(rttype) )             {    SER =  new default_struct_serializer  ( rttype, only_natural );  }
        else if ( SerPrimitiveExpType .naturallyIs(rttype) )          {    SER =  new ExpSerializer_ExpPrimitive ( rttype );                } 

        else throw new NoSerializerForTypeException(rttype);

        typeCache[rttype] = SER ;
        greyTypes.Remove(rttype);
        return SER;

    }

    public static CustomSerializer GetCustomSerializer( Type rttype )
    {
        var SerType = type2customSerializer[rttype];
        return (CustomSerializer)Activator.CreateInstance(SerType);

    }




    public static bool IsCustomSerializerItself ( Type T) => 
        T.IsSubclassOf( typeof(CustomSerializer) ) &&
        ( ! T.IsGenericTypeDefinition )  && 
        ( ! T.IsAbstract ) ;              

    
    public static Dictionary<Type,Type> type2customSerializer = null ;
        

    public static bool IsCustomSerializedType( Type requested_type)
    {
        if ( type2customSerializer != null ) return type2customSerializer.ContainsKey(requested_type) ;


        type2customSerializer = new Dictionary<Type, Type>();
        var TheSerializerTypesThemselves = AssembliesAux.GetTypeWhere(IsCustomSerializerItself).ToArray();

        foreach ( var serT in TheSerializerTypesThemselves)
        {
            var someT = serT.BaseType.GetGenericArguments()[0];  // TODO  it's theoretically bossible that the typearg is not found in the immediate base type - might need to walk down until the proper class is found 
            type2customSerializer[someT] = serT;
        }

        return type2customSerializer.ContainsKey(requested_type);

        
    }


    public static Type[]  allCustomSerializerTypes = null ;


    public static ExpSerializer GetSerializer( Type rttype , out ExpType exp_type , bool only_natural = false  )    // todo : move this to ExpType
    {
        var ser = GetSerializer(rttype , only_natural);
        exp_type = ser.exp_type;
        return ser;
 
    }

    public static ExpType GetExpType ( Type rttype , bool only_natural = false)
    {
        ExpType dummy ; 
        GetSerializer( rttype , out dummy , only_natural );
        return dummy;
    }

}


public class ExpAny : ExpType { } // for cases in which types vary 


/*
    it would be nice to have a notion of applicability ExpType -> cs_type , 
    in short, if the round trip ( T1 : serialize : deserialize : T2 ) is possible , direct assignment should be possible too 

    subsets of KVs mesh nicely with subclassing in c# for example 
*/


/*
    you could have a two stage sanitation of serialized data 
    example c#    class C { int memb }  , but only a subset of int are valid instances of C

    stage1 sanitation needs to guarantee that member deserialization yields an int 
    stage2 checks for the subset ( by calling its constructor for example ) 

    bugs in the serialization system itself always occur in stage1 ( but they are not the sole source - maleformed input ) 
    in other words stage1 deals with mismatches /representable/ within the ExpType notation 
*/



/*
    the notion of what a constitutes a "primitive type" does NOT depend on the definition of the csharp language, 
    but on the notion of "primitveness" in the serialized representation 
    ( particularly for structs it's not very useful )  // https://stackoverflow.com/questions/1827425/how-to-check-programmatically-if-a-type-is-a-struct-or-a-class

    A SerPrimitve type is a type whos serializer does not depend on any other serializer.
    Also SerPrimitive is the set of types for which the `channel` has to provide the Serializers 
    
    
    that's what those `static Is( Type cs_type ) ` are there for 
    ( the way this is currently written relies on there being one sensible definition of "primitiveness" and "structness", identical over all serialization formats ) 
*/



public class SerPrimitiveExpType : ExpType { 
    public Type cs_type;                   // hmmm ... maybe enum . otoh explicitly creating a subtype of `PrimitveExpType` per csharp type that is "considered a peer of a primitve" can get compile time choice of the prim ser function 

    public static bool naturallyIs( Type cs_type ) => 
        cs_type.IsPrimitive || 
        cs_type == typeof(string) ||
        cs_type.IsEnum;   // todo : string for example , propbably more stuff 

    
    public static SerPrimitiveExpType INT    = new SerPrimitiveExpType { cs_type = typeof(int)    } ; 
    public static SerPrimitiveExpType STRING = new SerPrimitiveExpType { cs_type = typeof(string) } ; 
    public static SerPrimitiveExpType FLOAT  = new SerPrimitiveExpType { cs_type = typeof(float) } ; 
    public static SerPrimitiveExpType BOOL   = new SerPrimitiveExpType { cs_type = typeof(bool) } ; 
    
    public static SerPrimitiveExpType FromCSharpTpye( Type rttype )
    {
        if ( rttype == typeof ( int    ) )  return INT;
        if ( rttype == typeof ( string ) )  return STRING;
        if ( rttype == typeof ( float  ) )  return FLOAT;
        if ( rttype == typeof ( bool   ) )  return BOOL;
        if ( rttype.IsEnum )                return new EnumExpType( rttype) ; 
        throw new NotImplementedException();
    }
}


/*
        besonders mit hinsicht auf relationen zwischen den exp-types ( convertierbarkeit ineinander ) 
        kann die Menge aller enums nicht als einzelner ExpType dargestellt werden 
    */

public class EnumExpType : SerPrimitiveExpType
{
    public EnumExpType ( Type rttype )
    {
        cs_type = rttype ;
    }
    public Type enum_cs_type => cs_type ;   // this is redundant for now, but the `cs_type` field in base is probably going to go away eventually 
}



/*
    eigentlich ist die Is() funktion ein aequeivalent von `DerivedExpType()` ( in dem Sinne, dass keine custom serializers beruecksichtigt werden ) 

*/







public abstract class ExpSerializer
{
    public abstract ExpType exp_type    {get ; } 
    public abstract Type    csharp_type { get; }             // all implementations of this are expected to be singletons, but expressing something as a property of a type in csharp is too much of a headache to be worth it 

    public abstract object      DESER<TMPVAL_Type>( TMPVAL_Type arg , SerializerChannel_min<TMPVAL_Type> channel);
    public abstract TMPVAL_Type SER  <TMPVAL_Type>( object      arg , SerializerChannel_min<TMPVAL_Type> channel);
}

public class ExpSerializer_ExpPrimitive : ExpSerializer
{
    /*
        this instance of boilerplatization only frees from explicitly writing the branch "if(is expPrimitive ) -> call channel.CreatePrimitive() else ...  " 
    */
    public ExpSerializer_ExpPrimitive( Type rttype)
    {
        _csharp_type = rttype;
        _exp_type = SerPrimitiveExpType.FromCSharpTpye( rttype);
    }
    public SerPrimitiveExpType _exp_type ;
    public Type             _csharp_type ; 

    public override ExpType exp_type => _exp_type;

    public override Type csharp_type => _csharp_type;

    public override object      DESER<TMPVAL_Type>( TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel     ) => channel.DESER_Primitive ( _exp_type , arg) ;
    public override TMPVAL_Type SER  <TMPVAL_Type>( object arg     , SerializerChannel_min<TMPVAL_Type> channel     ) => channel.CREATE_Primitive( _exp_type , arg );
    
}


public interface StructView_RO<TMPVAL_Type> {
    TMPVAL_Type Get( string name ) ;
}


public interface StructBuilder<TMPVAL_Type>
{
    void Add(string name , TMPVAL_Type val );
    TMPVAL_Type Final();
}

/*
    written such that it can later be understood as a specialization of 
       abstract  AbstractSerializationPeer<T_Adapter> 
       where T is the supertupe of all in-CSharp representations of temp data needed to construct the serialization 
       
       hmm, hmm ... LightJson has no common supertype over all values. `JsonValue` is convertable to the object and array types, but they don't derive from it 

       e.g. JSonObject for LightJson 
*/

public abstract class SerializerChannel_min<TMPVAL_Type> {
    public abstract object                       DESER_Primitive ( SerPrimitiveExpType exp_type , TMPVAL_Type tmp_val) ;
    public abstract TMPVAL_Type                  CREATE_Primitive( SerPrimitiveExpType exp_type        , object arg);

    public abstract StructView_RO<TMPVAL_Type>   GetStructView_RO   ( StructExpType    exp_type , TMPVAL_Type tmp_val) ;
    public abstract StructBuilder<TMPVAL_Type>   GetStructBuilder   ( StructExpType    exp_type );

    
    public abstract SequenceView_RO<TMPVAL_Type>  GetSequenceView_RO ( SequenceExpType sequence_exp_type , TMPVAL_Type tmp_val ) ;
    public abstract SequenceBuilder<TMPVAL_Type>  GetSequenceBuilder ( SequenceExpType sequence_exp_type , int size);

}

public abstract class SerializerChannel<TMPVAL_Type,FINAL_Type> : SerializerChannel_min<TMPVAL_Type>   // strictly for convenience. FINAL_Type isn't needed in most places: don't introduce it there, lower syntactic clutter 
{
    /*
        not yet sure, how sensible this is 
        first naive assumption was that there is an obvious choice for the final type ( e.g. JSON -> string ) 

        but in practice, i'm reading from a stringstream ? 
    */

    /*
    public abstract TMPVAL_Type INIT    ( FINAL_Type arg );
    public abstract FINAL_Type  FINALIZE( TMPVAL_Type tmp );
    */
}

public class E : Exception {} // todo -- find a name :) 


public abstract class CustomSerializer : ExpSerializer { }

public abstract class CustomSerializerT<T> : CustomSerializer 
{
    public override Type csharp_type => typeof(T);
}



// object to be passed around code paths that are agnostic to the choice of serialization channel ( most of them ) 
public interface DeserCapsule
{
    object DESER(System.Type rttype, bool only_natural = false);
}




class Program
{
    static void Main(string[] args)
    { 
        var channel_json = new ChannelLightJson();
       
        // --------- - - - - - 

       


        var ser_vector = ExpType.GetSerializer( typeof(UnityEngine.Vector3) , only_natural:true );

        JsonValue j_vec = ser_vector.SER( UnityEngine.Vector3.up , channel_json);

        var roundtrip_vec = ser_vector.DESER( j_vec , channel_json); 

        // ---- 
        var arraySH = new float [,] { { 1,2,3,4,1,2,3,4} , {  1,2,3,4,1,2,3,4} } ; 

        var SH = new UnityEngine.Rendering.SphericalHarmonicsL2();
        for ( int i = 0 ; i < 2 ; i++ ) for ( int j = 0 ; j < 8 ; j++ ) SH[i,j] = arraySH[i,j];

        var SER_sh = ExpType.GetSerializer( typeof(UnityEngine.Rendering.SphericalHarmonicsL2) , only_natural:true ); 

    }
}

