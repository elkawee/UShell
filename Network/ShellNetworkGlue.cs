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

    // veeeeeery ad hoc 
    // veery unsafe 

    public static class LightJsonExtensions {
        public static LightJson.JsonObject addTypeTag ( LightJson.JsonObject jObj , CMD_Base cmd_base ) {
            return jObj.Add( "cmd" , cmd_base.GetType().Name ) ;
        }
        public static LightJson.JsonValue LJ_Value ( this CMD_Base cmd_base ) { 
            if ( cmd_base is AC_Req ) return  (cmd_base as AC_Req).LJ_Value(); // don't fully understand the sorcery behind this - there is an implicit cast operator for JsonObject -> JsonValue 
            if ( cmd_base is AC_Resp ) return  (cmd_base as AC_Resp).LJ_Value();

            if ( cmd_base is EVAL_Req  ) return  (cmd_base as EVAL_Req ).LJ_Value();
            if ( cmd_base is EVAL_Resp ) return  (cmd_base as EVAL_Resp).LJ_Value();
            throw new NotImplementedException();
        } 

        public static JsonValue LJ_Value ( this EVAL_Req eval_req ) {  
            var jObj = new JsonObject();
            jObj["expr"] = eval_req.expr;
            return addTypeTag( jObj , eval_req);
        }
        public static JsonValue LJ_Value ( this EVAL_Resp eval_resp ) { 
            var jObj = new JsonObject();
            jObj["success"] = eval_resp.success;
            jObj["msg"]     = eval_resp.msg;

            var result_arr = new JsonArray();
            foreach ( var itm in eval_resp.result ) result_arr.Add( itm.ToString() ) ;  // quick-HACK ! TypeSerializationMapping also needs a Producer direction for this to be done all proper like 

            jObj["result"]  = result_arr; 
            return addTypeTag( jObj , eval_resp );
        }

        public static LightJson.JsonValue LJ_Value( this AC_Req ac_req ) {
            LightJson.JsonObject Jobj = new LightJson.JsonObject();
            Jobj.Add("arg" , ac_req.arg );
            Jobj.Add("offs", ac_req.offs );
            return addTypeTag( Jobj , ac_req );
        }

        public static LightJson.JsonValue LJ_Value( this PTokBase[] toks ) {
            var Jarr = new JsonArray();
            foreach ( var tok in toks ) {
                var obj = new JsonObject();
                if ( tok is PTokWhitespace ) {
                    obj["E"] = "Whitespace";
                    obj["len"] = (tok as PTokWhitespace).len;
                } else { 
                    var ptok = (PTok) tok ;
                    obj["E"]   = ptok.E.ToString();
                    obj["pay"] = ptok.pay;
                }
                Jarr.Add( obj ) ;
            }
            return Jarr;
        }

        public static JsonValue LJ_Value ( this AC_Resp ac_resp ) {
            var Jobj = new JsonObject();
            var suggs_Jarr = new JsonArray();
            foreach ( var sugg in ac_resp.suggs ) suggs_Jarr.Add( sugg ) ;

            Jobj["suggs"]        = suggs_Jarr;
            Jobj["nu_offs"]      = ac_resp.nu_offs;
            Jobj["toks"]         = ac_resp.toks.LJ_Value();
            Jobj["msg"]          = ac_resp.msg;
            Jobj["toks_changed"] = ac_resp.toks_changed;
            return addTypeTag ( Jobj, ac_resp );
        }

        public static IEnumerable <PTokBase> toks_form_JSonValue( JsonArray jArr ) {
            foreach ( JsonObject jObj in jArr ) {
                if ( jObj["E"] == "Whitespace" ) yield return new PTokWhitespace { len = jObj["len"] };
                else { 
                    PTokE E = (PTokE) Enum.Parse( typeof ( PTokE ), jObj["E"] ) ;
                    yield return new PTok { E = E , pay = jObj["pay"] } ;
                }
            }
        }

        public static CMD_Base CMD_from_JSonValue ( LightJson.JsonObject cmd_jObj  ) { 
            string type_tag = cmd_jObj["cmd"] ;
            if ( type_tag == "AC_Req" ) 
                return new AC_Req   { 
                    arg   = cmd_jObj["arg"] , 
                    offs  = cmd_jObj["offs"] 
                } ;
            
            if ( type_tag == "AC_Resp" ) 
                return new AC_Resp { 
                    suggs         = cmd_jObj["suggs"].AsJsonArray.Select( s => s.AsString).ToArray(),
                    nu_offs       = cmd_jObj["nu_offs"],
                    toks          = toks_form_JSonValue( cmd_jObj["toks"] ).ToArray(),
                    msg           = cmd_jObj["msg"],
                    toks_changed  = cmd_jObj["toks_changed"]
                };
            if ( type_tag == "EVAL_Req" ) {
                return new EVAL_Req { 
                    expr = cmd_jObj["expr"]
                };
            }
            if ( type_tag == "EVAL_Resp" ) {
                return new EVAL_Resp { 
                    result  = cmd_jObj["result"].AsJsonArray.Select( s=>s.AsString ).ToArray(),
                    msg     = cmd_jObj["msg"],
                    success = cmd_jObj["success"]
                };
            }

            throw new NotImplementedException();
        }
        
    
    }

    #if legacy_dump_paste_dingens
        // stupid hack to get the reader to only block if there really are no more bytes to read 
        // this was originally for NewtonJson Reader which insisted on reading whole blocks if possible and thus blocking on the TCP-stream if the packet was too short 
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
                    Console.WriteLine("read in :" + index + " : " + count );
                    int bytesRead =  base.Read(buffer,index,1);
                
                    string s = "";
                    for ( int i =index ; i< index + bytesRead ; i++ ) s += buffer[i];
                    Console.WriteLine("read    :" + bytesRead + "(" + s + ")" );
                    return bytesRead;
                }
            }

    #endif 


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
            LJ_Writer.Write ( cmd_base.LJ_Value() ) ;
        }

        public CMD_Base Read () {
            return ReadInternal( LJ_Reader );
        }

        public CMD_Base ReadInternal (LightJson.Serialization.JsonReader LJ_Reader ) {       // with explicit stream adapter for convenient unit tesing
            return LightJsonExtensions.CMD_from_JSonValue( LJ_Reader.ReadJsonValue() );
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
