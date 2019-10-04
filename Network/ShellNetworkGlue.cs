using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;



using D = System.Diagnostics.Debug;
using Newtonsoft.Json;




namespace ShellCommon {

#if !fake_network

    public static class AUX {   // old low level tcp stuff , prob. not needed anymore 
        const ulong sanity = (ulong)0xfac0;
        public static byte [] MK_delim ( int sz ) {
            return BitConverter.GetBytes((ulong)0xfac0).Concat ( BitConverter.GetBytes((ulong) sz) ).ToArray();
        }
        public static int READ_delim ( byte [] in_buff ) {
            
            var _sanity = BitConverter.ToUInt64(in_buff , 0 );
            D.Assert ( _sanity == sanity );
            return (int) BitConverter.ToUInt64( in_buff , 8 );
        }
        public static byte []  ReadNBytesBlocking ( int N , NetworkStream NetS ) {
            // Rube Goldberg would be proud 
            int read_overall = 0;
            var buffer = new byte [ N ] ; 
            /* var NetS = new NetworkStream( sock ); */
            IAsyncResult AR ; 
            while ( read_overall < N ) {
                AR = NetS.BeginRead(buffer,read_overall, N - read_overall , (_) => { } , 1 ) ;
                int curr_read = NetS.EndRead( AR) ;
                read_overall += curr_read;
            }
            if ( read_overall != N ) throw new Exception ( "Low level socket reading logic is fucked. All is lost.");
            return buffer;

        }
    }
#endif

    #region TCPAdapter
    public class JsonTCPAdapter {
        // stupid hack to get the reader to only block if there really are no more bytes to read 
        public class NoBlockReader:StreamReader {
                public NoBlockReader(string path) : base(path) {
                }

                public NoBlockReader(Stream stream) : base(stream) {
                }

                public NoBlockReader(string path,bool detectEncodingFromByteOrderMarks) : base(path,detectEncodingFromByteOrderMarks) {
                }

                public NoBlockReader(string path,Encoding encoding) : base(path,encoding) {
                }

                public NoBlockReader(Stream stream,Encoding encoding) : base(stream,encoding) {
                }

                public NoBlockReader(Stream stream,bool detectEncodingFromByteOrderMarks) : base(stream,detectEncodingFromByteOrderMarks) {
                }

                public NoBlockReader(string path,Encoding encoding,bool detectEncodingFromByteOrderMarks) : base(path,encoding,detectEncodingFromByteOrderMarks) {
                }

                public NoBlockReader(Stream stream,Encoding encoding,bool detectEncodingFromByteOrderMarks) : base(stream,encoding,detectEncodingFromByteOrderMarks) {
                }

                public NoBlockReader(string path,Encoding encoding,bool detectEncodingFromByteOrderMarks,int bufferSize) : base(path,encoding,detectEncodingFromByteOrderMarks,bufferSize) {
                }

                public NoBlockReader(Stream stream,Encoding encoding,bool detectEncodingFromByteOrderMarks,int bufferSize) : base(stream,encoding,detectEncodingFromByteOrderMarks,bufferSize) {
                }
                public override int Read(char[] buffer,int index,int count) {
                    //Console.WriteLine("read in :" + index + " : " + count );
                    int bytesRead =  base.Read(buffer,index,1);
                
                    string s = "";
                    for ( int i =index ; i< index + bytesRead ; i++ ) s += buffer[i];
                    //Console.WriteLine("read    :" + bytesRead + "(" + s + ")" );
                    return bytesRead;
                }
            }
    
        public TcpClient TCPcl;
        static JsonTextReader JRD;
        static JsonTextWriter JWR;
        NoBlockReader STrd ;
        StreamWriter STwr ;
        JsonSerializer ser = new JsonSerializer();
        public JsonTCPAdapter( TcpClient connectedClient ) {
            if ( !connectedClient.Connected ) throw new Exception () ; 
            TCPcl = connectedClient;
            STrd = new NoBlockReader(TCPcl.GetStream());
            STwr = new StreamWriter(TCPcl.GetStream());
            JRD = new JsonTextReader(STrd);
            JRD.SupportMultipleContent = true;
            JWR = new JsonTextWriter(STwr);
        }
        public void Write( CMD_Base cmd ) {
            var wrap = new CMD_Wrap ( cmd ) ;
            ser.Serialize(JWR,wrap );
            JWR.Flush();
        }
        public CMD_Base Read () {
            JRD.Read();
            CMD_Wrap wrap =  ser.Deserialize<CMD_Wrap>(JRD);
            return wrap.cmd;
        }
    }
    
    #endregion



    public static class ShellNetworkGlue {
#if fakeNetwork
        public static void Init() { }
        public static AC_Resp AC ( AC_Req  req ) {
            throw new NotImplementedException();
        }

#else
      
       

        static JsonTCPAdapter adapter;

        public static void Init() {
            var EP = new IPEndPoint( IPAddress.Loopback , 13333 );
            var CL     = new TcpClient();
            CL.Connect(EP);
            adapter = new JsonTCPAdapter( CL );
        
        }
        public static AC_Resp AC ( AC_Req req ) {
            adapter.Write( req );
            return (AC_Resp)adapter.Read() ;
        }


#endif
    }
    
}
