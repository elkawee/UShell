using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#if NLSENDJSON
using Newtonsoft.Json;
#endif 


// fallback namespace with the most simple and straightforward implementation - to aid in migrating code that used older versions of this 
namespace NLSPlain {
    public static class NetLogSend {
        public static UdpClient UCl = new UdpClient( new IPEndPoint( IPAddress.Loopback , 0 )); 
        public static int dstPort = 1066; 

        static byte[] GetBytes( string s ) {
            return Encoding.UTF8.GetBytes( s ); 
        }

        public static T NLSend<T> ( this T arg , string prefix = "" ) {
            var b = GetBytes( prefix + " : " + arg ); 
            UCl.Send ( b , b.Length , new IPEndPoint ( IPAddress.Loopback , dstPort )) ; 

            return arg; 
        }
        public static T NLSend<T> ( this T arg , Func<T,System.Object> mangle , string prefix = "" ) {
            var b = GetBytes( prefix + " : " + mangle(arg) ); 
            UCl.Send ( b , b.Length , new IPEndPoint ( IPAddress.Loopback , dstPort )) ; 

            return arg; 
        }
        

        public static T NLSendRec<T> ( this T arg ,string prefix = "", uint depth=1  ) where T : System.Collections.IEnumerable {
            if ( depth != 1 ) throw new NotImplementedException();
            /*
                hack:
                the non generic IEnumerabele as constraint circumvents a limitation in the c# inferer
                that can't resolve T_inner in stuff like : 
                T_Coll<T_inner> where T_Coll : IEnumerable<T_inner> X  = int[];

                dunno if this has to be paid for with runtime boxing, but it: works on my machine(tm)
            */
            var str = "" ;
            var E = arg.GetEnumerator();
            bool first = true;
            while ( true ) {

                if ( !E.MoveNext() ) break;
                var itm = E.Current;
                if (first ) first = false ; else str+=",";
                str += itm.ToString() ;
            }

            var s = prefix + " : [" +str + "]";
            s.NLSend();
            return arg;
        }
    
        public static T NLSendD<T> ( this T arg , int dst_port = 1077 ) {
            return NLSend(arg);
        }
    }
}




namespace NLS { 

    public enum NLSerMode { toString , json , csv }
   
    public class NLSendAttribute : System.Attribute {
        public const string default_dst_host = "127.0.0.1:1066";

        // because other places to put this are just as improper 
        public static void parse_host_string ( string host , out IPAddress ip , out int port ) {
            // TODO: make this readable , and fail predictably 
            Match m = Regex.Match(host , @"(\d+)\.(\d+)\.(\d+)\.(\d+):(\d+)" );
            if ( ! m.Success ) throw new Exception("can't parse : " + host ) ;
            ip = new IPAddress( Enumerable.Range(1,4).Select( i =>   byte.Parse( m.Groups[i].Value )  ).ToArray() ) ;
            
            int.TryParse(m.Groups[5].Value, out port ); 

            // Debug.Log( "parse host string (" + host + ") -> " + ip + "  " + port ) ; 
            // mysteriously enough, this gets called every frame -- looks like the Attribute constructor gets called every time an Access to an Attribute is made 
            // which would make sense, since it is allowed to do non-compile-time-static stuff 
            // worryingly slow though 
        }
       
    }
    public class NLSendSerializationAttribute : NLSendAttribute {
        public NLSerMode mode ; 
        public NLSendSerializationAttribute( NLSerMode mode ) { this.mode = mode ; }
    }
    public class NLSendDestinationAttribute : NLSendAttribute {
        public IPAddress  dst_ip;
        public int        dst_port;
        public string     dst_name;
        public NLSendDestinationAttribute ( string dst_name , string dst_host = default_dst_host ) {
            this.dst_name = dst_name;
            try { 
                parse_host_string(dst_host, out dst_ip , out dst_port );
            } catch ( Exception e ) {
                //Debug.Log(e);  // TODO low level emergency send 
            }
        }
    }

    public static class NetLogSend {
        public const string default_dst_name = "__default__";
       
        public static string NLSendName = System.Environment.GetEnvironmentVariable("NLSendName");
        public static DST default_dst = new DST { ip = new IPAddress( new byte [] { 127,0,0,1 } ) , port = 1066 };
        public static UdpClient UCl = new UdpClient( new IPEndPoint( 0 , 0 ));

        public struct DST {
            public IPAddress ip ;
            public int port;
        }
        public static Dictionary<string,DST> destinations = new Dictionary<string, DST>();

