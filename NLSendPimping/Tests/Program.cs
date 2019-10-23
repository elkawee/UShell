using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLSPlain;

namespace Tests {
    class Program {

        static void Main(string[] args) {

            T1();
            Console.ReadLine();
        }
        static void Dummy( int [] _ ) {
            // cast elems to anonymous struct and send again
            // this is to test proper polymorphism in .ToString() (NLSendRec) ...  i'm paranoid like that 
            _.Select( x => new { x = x , dummy = "dummy" } ) .NLSendRec();
        }

        static void T1 () {
            new int [] { 1,2,3 }.NLSendRec();
            Dummy( new [] {1 }.NLSendRec() );
            "------ zero elems ----------".NLSend();
            Dummy( new int [0].NLSendRec() ) ; 
            "------- metric shitton of elems ---------".NLSend();
            Enumerable.Range(1,100 * 100 ).NLSendRec();
            
            
            Dummy ( Enumerable.Range(1,100 * 100 ).NLSendRec().ToArray() ) ; // <- this overflows UDP size  
            
            // TODO: windows can and does automatic IP fragmentation in some cases, maybe this can be utilised from c# and loopback too?
            
        }
    }
}
