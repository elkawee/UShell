#### dong 

ich brauche einen Platzhalter-Typ fuer den literal-Fall 

`[ Exp1 , @lit , Exp2] \funcname` 
der dann beim absuchen der overloads fuer `funcname` als semi-wildcard dient vielleicht sogar eine neue spezielle typ column 

ausfuehrungsordnungsabhaenigkeiten : 
    - arg_tuple muss getypt sein fuer `funcname` aufloesung 
    - `funcname` muss aufgeloest sein um `@lit` deserialisieren zu koenen ( oder auch nur um festzustellen, ob eine gueltige deserialisierung existiert ) 
    - zum glueck ist der Column-Typ fuer den gesamten `Frame` konstant ( `arg_tuple` ) 
    
    - ich kann also in `emit()` von `FunCallTU` die uebliche links -> rechts Reihenfolge fuer typableitung umkehren 

### The Function name and its resolution 

Um statische Methoden und member-Methoden syntaktisch gleich behandeln zu koennen,
muss geklaert werden wie auf uneindeutigkeiten reagiert wird. 
Bsp : 
```csharp
class SomeClass { 
    public static void Foo( SomeClass _ ) { /* ... */ } 
    public        void Foo( )             { /* ... */ } 
}

```

waeren fuer eine Aufrufsyntax wie  ` [ <expr of type SomeClass> ] \Foo ` nicht via Name+Signatur
auseinanderzuhalten.

Statischer funktionsaufruf braucht definitiv einen Typnamen.

Bei Methodenaufruf ist die sache vertrackter.
- `compile time`     : Typ der Variable/Expression syntaktisch links vom `.`-Operator 
- `polymorph`        : Typ der Instanz 

(stackoverflow sagt)[https://stackoverflow.com/questions/4357729/use-reflection-to-invoke-an-overridden-base-method]
`MethodInfo::Invoke()` benutzt immer die `callvirt` variante.


### reflection API details 

Holy hell ... method binding seems pretty involved 

most of the logic involved seems to be about stuff i don't need, like querying 
for 'FooNamePrefix*' in a case insensitive way ... windows.

`RuntimeType.GetMethod(string name, type[] arg_types)` doesn't insist on exact matches.
For all tests i ran so far it behaves like compiled code. 
`!!` except for argument types that have `implicit conversion operators` ( these would have to be done by hand ) 

Der "Binder"-Kram bezieht sich auf dynamic objects? ( multi methods in ass backwards ) 


```csharp

// (dotnet sources)    rrtype.cs:2364

// Calculate prefixLookup, ignoreCase, and listType for use by GetXXXCandidates
private static void FilterHelper(
    BindingFlags bindingFlags, ref string name, bool allowPrefixLookup, out bool prefixLookup, 
    out bool ignoreCase, out MemberListType listType)
{
    prefixLookup = false;
    ignoreCase = false;

    if (name != null)
    {
        if ((bindingFlags & BindingFlags.IgnoreCase) != 0)
        {
            name = name.ToLower(CultureInfo.InvariantCulture);
            ignoreCase = true;
            listType = MemberListType.CaseInsensitive;
        }
        else
        {
            listType = MemberListType.CaseSensitive;
        }

        if (allowPrefixLookup && name.EndsWith("*", StringComparison.Ordinal))
        {
            // We set prefixLookup to true if name ends with a "*".
            // We will also set listType to All so that all members are included in 
            // the candidates which are later filtered by FilterApplyPrefixLookup.
            name = name.Substring(0, name.Length - 1);
            prefixLookup = true;
            listType = MemberListType.All;
        }
    }
    else
    {
        listType = MemberListType.All;
    }
}


// (dotnet sources)    rrtype.cs:2384

// Only called by GetXXXCandidates, GetInterfaces, and GetNestedTypes when FilterHelper has set "prefixLookup" to true.
// Most of the plural GetXXX methods allow prefix lookups while the singular GetXXX methods mostly do not.
private static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string name, bool ignoreCase)
{
    Contract.Assert(name != null);

    if (ignoreCase)
    {
        if (!memberInfo.Name.ToLower(CultureInfo.InvariantCulture).StartsWith(name, StringComparison.Ordinal))
            return false;
    }
    else
    {
        if (!memberInfo.Name.StartsWith(name, StringComparison.Ordinal))
            return false;
    }            

    return true;
}


// (dotnet sources)    rrtype.cs:2827

private ListBuilder<MethodInfo> GetMethodCandidates(
          String name, BindingFlags bindingAttr, CallingConventions callConv,
          Type[] types, bool allowPrefixLookup)
      {
          bool prefixLookup, ignoreCase;
          MemberListType listType;
          RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

          RuntimeMethodInfo[] cache = Cache.GetMethodList(listType, name);

          ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>(cache.Length);
          for (int i = 0; i < cache.Length; i++)
          {
              RuntimeMethodInfo methodInfo = cache[i];
              if (FilterApplyMethodInfo(methodInfo, bindingAttr, callConv, types) &&
                  (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(methodInfo, name, ignoreCase)))
              {
                  candidates.Add(methodInfo);
              }
          }

          return candidates;
      }



// (dotnet sources)    rrtype.cs:3178

protected override MethodInfo GetMethodImpl(
    String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, 
    Type[] types, ParameterModifier[] modifiers) 
{       
    ListBuilder<MethodInfo> candidates = GetMethodCandidates(name, bindingAttr, callConv, types, false);

    if (candidates.Count == 0) 
        return null;

    if (types == null || types.Length == 0) 
    {
        MethodInfo firstCandidate = candidates[0];

        if (candidates.Count == 1)
        {
            return firstCandidate;
        }
        else if (types == null) 
        { 
            for (int j = 1; j < candidates.Count; j++)
            {
                MethodInfo methodInfo = candidates[j];
                if (!System.DefaultBinder.CompareMethodSigAndName(methodInfo, firstCandidate))
                {
                    throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                }
            }

            // All the methods have the exact same name and sig so return the most derived one.
            return System.DefaultBinder.FindMostDerivedNewSlotMeth(candidates.ToArray(), candidates.Count) as MethodInfo;
        }
    }   

    if (binder == null) 
        binder = DefaultBinder;

    return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;                  
}

```