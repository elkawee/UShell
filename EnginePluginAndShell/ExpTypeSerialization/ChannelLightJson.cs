using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;



using LightJson ;


public class ChannelLightJson : SerializerChannel<JsonValue, string>
{
    public override JsonValue CREATE_Primitive(SerPrimitiveExpType exp_type, object arg)
    {
        if(exp_type.cs_type == typeof( int    ) ) return new JsonValue( (int)     arg) ;
        if(exp_type.cs_type == typeof( string ) ) return new JsonValue( (string ) arg );
        if(exp_type.cs_type == typeof( float  ) ) return new JsonValue( (float )  arg );
        if(exp_type.cs_type == typeof( bool   ) ) return new JsonValue( (bool )   arg );
        if(exp_type is EnumExpType)               return new JsonValue( arg.ToString());
        throw new NotImplementedException();
    }
    public override object DESER_Primitive(SerPrimitiveExpType exp_type, JsonValue tmp_val)
    {
        if( exp_type.cs_type == typeof( int    ) ) return (int)    tmp_val ;         //  uses implicit cast  operators from LightJson , TODO: make sure this throws if not properly convertible 
        if( exp_type.cs_type == typeof( string ) ) return (string) tmp_val;
        if( exp_type.cs_type == typeof( float  ) ) return (float) (double) tmp_val;  // there is no float variant in the list of implicit cast operators in JsonValue - somehow this leads to it being resolved to the (int) variant without the double cast 
        if( exp_type.cs_type == typeof( bool   ) ) return (bool )  tmp_val;
        if( exp_type is EnumExpType             )  return Enum.Parse( ((EnumExpType)exp_type).enum_cs_type , (string)tmp_val );
        throw new NotImplementedException();
    }

    public class StructView_RO : StructView_RO<JsonValue>
    {
        StructExpType exp_type;
        JsonObject    jobj;
        public StructView_RO (StructExpType exp_type , JsonValue jval)
        {
            if ( ! jval.IsJsonObject) throw new Exception();
            jobj = jval.AsJsonObject;
            this.exp_type = exp_type;
        }
        public JsonValue Get(string name)
        {
            return jobj[name];
        }
    }
    public override StructView_RO<JsonValue> GetStructView_RO(StructExpType exp_type, JsonValue tmp_val) => new StructView_RO(exp_type , tmp_val);

    public class StructBuilder : StructBuilder<JsonValue>
    {
        StructExpType exp_type ;
        JsonObject    jobj  = new JsonObject();
        public StructBuilder( StructExpType exp_type)
        {
            this.exp_type = exp_type;
        }
        public void Add(string name, JsonValue val)
        {
            jobj[name] = val ;
        }

        public JsonValue Final()
        {
            return jobj;
        }
    }
    public override StructBuilder<JsonValue> GetStructBuilder(StructExpType exp_type) => new StructBuilder(exp_type);

    public class SequenceView_RO : SequenceView_RO<JsonValue>
    {
        public JsonArray       jarr ;
        public SequenceExpType exp_type ;
        public int rator=0;

        public SequenceView_RO(SequenceExpType exp_type, JsonValue jval )
        {
            if ( ! jval.IsJsonArray) throw new Exception();
            jarr = jval.AsJsonArray;
        }
        public int Count() => jarr.Count;
        

        public JsonValue Next()
        {
            if( rator >= jarr.Count) throw new Exception();
            var R = jarr[rator];
            rator ++; 
            return R;
        }
    }
    public override SequenceView_RO<JsonValue> GetSequenceView_RO(SequenceExpType sequence_exp_type, JsonValue tmp_val) => new SequenceView_RO(sequence_exp_type , tmp_val);

    public class SequenceBuilder : SequenceBuilder<JsonValue>
    {
        public int size ; 
        public int cnt = 0 ; 
        public JsonArray jarr = new JsonArray();
        public SequenceExpType exp_type;
        public SequenceBuilder( SequenceExpType  exp_type , int size)
        {
            this.size = size ;
            this.exp_type = exp_type ;
        }
        public void Append(JsonValue elem)
        {
            if( cnt >= size ) throw new Exception();
            jarr.Add(elem);
            cnt ++ ;
        }

        public JsonValue Final() => jarr ;
        
    }
    public override SequenceBuilder<JsonValue> GetSequenceBuilder(SequenceExpType exp_type, int size) => new SequenceBuilder(exp_type , size );
    
}



public class DeserCapsuleLightJson : DeserCapsule
{
    static ChannelLightJson channelLightJson = new ChannelLightJson();
    public JsonValue json_value;
    public DeserCapsuleLightJson() { } // only for serialization 
    public DeserCapsuleLightJson( JsonValue json_value)
    {
        this.json_value = json_value;
    }

    public object DESER(System.Type rttype , bool only_natural = false )
    {
        var serializer = ExpType.GetSerializer(rttype , only_natural);
        return serializer.DESER<JsonValue>(json_value , channelLightJson);
    }
}

public class DeserCapsuleLightJson_Serializer : CustomSerializerT<DeserCapsuleLightJson>
{
    public override ExpType exp_type => SerPrimitiveExpType.STRING;

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {

        var str_val  = (string)channel.DESER_Primitive(SerPrimitiveExpType.STRING, arg );
        var json_val = JsonValue.Parse(str_val);
        return new DeserCapsuleLightJson( json_val );
    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var json_val = ((DeserCapsuleLightJson)arg).json_value;
        return channel.CREATE_Primitive( SerPrimitiveExpType.STRING , json_val.ToString());
    }
}