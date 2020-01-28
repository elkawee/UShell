using System;

using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

using PTokBase = MainGrammar.PTokBase;

namespace ShellCommon {

    

    public abstract class CMD_Base { }

    public abstract class REQ_Base : CMD_Base { }
    public abstract class RESP_Base : CMD_Base { }

    public class AC_Req : REQ_Base {
        public string arg;
        public int offs;
    }


    public class AC_Resp : RESP_Base {
        public string []       suggs        = new string [0];
        public int             nu_offs      = 0;
        public PTokBase []     toks         = new PTokBase[0];
        public string          msg          = null;
        public bool            toks_changed = false;


        public override string ToString () {
            return new {
                suggs = string.Join(",",suggs),
                nu_offs ,
                toks = "(... todo)",
                err_string = msg
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