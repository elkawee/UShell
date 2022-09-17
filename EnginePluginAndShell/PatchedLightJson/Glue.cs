using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using System.IO;
using LightJson.Serialization;

namespace LightJson {
    public class Glue {

        #region interface 

        /* 
         * this is the main reason for patching , 
         * consume input until a single json value is read, and return unconsumed input if there is any - instead of throwing an error like these things usually do 
         */

        public static JsonValue ParseWithRest ( string str_in , out string rest ) {
            var strRD = new StringReader( str_in ) ; 
            var jRD   = new JsonReader  ( strRD  ) ; 
            var R  = jRD.ReadJsonValue();
            rest = strRD.ReadToEnd();
            return R;
        }

        #endregion


    }
}
