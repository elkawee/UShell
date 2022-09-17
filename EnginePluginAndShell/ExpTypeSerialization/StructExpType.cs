using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;



public struct KV {
    public string  name ;
    public ExpType exp ;
}




public class StructExpType : ExpType {
    // public Type            cs_type ;   // from their intuitive meaning , it's better to have them only represent the superimposed structure over the json model and non of the mapping towards csharp
    public IEnumerable<KV> members ;

    public static bool naturallyIs ( Type source ) =>  source.IsValueType && !source.IsPrimitive && !source.IsEnum;    // todo : copied from stackoverflow AND incomplete on top of it :) ( see above ) 

    public StructExpType ( IEnumerable<KV> fields)  // normalize representation, guard against duplicate names 
    {
        var D = new SortedDictionary<string , ExpType>();
        foreach ( var f in fields ) { 
            Console.WriteLine( f.name );
            D.Add(f.name , f.exp);          // Add() throws on duplicate keys 
        }
        members = D.Select( kv => new KV { name = kv.Key , exp = kv.Value }).ToArray();
    }
    public ExpType GetMemberType( string _name ) => members.Where( kv => kv.name == _name  ).First().exp;

    public static FieldInfo[] NaturalFields ( Type rttype)  // shared with the default serializer, already sorted by name 
    {
        var D = new SortedDictionary<string , FieldInfo >();
        foreach ( FieldInfo fi in rttype.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ))
        {
            D.Add(fi.Name,fi);
        }
        return D.Select( kv => kv.Value ).ToArray();
    }
   

}




/*
    there is "generification" along two dimensions : 
        (the kind of serialization , over subsets of csharp types ) 
    generic here refers to the csharp type subsets 
*/

public class default_struct_serializer : ExpSerializer { 
    /*
        fallback implementation for all struct types that don't have an explicit mapping
    */


    public override ExpType exp_type => _exp_type;

    public override Type csharp_type => _csharp_type;

    StructExpType _exp_type    ;
    Type          _csharp_type ; 

    public struct field
    {
        public FieldInfo     fi   ;
        public ExpSerializer expS ;
    }

    public SortedDictionary<string,field> fields = new SortedDictionary<string, field>();

    public default_struct_serializer (Type csharp_type , bool only_natural = false )     // these constructors are "sort of trampolined" with GetSerializer(). Adding a decrementing integer argument in both of them should be enough as recursion guard against loops the type graph 
    {
        var thisStruct_type_args = new List<KV>();
        foreach ( FieldInfo fi in csharp_type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ))
        {
            // readonly fields ? 
            // circles ? ----- FUCK ! 
            var fieldSerializer = ExpType.GetSerializer(fi.FieldType, only_natural);

            thisStruct_type_args.Add ( new KV { name = fi.Name , exp = fieldSerializer.exp_type  }); 
            fields              .Add ( fi.Name , new field { fi = fi , expS = fieldSerializer }  ) ;
        }
        _exp_type = new StructExpType( thisStruct_type_args) ; 
        _csharp_type = csharp_type;
    }

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var SView           = channel.GetStructView_RO(_exp_type , arg );
        object struct_inst  = Activator.CreateInstance(csharp_type);
        TypedReference tref = __makeref (struct_inst);
        foreach ( var fld in fields.Values)
        {
            TMPVAL_Type   serd_field_value   = SView.Get(fld.fi.Name);
            object        deserd_field_value = fld.expS.DESER(serd_field_value, channel);
            fld.fi.SetValueDirect(tref,deserd_field_value);
        }
        return struct_inst ;
            

    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        if ( ! (arg.GetType() == csharp_type) )  throw new Exception() ; // GetType() pierces through boxing ( by experiment ) 

        var StructBuilder = channel.GetStructBuilder(_exp_type );

        foreach( field fld in fields.Values)
        {
            object           csharp_field_value = fld.fi.GetValue(arg);
            TMPVAL_Type  serialized_field_value = fld.expS.SER(csharp_field_value, channel);
            StructBuilder.Add(fld.fi.Name , serialized_field_value ) ;
        }
        return StructBuilder.Final();

    }
}

public abstract class StructLikeClassSerializer<T> : CustomSerializerT<T> where T:class,new() 
{
    public override ExpType exp_type => _exp_type;

    public struct field
    {
        public FieldInfo     fi   ;
        public ExpSerializer expS ;
    }

    public static SortedDictionary<string,field> fields = new SortedDictionary<string, field>() ;
    public static StructExpType _exp_type;

    static StructLikeClassSerializer () =>  INIT();


    public static void INIT()
    {
        var thisStruct_type_args = new List<KV>();
        
        foreach ( FieldInfo fi in (typeof(T)).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ))
        {
            
            var fieldSerializer = ExpType.GetSerializer( fi.FieldType );

            thisStruct_type_args.Add ( new KV { name = fi.Name , exp = fieldSerializer.exp_type  }); 
            fields              .Add ( fi.Name , new field { fi = fi , expS = fieldSerializer }  ) ;
        }
        _exp_type = new StructExpType( thisStruct_type_args) ; 

    }

    
    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        if ( ! (arg.GetType() == csharp_type) )  throw new Exception() ; // GetType() pierces through boxing ( by experiment )
        
        var StructBuilder = channel.GetStructBuilder(_exp_type );
        foreach ( var fld in fields.Values)
        {
            object           csharp_field_value = fld.fi.GetValue(arg);
            TMPVAL_Type  serialized_field_value = fld.expS.SER(csharp_field_value, channel);
            StructBuilder.Add(fld.fi.Name , serialized_field_value ) ;
        }
        return StructBuilder.Final();
    }


    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var SView           = channel.GetStructView_RO(_exp_type , arg );
        object obj_inst  = Activator.CreateInstance(csharp_type);
        
        foreach ( var fld in fields.Values)
        {
            TMPVAL_Type   serd_field_value   = SView.Get(fld.fi.Name);
            object        deserd_field_value = fld.expS.DESER(serd_field_value, channel);
            fld.fi.SetValue(obj_inst,deserd_field_value);
        }
        return obj_inst ;
    }

}