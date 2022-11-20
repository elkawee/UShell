using NLSPlain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public static class AssembliesAux
{

    /*
        if the internet is to be trusted, the list of referenced Assemblies is pruned at compile/link time
        if the *code* in a compiled Assembly does not explicitly access any type of the referenced one

        so this is still incomplete 
    */

   

    public class PlainNameComparer_Assembly : EqualityComparer<Assembly>
    {
        public static string StringPlainName ( Assembly asm) => asm.GetName().Name ; 

        public override bool Equals(Assembly x, Assembly y) => StringPlainName( x ) == StringPlainName( y ); 

        public override int GetHashCode(Assembly obj) => StringPlainName(obj).GetHashCode();

    }

    public class PlainNameComparer_AssemblyName : EqualityComparer<AssemblyName>
    {
        public static string StringPlainName ( AssemblyName asmN) => asmN.Name ; 

        public override bool Equals(AssemblyName x, AssemblyName y) => StringPlainName( x ) == StringPlainName( y ); 

        public override int GetHashCode(AssemblyName obj) => StringPlainName(obj).GetHashCode();

    }

    

    static bool initialzing = false ;          // to catch ugly edge-cases 
    static Assembly[] _allAssemblies = null ; 

    public static Assembly[] allAssemblies {  get { 
            if ( _allAssemblies != null ) return _allAssemblies ;

            if ( initialzing ) throw new Exception("double initialization request - this means assembly discovery has either crashed or there is a race condition - unrecoverable - this is a severe bug ");
            initialzing = true ;
            FetchAllAssemblies();
            initialzing = false ;
            return _allAssemblies ;

            } } 

    public static void FetchAllAssemblies ()
    {
        HashSet<AssemblyName> skippedAssemblies = new HashSet<AssemblyName>() ;
        HashSet<AssemblyName> todoAssemblies    = new HashSet<AssemblyName>() ;

        HashSet<Assembly>     doneAssemblies    = new HashSet<Assembly>();


        var comparer_asmname =  new PlainNameComparer_AssemblyName() ;

        // you can only get a reference to ::Assembly if the assembly it represents can and was successfully loaded
        // since we don't want to consider references between unloadable asms anyway using this type is quite practical 

        Action<Assembly> pushEdges = ( Assembly asm) => {
            AssemblyName [] new_names =  asm.GetReferencedAssemblies();
            foreach ( var name in new_names)
            {
                if ( ! skippedAssemblies.Contains(name ) 
                    && ( ! doneAssemblies.Select( x => x.GetName()).Contains(name , comparer_asmname )) )
                {
                    todoAssemblies.Add(name );
                }

                    
            }
        };

        Assembly[] initialAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach ( Assembly asm in initialAssemblies)
        {
            pushEdges(asm);
        }

        while( todoAssemblies.Any())
        {
            // pop random elem 
            AssemblyName pivot_name = todoAssemblies.First();    todoAssemblies.Remove(pivot_name);
            Assembly     pivot_asm  = null ;

            try    // load 
            {
                pivot_asm = AppDomain.CurrentDomain.Load(pivot_name);
                doneAssemblies.Add(pivot_asm);
            } catch ( Exception e)
            {
                skippedAssemblies.Add( pivot_name);
                e.Message.NLSend("skipping " + pivot_name.Name + " because : ");
            }

            // add edges  ( adding to "done" is done beforehand - i don't know if selfedges are contained in this graph - and this way i don't need to ) 
            pushEdges(pivot_asm);

        }

        _allAssemblies = doneAssemblies.ToArray();

    }


    public static IEnumerable<Type> GetTypeWhere(Func<Type, bool> filterF)
    {
        List<Type> R = new List<Type>();

        foreach ( Assembly asm in allAssemblies )
        {
            foreach ( Type T in asm.GetTypes())
            {
                if( filterF( T)) R.Add(T); 
            }
        }
        
        return R;
    }
}


