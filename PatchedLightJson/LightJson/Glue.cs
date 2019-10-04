using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

//using PatchedLightJSON;
using System.IO;
using LightJson.Serialization;

namespace LightJson {
    public class Glue {

        /* TODO 
            the converters all generate immediate values such as List<sometype> which is totally wasteful
            - it's probably best to rewrite TypeMapping to expect IEnumarable<>s instead 
        */

        #region impl

        public static Dictionary<Type,Func<JsonValue,object>> Converters;
        static Glue(){
            Converters = new Dictionary<Type, Func<JsonValue,object>>();
            // TODO : 
            // LightJson does converting of primitves by "implicit operator JsonValue( sometype value ) { ... }" 
            // these get compiled to static methods , just abuse those ( this list contains false positives - quick hack ) 
            foreach ( var mi in typeof(JsonValue).GetMethods().Where( mi => mi.Name.StartsWith("op_Imp") ) ) {
                Type rt = mi.ReturnType;
                Converters[rt] = (JsonValue jv ) => {
                    return mi.Invoke(null,new object [] { jv } );
                };
                Converters[typeof(float)] = (JsonValue jv ) => (float) (double) jv ;
            }
        }
        public static object ConvertPrimitive ( Type targetType , JsonValue jv ) {
            if ( ! Converters.ContainsKey(targetType ) )  throw new Exception("no conversion for primitve type : " + targetType );
            return Converters[targetType](jv);
        } 
        public static object ConvertList ( Type elemType , JsonValue jv ) {
            Type ListType      = typeof( List<>).MakeGenericType( new [] { elemType } );
            object R           = Activator.CreateInstance( ListType );
            MethodInfo AddMeth = ListType.GetMethod("Add");
            foreach ( var elem in jv.AsJsonArray ) {
                AddMeth.Invoke( R , new [] {   Convert( elemType , elem ) } );
            } 
            return R;
        }
        public static object ConvertDict ( Type valueType , JsonValue jv ) {
            Type DictType = typeof ( Dictionary<,>).MakeGenericType( new [] { typeof ( string ) , valueType } );
            object R = Activator.CreateInstance( DictType ) ;
            //Type KVType = typeof ( KeyValuePair<,>).MakeGenericType( new [] { typeof ( string ) , valueType } );
            MethodInfo SetterMI = DictType.GetProperty("Item").GetSetMethod();
            foreach ( var kv in jv.AsJsonObject ) {
                SetterMI.Invoke(R , new [] { kv.Key , Convert( valueType , kv.Value ) } );
            } 
            return R;
        }
        #endregion

        #region interface 
        public static object Convert( Type targetType , JsonValue jv ) {
            if ( targetType.IsGenericType ) { // meaning generic and also closed 
                if (  targetType.GetGenericTypeDefinition() == typeof( List<> ) || 
                      targetType.GetGenericTypeDefinition() == typeof (IEnumerable<>)  ) 
                {

                    if ( ! jv.IsJsonArray ) throw new Exception ( "trying to convert non-array to list" ) ;
                    return ConvertList ( targetType.GetGenericArguments()[0] , jv ) ; 
                }
                if ( targetType.GetGenericTypeDefinition() == typeof ( Dictionary<,> ) ) {
                    if ( ! jv.IsJsonObject ) throw new Exception ( "trying to convert non-object to dict" ) ; 
                    if ( targetType.GetGenericArguments()[0] != typeof(string ) ) throw new Exception ("key type for dictionaries must be string" );
                    Type valueType = targetType.GetGenericArguments()[1] ; 
                    return ConvertDict( valueType , jv ) ; 
                }
                throw new Exception( "generic Type " + targetType + " not supported" ) ; 
            }
            return ConvertPrimitive( targetType , jv );
        }

        public static JsonValue ParseWithRest ( string str_in , out string rest ) {
            var strRD = new StringReader( str_in ) ; 
            var jRD   = new JsonReader(strRD ) ; 
            var R  = jRD.ReadJsonValue();
            rest = strRD.ReadToEnd();
            return R;
        }

        #endregion


    }
}