        static NetLogSend () {
            destinations[default_dst_name] = default_dst;
        }

        public static DST get_destination ( 
            string dst_name , 
            DST fallback_dst  // to be filled depending on context, global default, or one provided from NLSendDestinationAttribute 
        ) {
            // return cached if present
            if ( destinations.ContainsKey(dst_name) ) return destinations[dst_name] ; 
            // if not see if it can be fetched from the environment
            // falling logic respective to Attribute provided defaults must be done at call-site 
            var env_var_name = "NLSendDst:" +dst_name;
            var env_value = Environment.GetEnvironmentVariable(env_var_name);
            if ( !string.IsNullOrEmpty(env_value)  ) {
                DST dst;
                try {
                    NLSendAttribute.parse_host_string(env_value, out dst.ip , out dst.port);
                } catch ( System.Exception e) {
                    low_level_emergency_send( default_dst , "WARNING: can't parse " + env_var_name + " = " + env_value + "  using default (" +fallback_dst + ") for this destination "  + e.Message);
                    dst = fallback_dst;
                }
                destinations[dst_name] = dst;
                return dst;
            }
            return fallback_dst;
        }
        
        /*
            DestinationAddress ip and port : 
            - static defaults, 
            - direct ip attribute on type , 
            - environment variable with name: "NLSendDst:<dst_name>" where <dst_name> matches the mandatory name argument for the NLSendDestination() constructor 
            in this order, from lowest priority to highest 

            e.g. [NLSendDestination("foo" , "192.168.2.101:668")] provides a fallback only in case an EnvironmentVariable named "NLSendDst:foo" = "host:port" is not present at runtime 
        */

        static byte[] GetBytes( string s ) {
            return Encoding.UTF8.GetBytes( s ); 
        }
        // have a variant of send_*() does not depend on anything fancy - in case stuff goes south 
        public static void low_level_emergency_send ( DST dst , string payload ) { var b = Encoding.UTF8.GetBytes(payload) ; new UdpClient("127.0.0.1",0).Send(b,b.Length, new IPEndPoint( dst.ip , dst.port )); }

        static void  send_string ( string serialized , string prefix  , string dst_name , DST fallback_dst ) {
            var b = GetBytes( "[" + NLSendName + "] "  + prefix + " : " + serialized ); 
            DST dst = get_destination( dst_name  , fallback_dst) ; 
            UCl.Send ( b , b.Length , new IPEndPoint ( dst.ip  , dst.port)) ; 
        }

        static void send_json ( System.Object o , string prefix , string dst_name , DST fallback_dst ) {
            #if NLSENDJSON
            send_string( JsonConvert.SerializeObject(o,Formatting.None) , prefix  , dst_name , fallback_dst ); 
            #else
            send_string( "json disabled. #define NLSENDJSON in PlayerSettings|OtherSettigns and drop in Newtonsoft.Json.dll" , prefix , dst_name , fallback_dst); 
            #endif

        }

        // --------------- public Interface ------------------- 

        public static T NLSend<T> ( this T arg , string prefix = "" ) {
            // can't get runtime type of null 
            if ( arg == null ) { send_string ( "$null$" , prefix , default_dst_name , default_dst); return arg; }       // TODO: check out how this interferes with unitys null check overloading stuff 

            // control flow of non degenerate cases 
            Type typ = arg.GetType();
            var ser_att = (NLSendSerializationAttribute) Attribute.GetCustomAttribute( typ , typeof ( NLSendSerializationAttribute ) );
            NLSerMode mode = (ser_att == null ) ?  NLSerMode.toString  : ser_att.mode ;

            var dst_att = (NLSendDestinationAttribute) Attribute.GetCustomAttribute( typ , typeof ( NLSendDestinationAttribute ));
            
            string dst_name  = default_dst_name;
            DST attr_fallback_dst = default_dst; 
            if ( dst_att != null ) {
                attr_fallback_dst = new DST { ip = dst_att.dst_ip , port = dst_att.dst_port };
                dst_name          = dst_att.dst_name;
            }
            
            if (mode == NLSerMode.toString ) send_string ( arg.ToString() , prefix , dst_name , attr_fallback_dst );
            if (mode == NLSerMode.json     ) send_json   ( arg            , prefix , dst_name , attr_fallback_dst );
            if (mode == NLSerMode.csv      ) throw new Exception("not implemented"); 


            return arg; 
        }

        // --- quick hack for dottyfy -- 
        public static T NLSendD<T> ( this T arg , int dst_port = 1066 ) {
            // todo 
            return arg;
        }



    }
}
