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

    public static AssemblyName[] _allAssemblyNames = null;
    public static AssemblyName[] allAssemblyNames { // this has the side effect of loading all of these names 
        get {
            if (_allAssemblyNames == null) _allAssemblyNames = FindAndRecursivelyLoadAllAssemblies_PlainName().ToArray();
            return _allAssemblyNames;
        } }

    public static IEnumerable<Assembly> allAssemblies {  get { return allAssemblyNames.Select(assN => AppDomain.CurrentDomain.Load(assN)); } }


    public static IEnumerable<AssemblyName> FindAndRecursivelyLoadAllAssemblies_PlainName()
    {

        /*
            using `AssemblyName.FullName` instead of just `.Name` reveals that there can be multiple versions of the same
            assembly referenced and loaded at the same time - making types non unique (!!) 

            using the plain name sweeps this under the rug and lists only the assembliy versions discovered first - which is most likely what you want in 99% of cases
            ( also might prevent discovery of indirectly referenced assemblies, if referenced by a suppressed version - which is an even more esotheric problem ) 

        */

        if (_allAssemblyNames != null) return _allAssemblyNames;

        var Domain = AppDomain.CurrentDomain;

        //  GetAssemblies() is a misnomer - should be GetLoadedAssemblies, cuz that's what it does 

        var loaded_ass_names = new List<AssemblyName>(Domain.GetAssemblies().Select(_ => _.GetName()));
        var todo_ass_names   = new List<AssemblyName>(Domain.GetAssemblies().SelectMany(assem => assem.GetReferencedAssemblies()));

        var loaded_string_names = new HashSet<string>(loaded_ass_names.Select(_ => _.Name));

        while (todo_ass_names.Count() > 0)
        {
            var currentAssemName = todo_ass_names[0];
            todo_ass_names.RemoveAt(0);

            if (loaded_string_names.Contains(currentAssemName.Name)) continue;
            try
            {
                var assem = Domain.Load(currentAssemName); // here be flying exceptions potentially, in that case dependencies are also skipped 
                todo_ass_names.AddRange(assem.GetReferencedAssemblies());

                foreach (var refd_assem in assem.GetReferencedAssemblies()) new { from = assem.GetName(), to = refd_assem.Name }.NLSend("adding ref :: ");

                loaded_ass_names.Add(currentAssemName);

                loaded_string_names.Add(currentAssemName.Name);

            }
            catch (Exception e)
            {
                e.ToString().NLSend("skipping Assembly :: ");

            }

        }
        return loaded_ass_names;


    }


    public static IEnumerable<Type> GetTypeWhere(Func<Type, bool> filterF)
    {
        List<Type> R = new List<Type>();

        foreach (var assemName in allAssemblyNames )
        {
            try
            {
                var assem = AppDomain.CurrentDomain.Load(assemName);
                foreach (var t in assem.GetTypes())
                {
                    if (filterF(t)) R.Add(t);
                }
            }
            catch (Exception e)
            {
                assemName.Name.NLSend("skipping in typescan :");
                e.NLSend("because : ");
            }

        }
        return R;
    }
}


