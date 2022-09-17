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




