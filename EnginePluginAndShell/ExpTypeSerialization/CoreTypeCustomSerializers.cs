using ShellCommon;
using System;
using LightJson;
using System.Reflection;
using System.Linq;

public class EVAL_RespSerializer : CustomSerializerT<EVAL_Resp>
{
    public override ExpType exp_type => _exp_type;


    public static SequenceExpType _seq_exp_type = new SequenceExpType(new ExpAny());
    public static StructExpType _exp_type = new StructExpType(new KV[] {
            new KV { name = "success" , exp = SerPrimitiveExpType.BOOL           },
            new KV { name = "msg"     , exp = SerPrimitiveExpType.STRING         },
            new KV { name = "result"  , exp = _seq_exp_type }
            });

    public EVAL_RespSerializer()
    {

    }

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var eval_resp = new EVAL_Resp();

        var struct_view = channel.GetStructView_RO(_exp_type, arg);

        eval_resp.msg = (string)channel.DESER_Primitive(SerPrimitiveExpType.STRING, struct_view.Get("msg"));
        eval_resp.success = (bool)channel.DESER_Primitive(SerPrimitiveExpType.BOOL, struct_view.Get("success"));

        // this is a wierd special case - the only (consumer of this message implemented in c#) to ever deserialize this type is the shell client
        // and that wants to display the json of the payload - that's the whole point of serializing it to begin with 
        // ... otoh 
        // deserializing it properly would be great for unit testing - imma go with strings for now 
        // ( in general, the shell also doesn't have all the types that could be coming throgh that pipe ) 

        var seq_view = channel.GetSequenceView_RO(_seq_exp_type, struct_view.Get("result"));

        eval_resp.result = new object[seq_view.Count()];
        if (channel is ChannelLightJson)
        {
            for (int i = 0; i < seq_view.Count(); i++)
            {
                var jval = (JsonValue)(object)seq_view.Next();
                eval_resp.result[i] = jval.ToString();
            }
        }
        else throw new NotImplementedException();

        return eval_resp;
    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {

        var eval_resp = (EVAL_Resp)arg;

        var struct_build = channel.GetStructBuilder(_exp_type);
        struct_build.Add("success", channel.CREATE_Primitive(SerPrimitiveExpType.BOOL, eval_resp.success));
        struct_build.Add("msg", channel.CREATE_Primitive(SerPrimitiveExpType.STRING, eval_resp.msg));

        var seq_build = channel.GetSequenceBuilder(new SequenceExpType(new ExpAny()), eval_resp.result.Length);
        foreach (var ob in eval_resp.result)
        {
            try
            {
                var SER = ExpType.GetSerializer(ob.GetType());
                seq_build.Append(SER.SER(ob, channel));
            }
            catch (Exception)
            {
                seq_build.Append(channel.CREATE_Primitive(SerPrimitiveExpType.STRING, "<no serializer>"));
            }
        }
        struct_build.Add("result", seq_build.Final());

        return struct_build.Final();

    }
}



public class MemberInfoSerializer : CustomSerializerT<MemberInfo>
{
    /*
        the only real way to serialize MemberInfo, is to store all arguments needed to uniquely fetch the instance again via reflection 
        (this is very incomplete but suffices for now ) 
    */

    public override ExpType exp_type => _exp_type;

