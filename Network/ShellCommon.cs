using System;

using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ShellCommon {


    public enum ShellTokenE {
            PlainIdentifier,
            Field,
            Prop,
            DescImmediate,
            DescGreedy,
            DescNonGreedy,
            Whitespace,
            ColonSeparator,
            SG_Operator,
            SG_Filter,
            Error
    }


    public struct ShellToken {
            public int s_offs, e_offs;              // <- remove 
            public string orig;
            public ShellTokenE id;                  // <- remove 
            public ConsoleColor col ;
            //public ShellToken( Token from )     { s_offs = from.s_offs ; e_offs = from.e_offs ; orig = from.orig ; id = from.id ; }
            public override string ToString ()  { return new { type = id , orig=orig }.ToString() ;}
    }

    public abstract class CMD_Base { }

    public abstract class REQ_Base : CMD_Base { }
    public abstract class RESP_Base : CMD_Base { }

    public class AC_Req : REQ_Base {
        public string arg;
        public uint offs;
    }
    public class AC_Resp : RESP_Base {
        public string [] suggs ;
        public uint nu_offs;
        public ShellToken [] toks;
        
        public bool error;
        public string err_string;
        public override string ToString () {
            return new {
                suggs = string.Join(",",suggs),
                nu_offs = nu_offs ,
                toks = "(... todo)",
                error = error ,
                err_string = err_string
            }.ToString();
        }
    }


    

     public class WrapConverter : JsonConverter {
            public override bool CanConvert(Type objectType) {
                return objectType == typeof ( CMD_Wrap ) ; 
            }
            public override object ReadJson(JsonReader reader,Type objectType,object existingValue,JsonSerializer serializer) {
                JObject jo = JObject.Load( reader ) ;
                CMD_Base base_cmd = null;
                string  enm = jo["kind"].ToObject<string>();

                if      ( enm == "AC_Req" )  base_cmd = jo["cmd"].ToObject<AC_Req>();
                else if ( enm == "AC_Resp" ) base_cmd = jo["cmd"].ToObject<AC_Resp>();
                else throw new NotImplementedException();
                
                return new CMD_Wrap ( base_cmd );
            }
            public override bool CanWrite
            {
                get { return false; }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException(); // won't be called because CanWrite returns false
            }
        }

    [JsonConverter(typeof(WrapConverter))]
    public class CMD_Wrap {
        public CMD_Wrap ( CMD_Base payload ) {
            kind = payload.GetType().Name ;
            cmd = payload;
        }
        public string kind ;
        public CMD_Base cmd ;
    }
    


}