using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;



using D = System.Diagnostics.Debug;

using MainGrammar;
using LightJson;
using NLSPlain;
using System.Reflection;

namespace ShellCommon {

    
    #region TCPAdapter
    public class LightJsonTCPAdapter {
        
        public class BlockingPeekReader : TextReader { 
            /*
                since there seems to be no sensible way of making a Thread wait on a Socket

                Luckily the API surface of TextReader, that is actually used by LightJson is pretty small
                this thing buffers a single char for Peek()ing --> thereby rendering Peek() blocking, by actually attempting to read 

                ( LightJson Scanner interprets Peek() == -1 as end of input (as MSDN claimes it would indicate ) , thus errors out  
                  but StreamReader also returns -1 if it would block upon reading (from ILSPy mscorlib 2.0 StreamReader )  ) 
            */
            public NetworkStream nw_stream ;
            public bool have_char = false;
            public int  buffer_char = 0;
            public BlockingPeekReader ( NetworkStream stream  ) { 
                nw_stream = stream;
            }
            public override int Read() {
                if ( have_char ) { have_char = false ; return buffer_char ; }
                else return nw_stream.ReadByte();
            }
            public override int Peek() { 
                if ( have_char ) return buffer_char;
                else { 
                    buffer_char = nw_stream.ReadByte();
                    have_char = true;
                    return buffer_char;
                }
            }
            // just in case ... 
            public override int Read(char[] buffer, int index, int count)       => throw new NotImplementedException();
            public override int ReadBlock(char[] buffer, int index, int count)  => throw new NotImplementedException();
            public override string ReadLine()                                   => throw new NotImplementedException();
            public override string ReadToEnd()                                  => throw new NotImplementedException();
            

        }

    
        public TcpClient TCPcl;
       
        public BlockingPeekReader netwStreamAdapter ;
        public StreamWriter  streamWriter ;

        LightJson.Serialization.JsonReader LJ_Reader ;
        LightJson.Serialization.JsonWriter LJ_Writer ;
        
        public LightJsonTCPAdapter( TcpClient connectedClient ) {
            if ( !connectedClient.Connected ) throw new Exception () ; 
            if ( !connectedClient.Client.Blocking ) throw new Exception() ;


            TCPcl = connectedClient;
            netwStreamAdapter = new BlockingPeekReader(TCPcl.GetStream());
            streamWriter = new StreamWriter(TCPcl.GetStream());
            LJ_Reader = new LightJson.Serialization.JsonReader ( netwStreamAdapter );
            LJ_Writer = new LightJson.Serialization.JsonWriter ( streamWriter ); 
           
        }



        static ChannelLightJson channelLightJson = new ChannelLightJson();
        

        public void Write( CMD_Base cmd , bool debug=true  ) {
            if ( debug ) {                                        // use intermediate StringStream to Debug Dump before sending to network 
                StringWriter strWR =  new StringWriter();
                var  dummy_LJ_Writer = new LightJson.Serialization.JsonWriter( strWR );
                WriteInternal( cmd , dummy_LJ_Writer ) ;
                streamWriter.Write( strWR.ToString().NLSend("WROTE| ") ) ;
            } else { 
                WriteInternal ( cmd , LJ_Writer );
            }
            streamWriter.Flush();    // TODO guesswork , i want immediate genereation of TCP packet at this point. MSDN is pretty obtuse in this regard 
            
        }

        public void WriteInternal ( CMD_Base cmd_base ,   LightJson.Serialization.JsonWriter LJ_Writer ) { // with explicit stream adapter for convenient unit tesing
            
            var serializer = ExpType.GetSerializer(typeof(CMD_Base));
            
            LJ_Writer.Write ( serializer.SER(cmd_base,channelLightJson ) ) ;    // <- continue here 
        }




        public CMD_Base Read () {
            return ReadInternal( LJ_Reader );
        }

        public CMD_Base ReadInternal (LightJson.Serialization.JsonReader LJ_Reader ) {       // with explicit stream adapter for convenient unit tesing
            var serializer = ExpType.GetSerializer(typeof(CMD_Base));
            var LJVal = LJ_Reader.ReadJsonValue();
            return (CMD_Base)serializer.DESER(LJVal , channelLightJson);
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
      
       

        static LightJsonTCPAdapter adapter;

        public static void Init() {
            var EP = new IPEndPoint( IPAddress.Loopback , 13333 );
            var CL     = new TcpClient();
            CL.Connect(EP);
            adapter = new LightJsonTCPAdapter( CL );
        
        }
        public static AC_Resp AC ( AC_Req req ) {
            adapter.Write( req );
            return (AC_Resp)adapter.Read() ;
        }
        public static EVAL_Resp EVAL ( EVAL_Req req ) {
            adapter.Write( req ) ;
            return (EVAL_Resp)adapter.Read(); 
        }


#endif
    }
    
}