    public static SystemTypeSerializer type_serializer = new SystemTypeSerializer();
    public static StructExpType _exp_type = new StructExpType(new KV[]
    {
            new KV { name = "memberKind" , exp = SerPrimitiveExpType.STRING },
            new KV { name = "name"       , exp = SerPrimitiveExpType.STRING },
            new KV { name = "type"       , exp = type_serializer.exp_type}
    });

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var struct_build = channel.GetStructBuilder(_exp_type);
        var membI = (MemberInfo)arg;
        struct_build.Add("name", channel.CREATE_Primitive(SerPrimitiveExpType.STRING, membI.Name));
        if (membI is PropertyInfo)
        {
            var propI = (PropertyInfo)membI;
            struct_build.Add("type", type_serializer.SER(propI.PropertyType, channel));
            struct_build.Add("memberKind", channel.CREATE_Primitive(SerPrimitiveExpType.STRING, "Property"));
        }
        else if (membI is FieldInfo)
        {
            var fieldI = (FieldInfo)membI;
            struct_build.Add("type", type_serializer.SER(fieldI.FieldType, channel));
            struct_build.Add("memberKind", channel.CREATE_Primitive(SerPrimitiveExpType.STRING, "Field"));
        }
        else throw new NotImplementedException();
        return struct_build.Final();
    }


    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var struct_view = channel.GetStructView_RO(_exp_type, arg);
        var memberKind = (string)channel.DESER_Primitive(SerPrimitiveExpType.STRING, struct_view.Get("memberKind"));
        var csharp_type = (Type)type_serializer.DESER(struct_view.Get("type"), channel);
        var name = (string)channel.DESER_Primitive(SerPrimitiveExpType.STRING, struct_view.Get("name"));

        if (memberKind == "Property")
        {
            return csharp_type.GetProperty(name);
        }
        else if (memberKind == "Field")
        {
            return csharp_type.GetField(name);
        }
        else throw new NotImplementedException();

    }

}


public class AC_ReqSerializer : StructLikeClassSerializer<AC_Req> { }
public class AC_RespSerializer : StructLikeClassSerializer<AC_Resp> { }
public class EVAL_ReqSerializer : StructLikeClassSerializer<EVAL_Req> { }
public class TYPEINFO_ReqSerializer : StructLikeClassSerializer<TYPEINFO_Req> { }
public class TYPEINFO_RespSerializer : StructLikeClassSerializer<TYPEINFO_Resp> { }


public class CMD_Serializer : CustomSerializerT<CMD_Base>
{


    public override ExpType exp_type => _exp_type;

    public static SystemTypeSerializer type_serializer = new SystemTypeSerializer();

    StructExpType _exp_type = new StructExpType(new KV[] {
            new KV{ name = "cmd_type" , exp = type_serializer.exp_type },
            new KV{ name = "cmd"      , exp = new ExpAny()}
            });

    Type _csharp_type = typeof(CMD_Base);

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var struct_build = channel.GetStructBuilder(_exp_type);
        struct_build.Add("cmd_type", type_serializer.SER(arg.GetType(), channel));
        ExpSerializer sub_type_ser = ExpType.GetSerializer(arg.GetType());
        TMPVAL_Type serd_subtype = sub_type_ser.SER(arg, channel);

        struct_build.Add("cmd", serd_subtype);
        return struct_build.Final();
    }

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        var struct_view = channel.GetStructView_RO(_exp_type, arg);
        Type type = (Type)type_serializer.DESER(struct_view.Get("cmd_type"), channel);

        var subtype_ser = ExpType.GetSerializer(type);

        return subtype_ser.DESER(struct_view.Get("cmd"), channel);

    }

}

// doing System.Type as a CustomSerializer for now - mostly to test it out 

/*
 * hey this one actually has a description of how to parse the full type names 
 * https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=netframework-4.6.1#system-type-assemblyqualifiedname
 */

public class SystemTypeSerializer : CustomSerializerT<System.Type>
{
    public override ExpType exp_type => SerPrimitiveExpType.STRING;

    public override object DESER<TMPVAL_Type>(TMPVAL_Type arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        string asm_qual_name = (string)channel.DESER_Primitive(SerPrimitiveExpType.STRING, arg);

        return Type.GetType(asm_qual_name, true);
    }

    public override TMPVAL_Type SER<TMPVAL_Type>(object arg, SerializerChannel_min<TMPVAL_Type> channel)
    {
        return channel.CREATE_Primitive(SerPrimitiveExpType.STRING, ((Type)arg).AssemblyQualifiedName);
    }
}