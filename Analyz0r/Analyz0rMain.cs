using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO ;
using System.Reflection;

using TranslateAndEval;
using LightJson;
using D = System.Diagnostics.Debug;
using SObject = System.Object;

namespace Analyz0r
{
    public static class A {
        
        
        
        public static void JsonifyCompilat ( Compilat compilat , string filename_postfix = ""  ) {
            var CHs = new HashSet<TypedCH>();

            using(var SW = new StreamWriter(new FileStream("compilat"+filename_postfix+".json",FileMode.Create))) {   // diagram in Nutshell 614 ...  wtf?

                foreach(var opcode in compilat.OPs) {

                    SW.WriteLine(conv(opcode));
                    foreach(var edge in OP2CH_edge(opcode)) {
                        CHs.Add( edge.CH ) ;
                        SW.WriteLine(conv(opcode,edge));
                    }

                }
                foreach ( var ch in CHs ) SW.WriteLine( conv ( ch ));
                CH_closedScope scope =  compilat.deltaScope.close();
                foreach ( var _ref in scope.refs() ) {
                    SW.WriteLine ( conv ( _ref.name , _ref.CH ) );
                }
            }

        }
        public static void JsonifyEval ( Compilat compilat , MemMapper MM , string filename_postfix = "") {
            // fetch all the CHS -- the MM might have more but those can be ignored 
            var CHs = new HashSet<TypedCH> () ;
            foreach ( var opc in compilat.OPs ) foreach ( var ch in OP2CHs ( opc ) ) CHs.Add( ch ) ;
            var L = new List<JsonObject>();
            foreach ( var ch in CHs ) L.Add ( conv ( ch ));
            foreach ( var ch in CHs ) {
                var column = MM.getGen ( ch ) ;
                L.Add ( conv (column ));
                L.Add ( conv ( ch , column ));
            }

            using ( var SW = new StreamWriter(new FileStream("eval"+filename_postfix+".json",FileMode.Create))) {   // todo FileMode is guesswork --- and also doesn't work  XD 
                foreach ( var j_obj in L ) SW.WriteLine ( j_obj ) ;
            }
        }

        public static JsonObject conv ( TypedCH ch , Column col ) {
            var J_edge = new JsonObject();
            J_edge["kind"] = "ch_column_edge";
            J_edge["id_from"] = ID(ch);
            J_edge["id_to"]   = ID(col);
            return J_edge;
        }

        public static JsonObject conv ( Column col ) {
            var J_col = new JsonObject();
            J_col["kind"] = "column";
            J_col["id"  ] = ID(col);
            var J_boxes = new JsonArray(); 

            foreach ( var box in col.boxes ) J_boxes.Add ( conv ( box ));
            J_col["boxes"] = J_boxes;
            return  J_col;
        }

        public static JsonObject conv ( VBox VB ) {
            // all VBox implementations are classes - so this ID stuff works
            var J_vb = new JsonObject();
            J_vb["kind"] = "vbox";
            J_vb["id"  ] = ID( VB );
            J_vb["payload"] = VB.value() == null ? "null" : VB.value().ToString();

            var J_preds = new JsonArray();
            foreach ( var pred in VB.preds() ) J_preds.Add( ID(pred ));
            J_vb["preds"] = J_preds ;
            return J_vb;
        }

        public static JsonObject conv ( OPCode opc ) {
            var J_opc = new JsonObject();
            J_opc["kind"] = "opcode"  ;
            J_opc["id" ]  = ID ( opc );
            J_opc["name"] = opc.GetType().Name;

            return J_opc; 
        }
        public static JsonObject conv ( OPCode op  , CH_Edge ch_edge  ) {
                var J_edge = new JsonObject();
                J_edge["kind"        ] = "OP>CH_edge" ;
                J_edge["id_from"     ] = ID(op);
                J_edge["id_to"       ] = ID(ch_edge.CH);
                J_edge["fieldname"   ] = ch_edge.fieldname;

                return J_edge ; 
        }
        public static JsonObject conv ( TypedCH CH ) {
            var J_CH = new JsonObject();
            J_CH["kind"] = "CH";
            J_CH["id"]   = ID ( CH );
            Type CHtype = CH.GetType();
            string name = typeof ( SingleCH).IsAssignableFrom(  CHtype ) ? "Single" : "Multi" ;
            name += "[" + CHtype.GetGenericArguments()[0].Name + "]" ;
            J_CH["name"] = name ;
            return J_CH;
        }
        public static JsonObject conv ( string scopename , TypedCH ch ) {
            var J_scope_edge = new JsonObject();
            J_scope_edge["name"] = scopename;
            J_scope_edge["kind"] = "scope_edge" ;
            J_scope_edge["id_to"] = ID(ch);
            return J_scope_edge;

        }
        



        #region util 

        // evilish and reflectiony because i don't want to put additional demands on the base types 
        // ... at least yet 

        public static IEnumerable<TypedCH> OP2CHs ( OPCode op  ) => new OPC_reflector( op.GetType() ).GetCHs(op)  ;  // todo cache the reflectors per type somewhere 
        public struct CH_Edge { public TypedCH CH ; public string fieldname ; } 
        public static IEnumerable<CH_Edge> OP2CH_edge ( OPCode op  ) => new OPC_reflector( op.GetType() ).GetCH_edges(op)  ;  // todo cache the reflectors per type somewhere 


        public class OPC_reflector {
            public readonly FieldInfo [] CH_FIs;
            public readonly Type         opcode_type;

            public OPC_reflector ( Type opcode_type ) {
                D.Assert ( opcode_type.IsSubclassOf ( typeof ( OPCode ) ));
                this.opcode_type = opcode_type;

                var BiF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ;

                CH_FIs = opcode_type.GetFields(BiF).Where( fi => typeof ( TypedCH ).IsAssignableFrom ( fi.FieldType ) ).ToArray() ;
            }
            public IEnumerable<TypedCH> GetCHs ( OPCode opcode ) {
                D.Assert ( opcode.GetType() == opcode_type );
                foreach ( var fi in CH_FIs ) yield return (TypedCH) fi.GetValue(opcode );
            }

            
            public IEnumerable<CH_Edge> GetCH_edges ( OPCode opcode ) {
                D.Assert ( opcode.GetType() == opcode_type );
                foreach ( var fi in CH_FIs ) yield return new CH_Edge { CH = (TypedCH) fi.GetValue(opcode ) , fieldname = fi.Name } ; 
            }
        }

        // need to make sure to use System.Object.GetHashCode which gives an unique id and not accidentally hit any override of it
		// dunno if there is a more direct way of doing this 
		static Func<object,int> systemHashCode = (Func<object,int>) Delegate.CreateDelegate( typeof(Func<object,int>) , typeof(SObject).GetMethod("GetHashCode") ); // stolen from c# in a nutshell p781 
        //                                                                                                                                  ^- this thing can create System.AccessViolationException instead NullRefException when invoked no null 

        // string because 64bit int -> double is lossy 
        // class  because all of this stuff assumes we are dealing with object instances exclusively 
        public static string ID<T> ( T arg ) where T : class {
            if ( arg == null ) return "null" ;      // <- don't yet know what's best
            return systemHashCode(arg).ToString();         // todo 
        }

        #endregion
    }
}
